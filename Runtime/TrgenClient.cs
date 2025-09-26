using System;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace Trgen
{   

    /// <summary>
    /// Elemento di lavoro interno utilizzato per la gestione asincrona delle richieste
    /// nella coda di comunicazione con il dispositivo TriggerBox.
    /// </summary>
    /// <remarks>
    /// Questa classe incapsula una richiesta da inviare al dispositivo insieme
    /// al meccanismo per restituire il risultato al chiamante in modo asincrono.
    /// Viene utilizzata internamente dal sistema di code per garantire operazioni thread-safe.
    /// </remarks>
    internal class WorkItem
    {
        /// <summary>
        /// Identificatore del pacchetto da inviare al dispositivo TriggerBox.
        /// </summary>
        public int PacketId { get; }
        
        /// <summary>
        /// Dati opzionali da inviare insieme al pacchetto.
        /// Null se il comando non richiede payload aggiuntivo.
        /// </summary>
        public uint[] Payload { get; }
        
        /// <summary>
        /// Oggetto TaskCompletionSource per restituire in modo asincrono
        /// il risultato dell'operazione al chiamante.
        /// </summary>
        public TaskCompletionSource<string> Tcs { get; }

        public WorkItem(int packetId, uint[] payload = null)
        {
            PacketId = packetId;
            Payload = payload;
            Tcs = new TaskCompletionSource<string>();
        }
    }

    /// <summary>
    /// Livelli di verbosità per il sistema di logging del client TriggerBox.
    /// </summary>
    /// <remarks>
    /// I livelli sono gerarchici: impostando un livello vengono mostrati anche
    /// tutti i messaggi dei livelli inferiori. Ad esempio, <see cref="Info"/>
    /// mostrerà anche messaggi <see cref="Warn"/> e <see cref="Error"/>.
    /// </remarks>
    public enum LogLevel
    {
        /// <summary>
        /// Nessun output di logging - silenzioso completo.
        /// </summary>
        None = 0,
        
        /// <summary>
        /// Solo messaggi di errore critici che impediscono il funzionamento.
        /// </summary>
        Error = 1,
        
        /// <summary>
        /// Messaggi di warning e errori - situazioni anomale ma gestibili.
        /// </summary>
        Warn = 2,
        
        /// <summary>
        /// Informazioni generali di stato, warning ed errori - per debug standard.
        /// </summary>
        Info = 3,
        
        /// <summary>
        /// Debug completo inclusi dump dei pacchetti di rete e dettagli interni.
        /// Utilizzare solo per diagnostica approfondita.
        /// </summary>
        Debug = 4
    }


    /// <summary>
    /// Client per la gestione della comunicazione con il dispositivo TriggerBox CoSANLab.
    /// 
    /// Questa classe gestisce la connessione TCP/IP, la comunicazione tramite protocollo TrGEN
    /// e il controllo dei trigger hardware su diversi tipi di porte:
    /// - NeuroScan (NS0-NS7): Per amplificatori NeuroScan
    /// - Synamps (SA0-SA7): Per amplificatori Synamps  
    /// - GPIO (GPIO0-GPIO7): Pin GPIO generici
    /// - TMS (TMSO, TMSI): Per stimolazione magnetica transcranica
    /// 
    /// La classe supporta operazioni sincrone e asincrone, gestione automatica della memoria
    /// dei trigger e invio di marker codificati su più porte contemporaneamente.
    /// </summary>
    /// <example>
    /// <code>
    /// // Esempio base di utilizzo
    /// var client = new TrgenClient("192.168.123.1", 4242);
    /// await client.ConnectAsync();
    /// 
    /// // Invio trigger singolo
    /// client.StartTrigger(TrgenPin.NS5);
    /// 
    /// // Invio marker codificato (valore 5 = pin NS0 e NS2)
    /// client.SendMarker(markerNS: 5);
    /// 
    /// // Reset di tutti i trigger
    /// client.StopTrigger();
    /// client.Disconnect();
    /// </code>
    /// </example>
    public class TrgenClient
    {
        /// <summary>
        /// Indirizzo IP del dispositivo TriggerBox (default: "192.168.123.1").
        /// </summary>
        private readonly string ip;
        
        /// <summary>
        /// Porta TCP per la comunicazione con il dispositivo (default: 4242).
        /// </summary>
        private readonly int port;
        
        /// <summary>
        /// Client TCP per la connessione persistente.
        /// </summary>
        private TcpClient _client;
        
        /// <summary>
        /// Stream di rete per l'invio e ricezione di dati.
        /// </summary>
        private NetworkStream _stream;
        
        /// <summary>
        /// Thread worker per la gestione asincrona delle richieste in coda.
        /// </summary>
        private Thread _worker;
        
        /// <summary>
        /// Coda thread-safe per i pacchetti da inviare al dispositivo.
        /// </summary>
        private readonly BlockingCollection<WorkItem> _queue = new();
        
        /// <summary>
        /// Flag che indica se il thread worker è attivo.
        /// </summary>
        private volatile bool _running;
        
        /// <summary>
        /// Timeout in millisecondi per le operazioni di connessione e comunicazione.
        /// </summary>
        private readonly int timeout;
        /// <summary>
        /// Implementazione specifica del dispositivo TriggerBox contenente
        /// informazioni su capacità e configurazione hardware.
        /// </summary>
        private TrgenImplementation _impl;
        
        /// <summary>
        /// Lunghezza della memoria programmabile per ogni trigger (default: 32).
        /// Viene aggiornato dopo la connessione con il valore reale del dispositivo.
        /// </summary>
        private int _memoryLength = 32;
        
        /// <summary>
        /// Flag interno che indica se la connessione è stata stabilita.
        /// </summary>
        private bool connected = false;
        
        /// <summary>
        /// Indica se il client è attualmente connesso al dispositivo TriggerBox.
        /// </summary>
        /// <value>
        /// <c>true</c> se la connessione è attiva; altrimenti <c>false</c>.
        /// </value>
        public bool Connected => connected;

        /// <summary>
        /// Livello di verbosità per i log di debug e diagnostica.
        /// Controlla la quantità di informazioni mostrate durante l'esecuzione.
        /// </summary>
        /// <value>
        /// Livello di log da <see cref="LogLevel"/>. Default: <see cref="LogLevel.Warn"/>.
        /// </value>
        /// <remarks>
        /// I messaggi vengono inviati sia alla console che al sistema di log di Unity.
        /// - <see cref="LogLevel.None"/>: Nessun output
        /// - <see cref="LogLevel.Error"/>: Solo errori
        /// - <see cref="LogLevel.Warn"/>: Warning ed errori  
        /// - <see cref="LogLevel.Info"/>: Informazioni generali
        /// - <see cref="LogLevel.Debug"/>: Debug completo con dump dei pacchetti
        /// </remarks>
        public LogLevel Verbosity { get; set; } = LogLevel.Warn;

        
        /// <summary>
        /// Crea una nuova istanza di TrgenClient per la comunicazione con il dispositivo TriggerBox.
        /// </summary>
        /// <param name="ip">
        /// Indirizzo IP del dispositivo TrGEN. 
        /// Default: "192.168.123.1" (indirizzo IP standard del TriggerBox CoSANLab).
        /// </param>
        /// <param name="port">
        /// Porta TCP per la comunicazione. 
        /// Default: 4242 (porta standard del protocollo TrGEN).
        /// </param>
        /// <param name="timeout">
        /// Timeout in millisecondi per le operazioni di connessione e comunicazione.
        /// Default: 2000ms (2 secondi).
        /// </param>
        /// <remarks>
        /// Il costruttore non stabilisce automaticamente la connessione. 
        /// È necessario chiamare <see cref="ConnectAsync()"/> o <see cref="Connect()"/> per connettersi al dispositivo.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Connessione con parametri default
        /// var client = new TrgenClient();
        /// 
        /// // Connessione con IP personalizzato
        /// var client = new TrgenClient("192.168.1.100");
        /// 
        /// // Connessione con tutti i parametri personalizzati
        /// var client = new TrgenClient("192.168.1.100", 4242, 5000);
        /// </code>
        /// </example>
        public TrgenClient(string ip = "192.168.123.1", int port = 4242, int timeout = 2000)
        {
            this.ip = ip;
            this.port = port;
            this.timeout = timeout;
        }

        /// <summary>
        /// Crea un nuovo oggetto TrgenPort per la programmazione di un trigger specifico.
        /// </summary>
        /// <param name="id">
        /// Identificatore numerico del trigger. Utilizzare le costanti definite in <see cref="TrgenPin"/>
        /// per garantire compatibilità (es: <see cref="TrgenPin.NS5"/>, <see cref="TrgenPin.GPIO0"/>).
        /// </param>
        /// <returns>
        /// Nuovo oggetto <see cref="TrgenPort"/> configurato con l'ID specificato e la memoria
        /// dimensionata secondo le capacità del dispositivo connesso.
        /// </returns>
        /// <remarks>
        /// La dimensione della memoria del trigger viene determinata automaticamente dopo la connessione
        /// al dispositivo. Se non ancora connesso, viene utilizzata la dimensione di default (32 istruzioni).
        /// </remarks>
        /// <example>
        /// <code>
        /// var trigger = client.CreateTrgenPort(TrgenPin.NS5);
        /// 
        /// // Programmazione manuale del trigger
        /// trigger.SetInstruction(0, InstructionEncoder.ActiveForUs(20));
        /// trigger.SetInstruction(1, InstructionEncoder.UnactiveForUs(3));
        /// trigger.SetInstruction(2, InstructionEncoder.End());
        /// 
        /// client.SendTrgenMemory(trigger);
        /// </code>
        /// </example>
        public TrgenPort CreateTrgenPort(int id)
        {
            return new TrgenPort(id, _memoryLength);
        }

        /// <summary>
        /// Registra un messaggio di log secondo il livello di verbosità configurato.
        /// </summary>
        /// <param name="level">Livello di importanza del messaggio.</param>
        /// <param name="message">Testo del messaggio da registrare.</param>
        /// <remarks>
        /// I messaggi vengono inviati sia alla console standard che al sistema di logging di Unity.
        /// Il messaggio viene mostrato solo se il livello è minore o uguale al valore di <see cref="Verbosity"/>.
        /// I messaggi di errore e warning sono inviati ai rispettivi metodi di Unity per la colorazione appropriata.
        /// </remarks>
        private void Log(LogLevel level, string message)
        {
            if (level <= Verbosity)
            {
                // qui puoi scegliere se usare UnityEngine.Debug o Console
                Console.WriteLine($"[{level}] {message}");

                switch (level)
                {
                    case LogLevel.Error:
                        UnityEngine.Debug.LogError(message);
                        break;
                    case LogLevel.Warn:
                        UnityEngine.Debug.LogWarning(message);
                        break;
                    default:
                        UnityEngine.Debug.Log(message);
                        break;
                }
            }

        }
        
        /// <summary>
        /// Stabilisce una connessione asincrona con il dispositivo TriggerBox.
        /// </summary>
        /// <returns>
        /// Task che rappresenta l'operazione asincrona di connessione.
        /// </returns>
        /// <exception cref="TimeoutException">
        /// Viene lanciata se la connessione non viene stabilita entro il timeout specificato.
        /// </exception>
        /// <exception cref="SocketException">
        /// Viene lanciata in caso di errori di rete durante la connessione.
        /// </exception>
        /// <remarks>
        /// Questo metodo:
        /// 1. Apre una connessione TCP persistente con il dispositivo
        /// 2. Avvia il thread worker per la gestione asincrona delle richieste
        /// 3. Richiede la configurazione hardware del dispositivo
        /// 4. Aggiorna la dimensione della memoria dei trigger
        /// 
        /// Utilizzare questo metodo quando si vuole una connessione non-bloccante.
        /// Per una connessione sincrona, utilizzare <see cref="Connect()"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// var client = new TrgenClient();
        /// try 
        /// {
        ///     await client.ConnectAsync();
        ///     Debug.Log($"Connesso! Memoria trigger: {client._memoryLength}");
        /// }
        /// catch (TimeoutException)
        /// {
        ///     Debug.LogError("Timeout durante la connessione");
        /// }
        /// </code>
        /// </example>
        public async Task ConnectAsync()
        {
            _client = new TcpClient();

            // connessione asincrona con timeout
            using var cts = new CancellationTokenSource(timeout);
            var connectTask = _client.ConnectAsync(ip, port);
            var delayTask = Task.Delay(timeout, cts.Token);

            if (await Task.WhenAny(connectTask, delayTask) != connectTask)
                throw new TimeoutException($"[TRGEN] Timeout connecting to {ip}:{port}");

            await connectTask; // assicura eventuali eccezioni di connessione

            _stream = _client.GetStream();

            connected = true;
            _running = true;

            // avvia il worker loop asincrono
            _ = Task.Run(() => WorkerLoopAsync());

            // chiedi la packed implementation
            int packed = await RequestImplementationAsync();
            _impl = new TrgenImplementation(packed);
            _memoryLength = _impl.MemoryLength;

            Log(LogLevel.Debug, $"Connected. Memory length = {_memoryLength}");
        }
        
        /// <summary>
        /// Stabilisce una connessione sincrona con il dispositivo TriggerBox.
        /// </summary>
        /// <exception cref="TimeoutException">
        /// Viene lanciata se la connessione non viene stabilita entro il timeout specificato.
        /// </exception>
        /// <exception cref="SocketException">
        /// Viene lanciata in caso di errori di rete durante la connessione.
        /// </exception>
        /// <remarks>
        /// Questo metodo:
        /// 1. Apre una connessione TCP persistente con il dispositivo (operazione bloccante)
        /// 2. Configura i timeout per lettura e scrittura sul stream di rete
        /// 3. Avvia il thread worker per la gestione delle richieste in coda
        /// 4. Richiede la configurazione hardware del dispositivo
        /// 5. Aggiorna la dimensione della memoria dei trigger
        /// 
        /// Utilizzare questo metodo quando si preferisce una connessione bloccante.
        /// Per una connessione non-bloccante, utilizzare <see cref="ConnectAsync()"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// var client = new TrgenClient();
        /// try 
        /// {
        ///     client.Connect();
        ///     Debug.Log("Connessione stabilita");
        /// }
        /// catch (TimeoutException)
        /// {
        ///     Debug.LogError("Timeout durante la connessione");
        /// }
        /// </code>
        /// </example>
        public void Connect()
        {
            _client = new TcpClient();
            var result = _client.BeginConnect(ip, port, null, null);
            if (!result.AsyncWaitHandle.WaitOne(timeout))
            {
                Log(LogLevel.Info, $"Connected. Memory length = {_memoryLength}");
                throw new TimeoutException($"[TRGEN] Timeout connecting to {ip}:{port}");
            }

            _stream = _client.GetStream();
            _stream.ReadTimeout = timeout;
            _stream.WriteTimeout = timeout;

            connected = true;
            _running = true;

            _worker = new Thread(WorkerLoop) { IsBackground = true };
            _worker.Start();

            int packed = RequestImplementation();
            _impl = new TrgenImplementation(packed);
            _memoryLength = _impl.MemoryLength;

            Log(LogLevel.Debug, $"Connected. Memory length = {_memoryLength}");
        }

        /*public void ConnectDebug()
        {
            // 1) prima proviamo a interrogare il device *sincronicamente* per sapere la packed implementation
            //    usiamo la SendPacket esistente che apre una connessione temporanea (non il worker).
            try
            {
                var ack = SendPacket(0x04); // sync call — usa la tua implementazione già testata
                int packed = ParseAckValue(ack, 0x04);
                _impl = new TrgenImplementation(packed);
                _memoryLength = _impl.MemoryLength;
                Log(LogLevel.Debug, $"RequestImplementation returned {packed}, memoryLength={_memoryLength}");
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, $"RequestImplementation failed: {ex}");
                // fallback sicuro: evita memoria a 0 — usa almeno 1 o il valore di default 32
                if (_memoryLength <= 0) _memoryLength = 32;
                // opcionalmente potresti rilanciare qui se vuoi fallire la Connect
                // throw;
            }

            // 2) ora apriamo la connessione persistente e avviamo il worker
            _client = new TcpClient();
            var result = _client.BeginConnect(ip, port, null, null);
            if (!result.AsyncWaitHandle.WaitOne(timeout))
                throw new TimeoutException($"[TRGEN] Timeout connecting to {ip}:{port}");

            _stream = _client.GetStream();
            _stream.ReadTimeout = timeout;
            _stream.WriteTimeout = timeout;
            connected = true;

            _running = true;
            _worker = new Thread(WorkerLoop) { IsBackground = true };
            _worker.Start();

            Log(LogLevel.Info, $"Connected. Memory length = {_memoryLength}");
        }*/

        /// <summary>
        /// Chiude la connessione con il dispositivo TriggerBox e libera le risorse utilizzate.
        /// </summary>
        /// <remarks>
        /// Questo metodo:
        /// 1. Ferma il thread worker impostando il flag _running a false
        /// 2. Completa la coda di richieste per permettere al worker di terminare correttamente
        /// 3. Chiude lo stream di rete e la connessione TCP
        /// 
        /// È buona pratica chiamare questo metodo prima di distruggere l'istanza del client
        /// o quando l'applicazione termina per evitare connessioni dangling.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Alla fine dell'utilizzo
        /// client.Disconnect();
        /// 
        /// // O in un blocco using (se implementato IDisposable)
        /// using (var client = new TrgenClient())
        /// {
        ///     await client.ConnectAsync();
        ///     // ... utilizzo del client ...
        /// } // Disconnect automatico
        /// </code>
        /// </example>
        public void Disconnect()
        {
            _running = false;
            _queue.CompleteAdding();
            _stream?.Close();
            _client?.Close();
        }

        private string InternalSendPacket(int packetId, uint[] payload)
        {
            byte[] header = ToLittleEndian((uint)packetId);
            byte[] payloadBytes = payload != null ? BuildPayload(payload) : Array.Empty<byte>();

            byte[] raw = new byte[header.Length + payloadBytes.Length];
            Buffer.BlockCopy(header, 0, raw, 0, header.Length);
            if (payloadBytes.Length > 0)
                Buffer.BlockCopy(payloadBytes, 0, raw, header.Length, payloadBytes.Length);

            uint crc = Crc32.Compute(raw);
            byte[] crcBytes = ToLittleEndian(crc);

            byte[] packet = new byte[raw.Length + crcBytes.Length];
            Buffer.BlockCopy(raw, 0, packet, 0, raw.Length);
            Buffer.BlockCopy(crcBytes, 0, packet, raw.Length, 4);

            DebugPacket(packet, $"Sending packet 0x{packetId:X8}");

            _stream.WriteTimeout = timeout;
            _stream.ReadTimeout = timeout;

            _stream.Write(packet, 0, packet.Length);

            byte[] buffer = new byte[64];
            int read = _stream.Read(buffer, 0, buffer.Length); // qui scatterà IOException se scade il timeout
            return Encoding.ASCII.GetString(buffer, 0, read);
        }

        private async Task<string> InternalSendPacketAsync(int packetId, uint[] payload, CancellationToken ct)
        {
            byte[] header = ToLittleEndian((uint)packetId);
            byte[] payloadBytes = payload != null ? BuildPayload(payload) : Array.Empty<byte>();

            byte[] raw = new byte[header.Length + payloadBytes.Length];
            Buffer.BlockCopy(header, 0, raw, 0, header.Length);
            if (payloadBytes.Length > 0)
                Buffer.BlockCopy(payloadBytes, 0, raw, header.Length, payloadBytes.Length);

            uint crc = Crc32.Compute(raw);
            byte[] crcBytes = ToLittleEndian(crc);

            byte[] packet = new byte[raw.Length + crcBytes.Length];
            Buffer.BlockCopy(raw, 0, packet, 0, raw.Length);
            Buffer.BlockCopy(crcBytes, 0, packet, raw.Length, 4);

            DebugPacket(packet, $"Sending packet 0x{packetId:X8}");

            // scrittura async
            await _stream.WriteAsync(packet, 0, packet.Length, ct);

            // lettura async con timeout
            byte[] buffer = new byte[64];
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);

            int read = await _stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);

            if (read == 0)
                throw new IOException("Connection closed by remote host.");

            return Encoding.ASCII.GetString(buffer, 0, read);
        }

        /// <summary>
        /// Verifica se il dispositivo TriggerBox è raggiungibile sulla rete.
        /// </summary>
        /// <returns>
        /// <c>true</c> se il dispositivo risponde entro il timeout specificato; altrimenti <c>false</c>.
        /// </returns>
        /// <remarks>
        /// Questo metodo apre una connessione temporanea per testare la raggiungibilità del dispositivo
        /// senza stabilire una connessione persistente. È utile per verificare la connettività prima
        /// di tentare operazioni più complesse.
        /// 
        /// Il test non garantisce che il dispositivo sia completamente funzionale, ma solo che
        /// sia raggiungibile sulla porta specificata.
        /// </remarks>
        /// <example>
        /// <code>
        /// var client = new TrgenClient("192.168.1.100");
        /// if (client.IsAvailable())
        /// {
        ///     await client.ConnectAsync();
        ///     Debug.Log("Dispositivo disponibile e connesso");
        /// }
        /// else
        /// {
        ///     Debug.LogWarning("Dispositivo non raggiungibile");
        /// }
        /// </code>
        /// </example>
        public bool IsAvailable()
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var result = client.BeginConnect(ip, port, null, null);
                    return result.AsyncWaitHandle.WaitOne(timeout);
                }
            }
            catch { return false; }
        }

        /// <summary>
        /// Invia un pacchetto di dati al dispositivo TriggerBox utilizzando una connessione temporanea.
        /// </summary>
        /// <param name="packetId">
        /// Identificatore univoco del pacchetto che determina il tipo di operazione richiesta.
        /// Ogni comando ha un ID specifico (es: 0x02 per Start, 0x09 per Stop).
        /// </param>
        /// <param name="payload">
        /// Dati opzionali da inviare insieme al pacchetto. 
        /// Il formato dipende dal tipo di comando specificato da <paramref name="packetId"/>.
        /// </param>
        /// <returns>
        /// Stringa contenente la risposta del dispositivo, tipicamente in formato "ACKxx.valore" per successo
        /// o "NACKxx" per errore, dove xx è l'ID del pacchetto.
        /// </returns>
        /// <exception cref="SocketException">
        /// Viene lanciata in caso di errori di connessione o comunicazione di rete.
        /// </exception>
        /// <exception cref="TimeoutException">
        /// Viene lanciata se l'operazione non si completa entro il timeout specificato.
        /// </exception>
        /// <remarks>
        /// Questo metodo:
        /// 1. Apre una connessione TCP temporanea
        /// 2. Costruisce il pacchetto con header, payload e checksum CRC32
        /// 3. Invia il pacchetto e attende la risposta
        /// 4. Chiude la connessione
        /// 
        /// Per operazioni multiple, è più efficiente utilizzare una connessione persistente
        /// con <see cref="Connect()"/> e utilizzare i metodi di alto livello.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Richiesta dello stato del dispositivo
        /// var response = client.SendPacket(0x05);
        /// Debug.Log($"Stato: {response}");
        /// 
        /// // Invio comando con payload
        /// var payload = new uint[] { 0x01, 0x02 };
        /// var response = client.SendPacket(0x06, payload);
        /// </code>
        /// </example>
        public string SendPacket(int packetId, uint[] payload = null)
        {
            byte[] header = ToLittleEndian((uint)packetId);
            byte[] payloadBytes = payload != null ? BuildPayload(payload) : Array.Empty<byte>();

            byte[] raw = new byte[header.Length + payloadBytes.Length];
            Buffer.BlockCopy(header, 0, raw, 0, header.Length);
            if (payloadBytes.Length > 0)
                Buffer.BlockCopy(payloadBytes, 0, raw, header.Length, payloadBytes.Length);

            uint crc = Crc32.Compute(raw);
            byte[] crcBytes = ToLittleEndian(crc);

            byte[] packet = new byte[raw.Length + crcBytes.Length];
            Buffer.BlockCopy(raw, 0, packet, 0, raw.Length);
            Buffer.BlockCopy(crcBytes, 0, packet, raw.Length, 4);
            DebugPacket(packet, $"Sending packet 0x{packetId:X8}");
            using (var client = new TcpClient())
            {
                try
                {
                    client.Connect(ip, port); // sincrono
                    connected = true;

                    using var stream = client.GetStream();
                    stream.Write(packet, 0, packet.Length);

                    byte[] buffer = new byte[64];
                    int read = stream.Read(buffer, 0, buffer.Length);
                    return Encoding.ASCII.GetString(buffer, 0, read);
                }
                catch
                {
                    connected = false;
                    throw;
                }
            }
        }

        private byte[] ToLittleEndian(uint value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return bytes;
        }

        private byte[] BuildPayload(uint[] words)
        {
            List<byte> result = new();
            foreach (var word in words)
                result.AddRange(ToLittleEndian(word));
            return result.ToArray();
        }

        public int ParseAckValue(string ackStr, int expectedId)
        {
            if (!ackStr.StartsWith($"ACK{expectedId}"))
                throw new Exception($"Unexpected ACK: {ackStr}");
            var parts = ackStr.Split('.');
            if (parts.Length != 2)
                throw new Exception($"Malformed ACK: {ackStr}");
            return int.Parse(parts[1]);
        }

        private void DebugPacket(byte[] packet, string label = "Packet")
        {
            var hex = BitConverter.ToString(packet).Replace("-", " ");
            Log(LogLevel.Debug, $"{label}: {hex}");
        }

        // -------- API pubbliche per il controllo del TriggerBox --------
        
        /// <summary>
        /// Avvia l'esecuzione delle sequenze di trigger programmate nel dispositivo.
        /// </summary>
        /// <remarks>
        /// Questo comando fa sì che il dispositivo inizi l'esecuzione di tutte le sequenze
        /// di trigger precedentemente programmate. È necessario chiamare questo metodo dopo
        /// aver programmato i trigger desiderati con <see cref="ProgramDefaultTrigger"/> o 
        /// <see cref="SendTrgenMemory"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Programma un trigger e avvia l'esecuzione
        /// var trigger = client.CreateTrgenPort(TrgenPin.NS5);
        /// client.ProgramDefaultTrigger(trigger);
        /// client.Start(); // Avvia l'esecuzione
        /// </code>
        /// </example>
        public void Start() => EnqueuePacket(0x02);
        
        /// <summary>
        /// Ferma l'esecuzione di tutte le sequenze di trigger attive nel dispositivo.
        /// </summary>
        /// <remarks>
        /// Questo comando interrompe immediatamente l'esecuzione di tutte le sequenze
        /// di trigger senza modificare la programmazione della memoria. I trigger possono
        /// essere riavviati successivamente con <see cref="Start()"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Ferma tutti i trigger in esecuzione
        /// client.Stop();
        /// </code>
        /// </example>
        public void Stop() => EnqueuePacket(0x09);
        
        /// <summary>
        /// Imposta il livello logico dei pin NeuroScan e Synamps.
        /// </summary>
        /// <param name="mask">
        /// Maschera a bit che specifica quali pin attivare. 
        /// Bit 0-7: pin NeuroScan (NS0-NS7), Bit 8-15: pin Synamps (SA0-SA7).
        /// </param>
        /// <remarks>
        /// Questo metodo permette di controllare direttamente lo stato logico dei pin
        /// senza utilizzare le sequenze programmate. È utile per test o controlli manuali.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Attiva NS0 e NS2 (bit 0 e 2)
        /// client.SetLevel(0b00000101); // = 5 in decimale
        /// 
        /// // Attiva SA0 (bit 8)  
        /// client.SetLevel(0b100000000); // = 256 in decimale
        /// </code>
        /// </example>
        public void SetLevel(uint mask) => EnqueuePacket(0x06, new uint[] { mask });
        
        /// <summary>
        /// Imposta il livello logico dei pin GPIO.
        /// </summary>
        /// <param name="mask">
        /// Maschera a bit che specifica quali pin GPIO attivare.
        /// Bit 0-7: pin GPIO0-GPIO7.
        /// </param>
        /// <remarks>
        /// Controlla direttamente i pin GPIO del dispositivo. Ogni bit della maschera
        /// corrisponde a un pin GPIO (bit 0 = GPIO0, bit 1 = GPIO1, etc.).
        /// </remarks>
        /// <example>
        /// <code>
        /// // Attiva GPIO0 e GPIO3
        /// client.SetGpio(0b00001001); // = 9 in decimale
        /// </code>
        /// </example>
        public void SetGpio(uint mask) => EnqueuePacket(0x03, new uint[] { mask });
        
        /// <summary>
        /// Legge lo stato corrente dei pin NeuroScan e Synamps.
        /// </summary>
        /// <returns>
        /// Valore intero rappresentante lo stato dei pin come maschera a bit.
        /// Bit 0-7: stato pin NeuroScan, Bit 8-15: stato pin Synamps.
        /// </returns>
        /// <example>
        /// <code>
        /// int level = client.GetLevel();
        /// bool ns0Active = (level & 0x01) != 0; // Verifica se NS0 è attivo
        /// bool sa0Active = (level & 0x100) != 0; // Verifica se SA0 è attivo
        /// </code>
        /// </example>
        public int GetLevel() => ParseAckValue(EnqueuePacket(0x08).Result, 0x08);
        
        /// <summary>
        /// Legge lo stato generale del dispositivo TriggerBox.
        /// </summary>
        /// <returns>
        /// Codice di stato del dispositivo. I valori specifici dipendono dall'implementazione
        /// del firmware del dispositivo.
        /// </returns>
        /// <example>
        /// <code>
        /// int status = client.GetStatus();
        /// Debug.Log($"Stato dispositivo: {status}");
        /// </code>
        /// </example>
        public int GetStatus() => ParseAckValue(EnqueuePacket(0x05).Result, 0x05);
        
        /// <summary>
        /// Legge lo stato corrente dei pin GPIO.
        /// </summary>
        /// <returns>
        /// Valore intero rappresentante lo stato dei pin GPIO come maschera a bit.
        /// Bit 0-7: stato pin GPIO0-GPIO7.
        /// </returns>
        /// <example>
        /// <code>
        /// int gpioState = client.GetGpio();
        /// bool gpio0Active = (gpioState & 0x01) != 0;
        /// bool gpio7Active = (gpioState & 0x80) != 0;
        /// </code>
        /// </example>
        public int GetGpio() => ParseAckValue(EnqueuePacket(0x07).Result, 0x07);
        /// <summary>
        /// Invia la memoria programmata di un trigger al dispositivo TriggerBox.
        /// </summary>
        /// <param name="t">
        /// Oggetto <see cref="TrgenPort"/> contenente l'ID del trigger e la sua memoria programmata.
        /// La memoria deve essere precedentemente configurata con le istruzioni desiderate.
        /// </param>
        /// <remarks>
        /// Questo metodo trasferisce l'intera sequenza di istruzioni memorizzata nel trigger
        /// al dispositivo hardware. Il trigger deve essere programmato con istruzioni valide
        /// utilizzando <see cref="TrgenPort.SetInstruction"/> prima di chiamare questo metodo.
        /// </remarks>
        /// <example>
        /// <code>
        /// var trigger = client.CreateTrgenPort(TrgenPin.NS5);
        /// 
        /// // Programma una sequenza: attivo 20μs, inattivo 3μs, fine
        /// trigger.SetInstruction(0, InstructionEncoder.ActiveForUs(20));
        /// trigger.SetInstruction(1, InstructionEncoder.UnactiveForUs(3));
        /// trigger.SetInstruction(2, InstructionEncoder.End());
        /// 
        /// // Invia la programmazione al dispositivo
        /// client.SendTrgenMemory(trigger);
        /// </code>
        /// </example>
        public void SendTrgenMemory(TrgenPort t)
        {
            int id = t.Id;
            int packetId = 0x01 | (id << 24);
            EnqueuePacket(packetId, t.Memory);
        }

        /// <summary>
        /// Richiede la configurazione hardware del dispositivo TriggerBox (versione sincrona).
        /// </summary>
        /// <returns>
        /// Valore packed contenente informazioni sulla configurazione hardware del dispositivo,
        /// inclusi il numero di pin disponibili per ogni tipo e la lunghezza della memoria programmabile.
        /// </returns>
        /// <remarks>
        /// Questo metodo viene chiamato automaticamente durante la connessione per determinare
        /// le capacità hardware del dispositivo. Il valore restituito viene utilizzato per
        /// creare un'istanza di <see cref="TrgenImplementation"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// int packed = client.RequestImplementation();
        /// var impl = new TrgenImplementation(packed);
        /// Debug.Log($"Pin NS: {impl.NsNum}, Memoria: {impl.MemoryLength}");
        /// </code>
        /// </example>
        public int RequestImplementation()
        {
            var ack = EnqueuePacket(0x04).Result;
            return ParseAckValue(ack, 0x04);
        }
        
        /// <summary>
        /// Richiede la configurazione hardware del dispositivo TriggerBox (versione asincrona).
        /// </summary>
        /// <returns>
        /// Task che restituisce il valore packed contenente la configurazione hardware del dispositivo.
        /// </returns>
        /// <remarks>
        /// Versione asincrona di <see cref="RequestImplementation()"/>. Utilizzare questo metodo
        /// nelle operazioni asincrone per evitare il blocco del thread chiamante.
        /// </remarks>
        /// <example>
        /// <code>
        /// int packed = await client.RequestImplementationAsync();
        /// var impl = new TrgenImplementation(packed);
        /// Debug.Log($"Pin GPIO: {impl.GpioNum}, Pin SA: {impl.SaNum}");
        /// </code>
        /// </example>
        public async Task<int> RequestImplementationAsync()
        {
            var ack = await EnqueuePacket(0x04);
            return ParseAckValue(ack, 0x04);
        }

        // -------- Gestione interna --------
        private Task<string> EnqueuePacket(int packetId, uint[] payload = null)
        {
            var item = new WorkItem(packetId, payload);
            _queue.Add(item);
            return item.Tcs.Task;
        }

        private async Task WorkerLoopAsync()
        {
            try
            {
                foreach (var item in _queue.GetConsumingEnumerable())
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(timeout);
                        string ack = await InternalSendPacketAsync(item.PacketId, item.Payload, cts.Token);
                        item.Tcs.SetResult(ack);
                    }
                    catch (Exception ex)
                    {
                        item.Tcs.SetException(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, $"WorkerLoopAsync crashed: {ex}");
                connected = false;
            }
        }

        private void WorkerLoop()
        {
            try
            {
                foreach (var item in _queue.GetConsumingEnumerable())
                {
                    try
                    {
                        // Usa la tua logica SendPacket interna
                        string ack = InternalSendPacket(item.PacketId, item.Payload);
                        item.Tcs.SetResult(ack);
                    }
                    catch (Exception ex)
                    {
                        item.Tcs.SetException(ex);
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // Questo è lanciato da GetConsumingEnumerable() se CompleteAdding è stato chiamato
                Log(LogLevel.Debug, "WorkerLoop: queue completed, exiting.");
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, $"WorkerLoop crashed: {ex}");
                connected = false;
            }
        }

        /// <summary>
        /// Resetta un trigger specifico cancellando la sua memoria programmata.
        /// </summary>
        /// <param name="t">
        /// Oggetto <see cref="TrgenPort"/> che rappresenta il trigger da resettare.
        /// </param>
        /// <remarks>
        /// Questo metodo:
        /// 1. Imposta la prima istruzione come END per terminare immediatamente l'esecuzione
        /// 2. Riempie il resto della memoria con istruzioni NOT_ADMISSIBLE per sicurezza
        /// 3. Invia la memoria resettata al dispositivo
        /// 
        /// Dopo il reset, il trigger non produrrà alcun segnale fino a quando non viene
        /// riprogrammato con nuove istruzioni.
        /// </remarks>
        /// <example>
        /// <code>
        /// var trigger = client.CreateTrgenPort(TrgenPin.NS5);
        /// client.ResetTrigger(trigger); // Reset completo del trigger NS5
        /// </code>
        /// </example>
        public void ResetTrigger(TrgenPort t)
        {
            t.SetInstruction(0, InstructionEncoder.End());
            for (int i = 1; i < _memoryLength -1; i++)
                t.SetInstruction(i, InstructionEncoder.NotAdmissible());
            SendTrgenMemory(t);
        }

        /// <summary>
        /// Programma un trigger con una sequenza di default per l'invio di impulsi standard.
        /// </summary>
        /// <param name="t">
        /// Oggetto <see cref="TrgenPort"/> che rappresenta il trigger da programmare.
        /// </param>
        /// <param name="us">
        /// Durata in microsecondi dell'impulso attivo. Default: 20μs.
        /// Questo valore determina per quanto tempo il pin rimane nello stato alto.
        /// </param>
        /// <remarks>
        /// Questo metodo programma una sequenza standard composta da:
        /// 1. Attivazione del pin per la durata specificata
        /// 2. Disattivazione del pin per 3μs (tempo di ripristino)  
        /// 3. Terminazione della sequenza
        /// 4. Riempimento del resto della memoria con istruzioni non ammissibili
        /// 
        /// Questa è la configurazione più comune per l'invio di trigger in applicazioni
        /// di neurofisiologia e stimolazione.
        /// </remarks>
        /// <example>
        /// <code>
        /// var trigger = client.CreateTrgenPort(TrgenPin.NS5);
        /// 
        /// // Trigger standard da 20μs
        /// client.ProgramDefaultTrigger(trigger);
        /// 
        /// // Trigger lungo da 100μs
        /// client.ProgramDefaultTrigger(trigger, 100);
        /// 
        /// client.Start(); // Avvia l'esecuzione
        /// </code>
        /// </example>
        public void ProgramDefaultTrigger(TrgenPort t, uint us = 20)
        {
            t.SetInstruction(0, InstructionEncoder.ActiveForUs(us));
            t.SetInstruction(1, InstructionEncoder.UnactiveForUs(3));
            t.SetInstruction(2, InstructionEncoder.End());
            for (int i = 3; i < _memoryLength - 1; i++)
                t.SetInstruction(i, InstructionEncoder.NotAdmissible());
            SendTrgenMemory(t);
        }

        /// <summary>
        /// Resetta tutti i trigger specificati dalla lista di ID.
        /// </summary>
        /// <param name="ids">
        /// Lista degli identificatori dei trigger da resettare.
        /// Utilizzare le costanti di <see cref="TrgenPin"/> o le liste predefinite
        /// come <see cref="TrgenPin.AllNs"/>, <see cref="TrgenPin.AllGpio"/>.
        /// </param>
        /// <remarks>
        /// Questo metodo è un wrapper che applica <see cref="ResetTrigger"/> a tutti
        /// i trigger specificati nella lista. È utile per resettare gruppi di trigger
        /// dello stesso tipo (es. tutti i pin NeuroScan) in una singola operazione.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Reset di tutti i pin NeuroScan
        /// client.ResetAll(TrgenPin.AllNs);
        /// 
        /// // Reset di pin specifici
        /// var customPins = new List&lt;int&gt; { TrgenPin.NS0, TrgenPin.GPIO5 };
        /// client.ResetAll(customPins);
        /// 
        /// // Reset di tutti i tipi di pin
        /// client.ResetAll(TrgenPin.AllNs);
        /// client.ResetAll(TrgenPin.AllSa);
        /// client.ResetAll(TrgenPin.AllGpio);
        /// </code>
        /// </example>
        public void ResetAll(List<int> ids)
        {
            foreach (var id in ids)
            {
                var tr = CreateTrgenPort(id);
                ResetTrigger(tr);
            }
        }

        /// <summary>
        /// Attiva un singolo trigger dopo aver resettato tutti gli altri.
        /// </summary>
        /// <param name="triggerId">
        /// Identificatore del trigger da attivare.
        /// Utilizzare le costanti di <see cref="TrgenPin"/> per garantire compatibilità.
        /// </param>
        /// <remarks>
        /// Questo metodo esegue una sequenza completa:
        /// 1. Reset di tutti i trigger GPIO, Synamps e NeuroScan
        /// 2. Programmazione del trigger specificato con sequenza di default (20μs attivo)
        /// 3. Avvio dell'esecuzione
        /// 
        /// È il metodo più semplice per inviare un trigger singolo, garantendo che
        /// non ci siano interferenze da trigger precedentemente programmati.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Invio di un trigger sul pin NeuroScan 5
        /// client.StartTrigger(TrgenPin.NS5);
        /// 
        /// // Invio di un trigger su GPIO 0
        /// client.StartTrigger(TrgenPin.GPIO0);
        /// </code>
        /// </example>
        public void StartTrigger(int triggerId)
        {
            ResetAll(TrgenPin.AllGpio);
            ResetAll(TrgenPin.AllSa);
            ResetAll(TrgenPin.AllNs);

            var tr = CreateTrgenPort(triggerId);
            ProgramDefaultTrigger(tr);
            Start();
        }

        /// <summary>
        /// Attiva contemporaneamente più trigger dopo aver resettato tutti gli altri.
        /// </summary>
        /// <param name="triggerIds">
        /// Lista degli identificatori dei trigger da attivare simultaneamente.
        /// Tutti i trigger specificati verranno programmati con la stessa sequenza di default.
        /// </param>
        /// <remarks>
        /// Questo metodo:
        /// 1. Reset di tutti i trigger GPIO, Synamps e NeuroScan
        /// 2. Programmazione di tutti i trigger specificati con sequenza di default (20μs attivo)
        /// 3. Avvio dell'esecuzione simultanea
        /// 
        /// Tutti i trigger inizieranno l'esecuzione contemporaneamente, permettendo
        /// di inviare pattern complessi o marker multi-bit.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Attivazione simultanea di più pin NeuroScan
        /// var triggers = new List&lt;int&gt; { 
        ///     TrgenPin.NS0, 
        ///     TrgenPin.NS2, 
        ///     TrgenPin.NS5 
        /// };
        /// client.StartTriggerList(triggers);
        /// 
        /// // Combinazione di diversi tipi di pin
        /// var mixedTriggers = new List&lt;int&gt; { 
        ///     TrgenPin.NS1, 
        ///     TrgenPin.GPIO3,
        ///     TrgenPin.SA7
        /// };
        /// client.StartTriggerList(mixedTriggers);
        /// </code>
        /// </example>
        public void StartTriggerList(List<int> triggerIds)
        {
            ResetAll(TrgenPin.AllGpio);
            ResetAll(TrgenPin.AllSa);
            ResetAll(TrgenPin.AllNs);

            foreach (var id in triggerIds)
            {
                var tr = CreateTrgenPort(id);
                ProgramDefaultTrigger(tr);
            }
            Start();
        }

        /// <summary>
        /// Invia marker codificati in binario su una o più porte del TriggerBox.
        /// </summary>
        /// <param name="markerNS">
        /// Valore decimale del marker da inviare sui pin NeuroScan (NS0-NS7).
        /// Il valore viene convertito in binario e ogni bit attiva il corrispondente pin.
        /// Null per non inviare marker sui pin NeuroScan.
        /// </param>
        /// <param name="markerSA">
        /// Valore decimale del marker da inviare sui pin Synamps (SA0-SA7).
        /// Il valore viene convertito in binario e ogni bit attiva il corrispondente pin.
        /// Null per non inviare marker sui pin Synamps.
        /// </param>
        /// <param name="markerGPIO">
        /// Valore decimale del marker da inviare sui pin GPIO (GPIO0-GPIO7).
        /// Il valore viene convertito in binario e ogni bit attiva il corrispondente pin.
        /// Null per non inviare marker sui pin GPIO.
        /// </param>
        /// <param name="LSB">
        /// Determina l'ordine di mappatura dei bit sui pin:
        /// - <c>true</c>: LSB (Least Significant Bit) first - bit 0 → pin 0, bit 1 → pin 1, etc.
        /// - <c>false</c>: MSB (Most Significant Bit) first - bit 7 → pin 0, bit 6 → pin 1, etc.
        /// Default: false (MSB first).
        /// </param>
        /// <remarks>
        /// <para>
        /// Questo metodo converte i valori decimali in rappresentazioni binarie a 8 bit
        /// e attiva i pin corrispondenti ai bit settati a 1. È particolarmente utile per
        /// inviare codici numerici in esperimenti di neurofisiologia.
        /// </para>
        /// 
        /// <para>
        /// Il metodo esegue:
        /// 1. Reset di tutti i trigger (NS, SA, GPIO, TMS)
        /// 2. Conversione dei valori in maschere binarie a 8 bit
        /// 3. Programmazione dei trigger corrispondenti ai bit attivi
        /// 4. Avvio dell'esecuzione simultanea
        /// </para>
        /// 
        /// <para>
        /// Se tutti i parametri sono null, il metodo termina senza effettuare operazioni.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Invio del valore 5 sui pin NeuroScan
        /// // 5 in binario = 00000101, attiva NS0 e NS2 (se LSB=true) 
        /// // o NS5 e NS7 (se LSB=false, default)
        /// client.SendMarker(markerNS: 5);
        /// 
        /// // Invio simultaneo su più porte
        /// client.SendMarker(markerNS: 3, markerGPIO: 7, LSB: true);
        /// // NS: 3 = 00000011 → attiva NS0, NS1
        /// // GPIO: 7 = 00000111 → attiva GPIO0, GPIO1, GPIO2
        /// 
        /// // Esempio con MSB first (default)
        /// client.SendMarker(markerSA: 129); // 129 = 10000001
        /// // Attiva SA0 e SA7 (primi e ultimi pin)
        /// </code>
        /// </example>
        public void SendMarker(int? markerNS = null, int? markerSA = null, int? markerGPIO = null, bool LSB = false)
        {
            // Se tutti i marker sono null, esci
            if (markerNS == null && markerSA == null && markerGPIO == null)
                return;

            var neuroscanMap = new int[]
            {
                TrgenPin.NS0,
                TrgenPin.NS1,
                TrgenPin.NS2,
                TrgenPin.NS3,
                TrgenPin.NS4,
                TrgenPin.NS5,
                TrgenPin.NS6,
                TrgenPin.NS7
                    };

                    var synampsMap = new int[]
                    {
                TrgenPin.SA0,
                TrgenPin.SA1,
                TrgenPin.SA2,
                TrgenPin.SA3,
                TrgenPin.SA4,
                TrgenPin.SA5,
                TrgenPin.SA6,
                TrgenPin.SA7
                    };

                    var gpioMap = new int[]
                    {
                TrgenPin.GPIO0,
                TrgenPin.GPIO1,
                TrgenPin.GPIO2,
                TrgenPin.GPIO3,
                TrgenPin.GPIO4,
                TrgenPin.GPIO5,
                TrgenPin.GPIO6,
                TrgenPin.GPIO7
            };

            //ResetAllNS();
            //ResetAllSA();
            //ResetAllGPIO();
            //ResetAllTMSO();
            ResetAll(TrgenPin.AllGpio);
            ResetAll(TrgenPin.AllSa);
            ResetAll(TrgenPin.AllNs);
            ResetAll(TrgenPin.AllTMS);

            if (markerNS != null)
            {
                var maskNS = Convert.ToString(markerNS.Value, 2).PadLeft(8, '0').ToCharArray();
                if (!LSB) Array.Reverse(maskNS);
                for (int idx = 0; idx < maskNS.Length; idx++)
                {
                    if (maskNS[idx] == '1')
                    {
                        var nsx = CreateTrgenPort(neuroscanMap[idx]);
                        ProgramDefaultTrigger(nsx);
                    }
                }
            }

            if (markerSA != null)
            {
                var maskSA = Convert.ToString(markerSA.Value, 2).PadLeft(8, '0').ToCharArray();
                if (!LSB) Array.Reverse(maskSA);
                for (int idx = 0; idx < maskSA.Length; idx++)
                {
                    if (maskSA[idx] == '1')
                    {
                        var sax = CreateTrgenPort(synampsMap[idx]);
                        ProgramDefaultTrigger(sax);
                    }
                }
            }

            if (markerGPIO != null)
            {
                var maskGPIO = Convert.ToString(markerGPIO.Value, 2).PadLeft(8, '0').ToCharArray();
                if (!LSB) Array.Reverse(maskGPIO);
                for (int idx = 0; idx < maskGPIO.Length; idx++)
                {
                    if (maskGPIO[idx] == '1')
                    {
                        var gpx = CreateTrgenPort(gpioMap[idx]);
                        ProgramDefaultTrigger(gpx);
                    }
                }
            }

            // Avvio sequenza
            Start();
        }
        /// <summary>
        /// Ferma tutti i trigger attivi e resetta completamente lo stato del dispositivo.
        /// </summary>
        /// <remarks>
        /// Questo metodo esegue una procedura completa di arresto e pulizia:
        /// 1. Invia il comando STOP per fermare tutte le esecuzioni in corso
        /// 2. Reset di tutti i trigger NeuroScan (NS0-NS7)
        /// 3. Reset di tutti i trigger Synamps (SA0-SA7)  
        /// 4. Reset di tutti i trigger GPIO (GPIO0-GPIO7)
        /// 5. Reset di tutti i trigger TMS (TMSO, TMSI)
        /// 
        /// Dopo questo comando, tutti i pin tornano allo stato inattivo e tutte
        /// le memorie dei trigger vengono cancellate. È il comando di "panic button"
        /// per fermare immediatamente qualsiasi attività del dispositivo.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Avvio di alcuni trigger
        /// client.SendMarker(markerNS: 15, markerGPIO: 7);
        /// 
        /// // ... dopo un po' ...
        /// 
        /// // Stop completo e reset di tutto
        /// client.StopTrigger();
        /// 
        /// // Ora il dispositivo è in stato pulito e pronto per nuove operazioni
        /// </code>
        /// </example>
        public void StopTrigger()
        {
            Stop();
            //ResetAllTMSO();
            //ResetAllSA();
            //ResetAllGPIO();
            //ResetAllNS();
            ResetAll(TrgenPin.AllGpio);
            ResetAll(TrgenPin.AllSa);
            ResetAll(TrgenPin.AllNs);
            ResetAll(TrgenPin.AllTMS);
        }
    }
}
