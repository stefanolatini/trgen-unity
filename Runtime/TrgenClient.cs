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

    internal class WorkItem
    {
        public int PacketId { get; }
        public uint[] Payload { get; }
        public TaskCompletionSource<string> Tcs { get; }

        public WorkItem(int packetId, uint[] payload = null)
        {
            PacketId = packetId;
            Payload = payload;
            Tcs = new TaskCompletionSource<string>();
        }
    }

    public enum LogLevel
    {
        None = 0,   // Nessun output
        Error = 1,  // Solo errori
        Warn = 2,   // Warning ed errori
        Info = 3,   // Tutto + info
        Debug = 4   // Tutto + dump pacchetti ecc.
    }


    /// <summary>
    /// Gestisce la connessione, la comunicazione e il controllo dei trigger hardware tramite il protocollo TrGEN.
    /// Permette di programmare, resettare e inviare segnali di trigger su diversi tipi di porte (NeuroScan, Synamaps, GPIO).
    /// </summary>
    public class TrgenClient
    {
        private readonly string ip;
        private readonly int port;
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _worker;
        private readonly BlockingCollection<WorkItem> _queue = new();
        private volatile bool _running;
        private readonly int timeout;
        private TrgenImplementation _impl;
        private int _memoryLength = 32;
        private bool connected = false;
        public bool Connected => connected;

	// nuovo campo
        public LogLevel Verbosity { get; set; } = LogLevel.Warn;

        
        /// <summary>
        /// Crea una nuova istanza di TrgenClient.
        /// </summary>
        /// <param name="ip">Indirizzo IP del dispositivo TrGEN.</param>
        /// <param name="port">Porta di comunicazione.</param>
        /// <param name="timeout">Timeout per la connessione in millisecondi.</param>
        public TrgenClient(string ip = "192.168.123.1", int port = 4242, int timeout = 2000)
        {
            this.ip = ip;
            this.port = port;
            this.timeout = timeout;
        }

        /// <summary>
        /// Crea un oggetto TrgenPort associato a un identificatore specifico.
        /// </summary>
        /// <param name="id">Identificatore del trigger.</param>
        /// <returns>Oggetto TrgenPort.</returns>
        public TrgenPort CreateTrgenPort(int id)
        {
            return new TrgenPort(id, _memoryLength);
        }

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
        /// Invia un pacchetto di dati al dispositivo TrGEN.
        /// </summary>
        /// <param name="packetId">Identificatore del pacchetto.</param>
        /// <param name="payload">Dati opzionali da inviare.</param>
        /// <returns>Risposta del dispositivo come stringa.</returns>
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

        // -------- API pubbliche (stessa firma! ma Thread SAFE) --------
        public void Start() => EnqueuePacket(0x02);
        public void Stop() => EnqueuePacket(0x09);
        public void SetLevel(uint mask) => EnqueuePacket(0x06, new uint[] { mask });
        public void SetGpio(uint mask) => EnqueuePacket(0x03, new uint[] { mask });
        public int GetLevel() => ParseAckValue(EnqueuePacket(0x08).Result, 0x08);
        public int GetStatus() => ParseAckValue(EnqueuePacket(0x05).Result, 0x05);
        public int GetGpio() => ParseAckValue(EnqueuePacket(0x07).Result, 0x07);
        public void SendTrgenMemory(TrgenPort t)
        {
            int id = t.Id;
            int packetId = 0x01 | (id << 24);
            EnqueuePacket(packetId, t.Memory);
        }

        public int RequestImplementation()
        {
            var ack = EnqueuePacket(0x04).Result;
            return ParseAckValue(ack, 0x04);
        }

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

        public void ResetTrigger(TrgenPort t)
        {
            t.SetInstruction(0, InstructionEncoder.End());
            for (int i = 1; i < _memoryLength -1; i++)
                t.SetInstruction(i, InstructionEncoder.NotAdmissible());
            SendTrgenMemory(t);
        }

        public void ProgramDefaultTrigger(TrgenPort t, uint us = 20)
        {
            t.SetInstruction(0, InstructionEncoder.ActiveForUs(us));
            t.SetInstruction(1, InstructionEncoder.UnactiveForUs(3));
            t.SetInstruction(2, InstructionEncoder.End());
            for (int i = 3; i < _memoryLength - 1; i++)
                t.SetInstruction(i, InstructionEncoder.NotAdmissible());
            SendTrgenMemory(t);
        }

        // Implement ResetAll per tipo pin
        public void ResetAll(List<int> ids)
        {
            foreach (var id in ids)
            {
                var tr = CreateTrgenPort(id);
                ResetTrigger(tr);
            }
        }

        public void StartTrigger(int triggerId)
        {
            ResetAll(TriggerPin.AllGpio);
            ResetAll(TriggerPin.AllSa);
            ResetAll(TriggerPin.AllNs);

            var tr = CreateTrgenPort(triggerId);
            ProgramDefaultTrigger(tr);
            Start();
        }

        public void StartTriggerList(List<int> triggerIds)
        {
            ResetAll(TriggerPin.AllGpio);
            ResetAll(TriggerPin.AllSa);
            ResetAll(TriggerPin.AllNs);

            foreach (var id in triggerIds)
            {
                var tr = CreateTrgenPort(id);
                ProgramDefaultTrigger(tr);
            }
            Start();
        }

        /// <summary>
        /// Invia un marker (segnale di trigger) su una o più porte (NeuroScan, Synamps, GPIO).
        /// </summary>
        /// <param name="markerNS">Valore marker per NeuroScan.</param>
        /// <param name="markerSA">Valore marker per Synamps.</param>
        /// <param name="markerGPIO">Valore marker per GPIO.</param>
        /// <param name="LSB">Se true, usa il bit meno significativo come primo pin.</param>
        public void SendMarker(int? markerNS = null, int? markerSA = null, int? markerGPIO = null, bool LSB = false)
        {
            // Se tutti i marker sono null, esci
            if (markerNS == null && markerSA == null && markerGPIO == null)
                return;

            var neuroscanMap = new int[]
            {
                TriggerPin.NS0,
                TriggerPin.NS1,
                TriggerPin.NS2,
                TriggerPin.NS3,
                TriggerPin.NS4,
                TriggerPin.NS5,
                TriggerPin.NS6,
                TriggerPin.NS7
                    };

                    var synampsMap = new int[]
                    {
                TriggerPin.SA0,
                TriggerPin.SA1,
                TriggerPin.SA2,
                TriggerPin.SA3,
                TriggerPin.SA4,
                TriggerPin.SA5,
                TriggerPin.SA6,
                TriggerPin.SA7
                    };

                    var gpioMap = new int[]
                    {
                TriggerPin.GPIO0,
                TriggerPin.GPIO1,
                TriggerPin.GPIO2,
                TriggerPin.GPIO3,
                TriggerPin.GPIO4,
                TriggerPin.GPIO5,
                TriggerPin.GPIO6,
                TriggerPin.GPIO7
            };

            //ResetAllNS();
            //ResetAllSA();
            //ResetAllGPIO();
            //ResetAllTMSO();
            ResetAll(TriggerPin.AllGpio);
            ResetAll(TriggerPin.AllSa);
            ResetAll(TriggerPin.AllNs);
            ResetAll(TriggerPin.AllTMS);

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
        /// Ferma tutti i trigger attivi e resetta lo stato dei pin.
        /// </summary>
        public void StopTrigger()
        {
            Stop();
            //ResetAllTMSO();
            //ResetAllSA();
            //ResetAllGPIO();
            //ResetAllNS();
            ResetAll(TriggerPin.AllGpio);
            ResetAll(TriggerPin.AllSa);
            ResetAll(TriggerPin.AllNs);
            ResetAll(TriggerPin.AllTMS);
        }
    }
}
