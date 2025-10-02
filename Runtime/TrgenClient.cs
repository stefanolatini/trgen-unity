using System;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;

namespace Trgen
{
    /// <summary>
    /// Gestisce la connessione, la comunicazione e il controllo dei trigger hardware tramite il protocollo TrGEN.
    /// Permette di programmare, resettare e inviare segnali di trigger su diversi tipi di porte (NeuroScan, Synamaps, GPIO).
    /// </summary>
    public class TrgenClient
    {
        private readonly string ip;
        private readonly int port;
        private readonly int timeout;
        private TrgenImplementation _impl;


        #region Configuration Export/Import

        /// <summary>
        /// Esporta la configurazione attuale del client in un file .trgen
        /// </summary>
        /// <param name="filePath">Percorso del file (senza estensione o con .trgen)</param>
        /// <param name="projectName">Nome del progetto/esperimento</param>
        /// <param name="description">Descrizione della configurazione</param>
        /// <param name="author">Autore della configurazione</param>
        /// <returns>Percorso completo del file salvato</returns>
        public string ExportConfiguration(string filePath, string projectName = "", string description = "", string author = "")
        {
            return TrgenConfigurationManager.ExportConfiguration(this, filePath, projectName, description, author);
        }

        /// <summary>
        /// Importa una configurazione da un file .trgen e la applica al client corrente
        /// </summary>
        /// <param name="filePath">Percorso del file .trgen da importare</param>
        /// <param name="applyNetworkSettings">Se applicare anche le impostazioni di rete (richiede riconnessione)</param>
        /// <param name="programPorts">Se programmare effettivamente le porte sul dispositivo hardware</param>
        /// <returns>Configurazione importata</returns>
        public TrgenConfiguration ImportConfiguration(string filePath, bool applyNetworkSettings = false, bool programPorts = true)
        {
            return TrgenConfigurationManager.ImportConfiguration(this, filePath, applyNetworkSettings, programPorts);
        }

        /// <summary>
        /// Crea una nuova configurazione basata sullo stato attuale del client
        /// </summary>
        /// <param name="projectName">Nome del progetto</param>
        /// <param name="description">Descrizione</param>
        /// <param name="author">Autore</param>
        /// <returns>Oggetto configurazione</returns>
        public TrgenConfiguration CreateConfiguration(string projectName = "", string description = "", string author = "")
        {
            var config = new TrgenConfiguration();
            
            // Metadati
            config.Metadata.ProjectName = projectName;
            config.Metadata.Description = description;
            config.Metadata.Author = author;
            config.Metadata.CreatedAt = DateTime.Now;
            config.Metadata.ModifiedAt = DateTime.Now;

            // Impostazioni default dal client corrente
            config.Defaults.DefaultTriggerDurationUs = _defaultTriggerDurationUs;
            config.Defaults.DefaultLogLevel = Verbosity.ToString();
            config.Defaults.DefaultTimeoutMs = timeout;

            // Impostazioni di rete dal client corrente
            config.Network.IpAddress = ip;
            config.Network.Port = port;
            config.Network.TimeoutMs = timeout;

            // Configurazioni delle porte (tutte le porte disponibili)
            CreateDefaultPortConfigurations(config);

            return config;
        }

        /// <summary>
        /// Applica una configurazione caricata al client corrente
        /// </summary>
        /// <param name="config">Configurazione da applicare</param>
        /// <param name="applyNetworkSettings">Se applicare le impostazioni di rete</param>
        public void ApplyConfiguration(TrgenConfiguration config, bool applyNetworkSettings = false)
        {
            // Applica impostazioni default
            SetDefaultDuration(config.Defaults.DefaultTriggerDurationUs);
            
            // Applica livello di verbosità
            if (Enum.TryParse<LogLevel>(config.Defaults.DefaultLogLevel, out var logLevel))
            {
                Verbosity = logLevel;
            }

            if (applyNetworkSettings)
            {
                UnityEngine.Debug.LogWarning("[TRGEN] Le impostazioni di rete richiedono la creazione di un nuovo client. Utilizzare CreateClientFromConfiguration() invece.");
            }

            UnityEngine.Debug.Log($"[TRGEN] Configurazione '{config.Metadata.ProjectName}' applicata con successo");
        }

        /// <summary>
        /// Crea un nuovo client TrGEN da una configurazione
        /// </summary>
        /// <param name="config">Configurazione da utilizzare</param>
        /// <returns>Nuovo client configurato</returns>
        public static TrgenClient CreateClientFromConfiguration(TrgenConfiguration config)
        {
            var client = new TrgenClient(
                config.Network.IpAddress,
                config.Network.Port,
                config.Network.TimeoutMs
            );

            client.SetDefaultDuration(config.Defaults.DefaultTriggerDurationUs);
            
            if (Enum.TryParse<LogLevel>(config.Defaults.DefaultLogLevel, out var logLevel))
            {
                client.Verbosity = logLevel;
            }

            return client;
        }

        private void CreateDefaultPortConfigurations(TrgenConfiguration config)
        {
            // NeuroScan Ports (NS0-NS7) - Con memoria attuale
            for (int i = 0; i <= 7; i++)
            {
                var portConfig = CreatePortConfigWithMemory(i, $"NeuroScan {i}", "NS", $"NeuroScan trigger port {i}");
                config.TriggerPorts[$"NS{i}"] = portConfig;
            }

            // Synamps Ports (SA0-SA7) - Con memoria attuale  
            for (int i = 0; i <= 7; i++)
            {
                var portConfig = CreatePortConfigWithMemory(8 + i, $"Synamps {i}", "SA", $"Synamps trigger port {i}");
                config.TriggerPorts[$"SA{i}"] = portConfig;
            }

            // TMS Ports - Con memoria attuale
            config.TriggerPorts["TMSO"] = CreatePortConfigWithMemory(16, "TMS Output", "TMS", "Transcranial Magnetic Stimulation Output");
            config.TriggerPorts["TMSI"] = CreatePortConfigWithMemory(17, "TMS Input", "TMS", "Transcranial Magnetic Stimulation Input");

            // GPIO Ports (GPIO0-GPIO7) - Con memoria attuale
            for (int i = 0; i <= 7; i++)
            {
                var portConfig = CreatePortConfigWithMemory(18 + i, $"GPIO {i}", "GPIO", $"General Purpose Input/Output {i}");
                config.TriggerPorts[$"GPIO{i}"] = portConfig;
            }
        }

        /// <summary>
        /// Crea una configurazione di porta con la memoria attualmente programmata
        /// </summary>
        /// <param name="portId">ID della porta</param>
        /// <param name="portName">Nome della porta</param>
        /// <param name="portType">Tipo di porta</param>
        /// <param name="notes">Note descrittive</param>
        /// <returns>Configurazione della porta con memoria</returns>
        private TriggerPortConfig CreatePortConfigWithMemory(int portId, string portName, string portType, string notes)
        {
            var portConfig = new TriggerPortConfig(_memoryLength)
            {
                Id = portId,
                Name = portName,
                Type = portType,
                Enabled = true,
                Notes = notes
            };

            try
            {
                // Crea TrgenPort per ottenere lo stato attuale della memoria
                var trgenPort = CreateTrgenPort(portId);
                portConfig.SetMemoryFromTrgenPort(trgenPort);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[TRGEN] Impossibile leggere memoria per porta {portId}: {ex.Message}");
                // Memoria vuota di fallback
                portConfig.MemoryInstructions = new uint[_memoryLength];
                portConfig.ProgrammingState = PortProgrammingState.Unknown;
            }

            return portConfig;
        }

        /// <summary>
        /// Programma una porta con istruzioni specifiche e aggiorna la configurazione
        /// </summary>
        /// <param name="portId">ID della porta da programmare</param>
        /// <param name="instructions">Array di istruzioni da programmare</param>
        /// <returns>TrgenPort programmato</returns>
        public TrgenPort ProgramPortWithInstructions(int portId, uint[] instructions)
        {
            if (instructions == null)
                throw new ArgumentNullException(nameof(instructions));

            var trgenPort = CreateTrgenPort(portId);
            
            // Programma le istruzioni
            for (int i = 0; i < Math.Min(instructions.Length, trgenPort.Memory.Length); i++)
            {
                trgenPort.SetInstruction(i, instructions[i]);
            }

            // Invia la memoria al dispositivo
            SendTrgenMemory(trgenPort);

            UnityEngine.Debug.Log($"[TRGEN] Porta {portId} programmata con {instructions.Length} istruzioni");
            return trgenPort;
        }

        /// <summary>
        /// Ottiene la memoria attuale di una porta specifica
        /// </summary>
        /// <param name="portId">ID della porta</param>
        /// <returns>Array della memoria della porta</returns>
        public uint[] GetPortMemory(int portId)
        {
            var trgenPort = CreateTrgenPort(portId);
            var memory = new uint[trgenPort.Memory.Length];
            Array.Copy(trgenPort.Memory, memory, trgenPort.Memory.Length);
            return memory;
        }

        /// <summary>
        /// Crea un snapshot completo dello stato di memoria di tutte le porte
        /// </summary>
        /// <returns>Dizionario con lo stato di memoria di tutte le porte</returns>
        public Dictionary<string, uint[]> CreateMemorySnapshot()
        {
            var snapshot = new Dictionary<string, uint[]>();

            // NeuroScan
            for (int i = 0; i <= 7; i++)
            {
                snapshot[$"NS{i}"] = GetPortMemory(i);
            }

            // Synamps
            for (int i = 0; i <= 7; i++)
            {
                snapshot[$"SA{i}"] = GetPortMemory(8 + i);
            }

            // TMS
            snapshot["TMSO"] = GetPortMemory(16);
            snapshot["TMSI"] = GetPortMemory(17);

            // GPIO
            for (int i = 0; i <= 7; i++)
            {
                snapshot[$"GPIO{i}"] = GetPortMemory(18 + i);
            }

            return snapshot;
        }

        #endregion

        private int _memoryLength = 32;
        private bool connected = false;
        private uint _defaultTriggerDurationUs = 40; // Durata standard del trigger in microsecondi
        public bool Connected => connected;


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
        /// Crea una nuova istanza di TriggerClient.
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
        /// Imposta la durata standard del trigger in microsecondi.
        /// Questa durata verrà utilizzata da ProgramDefaultTrigger quando non viene specificata esplicitamente.
        /// </summary>
        /// <param name="durationUs">Durata del trigger in microsecondi (consigliato: 5-100µs).</param>
        public void SetDefaultDuration(uint durationUs)
        {
            _defaultTriggerDurationUs = durationUs;
        }

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
        /// Crea un oggetto TrgenPort associato a un identificatore specifico.
        /// </summary>
        /// <param name="id">Identificatore del trigger.</param>
        /// <returns>Oggetto TrgenPort.</returns>
        public TrgenPort CreateTrgenPort(int id)
        {
            return new TrgenPort(id, _memoryLength);
        }

        /// <summary>
        /// Tenta di connettersi al dispositivo TrGEN e aggiorna la configurazione interna.
        /// </summary>
        public void Connect()
        {
            try
            {
                int packed = RequestImplementation();
                _impl = new TrgenImplementation(packed);
                UnityEngine.Debug.Log(_impl.MemoryLength);
                _memoryLength = _impl.MemoryLength;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[TRGEN] Connect failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Verifica se il server TrGEN è raggiungibile.
        /// </summary>
        /// <returns>True se disponibile, altrimenti False.</returns>
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
            Console.WriteLine($"{label}: {hex}");
            UnityEngine.Debug.Log($"{label}: {hex}");
        }

        /*private byte[] BuildPayload(uint[] words)
        {
            byte[] payload = new byte[words.Length * 4];
            for (int i = 0; i < words.Length; i++)
            {
                byte[] word = BitConverter.GetBytes(words[i]);
                Buffer.BlockCopy(word, 0, payload, i * 4, 4);
            }
            return payload;
        }*/

        // Commands
        public void Start() => SendPacket(0x02);
        public void Stop() => SendPacket(0x09);
        public void SetLevel(uint mask) => SendPacket(0x06, new uint[] { mask });
        public void SetGpio(uint mask) => SendPacket(0x03, new uint[] { mask });
        public int GetLevel() => ParseAckValue(SendPacket(0x08), 0x08);
        public int GetStatus() => ParseAckValue(SendPacket(0x05), 0x05);
        public int GetGpio() => ParseAckValue(SendPacket(0x07), 0x07);

        public void SendTrgenMemory(TrgenPort t)
        {
            int id = t.Id;
            int packetId = 0x01 | (id << 24);
            SendPacket(packetId, t.Memory);
        }

        public int RequestImplementation()
        {
            var ack = SendPacket(0x04);
            return ParseAckValue(ack, 0x04);
        }

        public void ResetTrigger(TrgenPort t)
        {
            t.SetInstruction(0, InstructionEncoder.End());
            for (int i = 1; i < _memoryLength; i++)
                t.SetInstruction(i, InstructionEncoder.NotAdmissible());
            SendTrgenMemory(t);
        }

        public void ResetAllTMSO()
        {
            var t = CreateTrgenPort(TrgenPin.TMSO);
            t.SetInstruction(0, InstructionEncoder.End());
            for (int i = 1; i < _memoryLength; i++)
                t.SetInstruction(i, InstructionEncoder.NotAdmissible());
            SendTrgenMemory(t);
        }

        public void ResetAllSA()
        {
            int[] sinampMap = new int[] {
                TrgenPin.SA0,
                TrgenPin.SA1,
                TrgenPin.SA2,
                TrgenPin.SA3,
                TrgenPin.SA4,
                TrgenPin.SA5,
                TrgenPin.SA6,
                TrgenPin.SA7,
            };
            for (int i = 0; i < sinampMap.Length; i++)
            {
                var sa = CreateTrgenPort(sinampMap[i]);
                sa.SetInstruction(0, InstructionEncoder.End());
                for (int j = 1; j < _memoryLength; j++)
                    sa.SetInstruction(j, InstructionEncoder.NotAdmissible());
                SendTrgenMemory(sa);
            }
        }

        public void ResetAllGPIO()
        {
            int[] gpioMap = new int[] {
                TrgenPin.GPIO0,
                TrgenPin.GPIO1,
                TrgenPin.GPIO2,
                TrgenPin.GPIO3,
                TrgenPin.GPIO4,
                TrgenPin.GPIO5,
                TrgenPin.GPIO6,
                TrgenPin.GPIO7,
            };
            for (int i = 0; i < gpioMap.Length; i++)
            {
                var gpio = CreateTrgenPort(gpioMap[i]);
                gpio.SetInstruction(0, InstructionEncoder.End());
                for (int j = 1; j < _memoryLength; j++)
                    gpio.SetInstruction(j, InstructionEncoder.NotAdmissible());
                SendTrgenMemory(gpio);
            }
        }
        public void ResetAllNS()
        {
            int[] neuroscanMap = new int[] {
                TrgenPin.NS0,
                TrgenPin.NS1,
                TrgenPin.NS2,
                TrgenPin.NS3,
                TrgenPin.NS4,
                TrgenPin.NS5,
                TrgenPin.NS6,
                TrgenPin.NS7,
            };
            for (int i = 0; i < neuroscanMap.Length; i++)
            {
                var ns = CreateTrgenPort(neuroscanMap[i]);
                ns.SetInstruction(0, InstructionEncoder.End());
                for (int j = 1; j < _memoryLength; j++)
                    ns.SetInstruction(j, InstructionEncoder.NotAdmissible());
                SendTrgenMemory(ns);
            }
        }

        public void ProgramDefaultTrigger(TrgenPort t, uint us)
        {
            t.SetInstruction(0, InstructionEncoder.ActiveForUs(us));
            t.SetInstruction(1, InstructionEncoder.UnactiveForUs(3));
            t.SetInstruction(2, InstructionEncoder.End());
            for (int i = 3; i < _memoryLength; i++)
                t.SetInstruction(i, InstructionEncoder.NotAdmissible());
            SendTrgenMemory(t);
        }

        /// <summary>
        /// Programma un trigger con la durata standard configurata tramite SetDefaultDuration.
        /// </summary>
        /// <param name="t">Porta del trigger da programmare.</param>
        public void ProgramDefaultTrigger(TrgenPort t)
        {
            ProgramDefaultTrigger(t, _defaultTriggerDurationUs);
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
            ResetAll(TrgenPin.AllGpio);
            ResetAll(TrgenPin.AllSa);
            ResetAll(TrgenPin.AllNs);

            var tr = CreateTrgenPort(triggerId);
            ProgramDefaultTrigger(tr);
            Start();
        }

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

            var neuroscanMap = new int[] {
                TrgenPin.NS0,
                TrgenPin.NS1,
                TrgenPin.NS2,
                TrgenPin.NS3,
                TrgenPin.NS4,
                TrgenPin.NS5,
                TrgenPin.NS6,
                TrgenPin.NS7
            };

            var synampsMap = new int[] {
                TrgenPin.SA0,
                TrgenPin.SA1,
                TrgenPin.SA2,
                TrgenPin.SA3,
                TrgenPin.SA4,
                TrgenPin.SA5,
                TrgenPin.SA6,
                TrgenPin.SA7
            };

            var gpioMap = new int[] {
                TrgenPin.GPIO0,
                TrgenPin.GPIO1,
                TrgenPin.GPIO2,
                TrgenPin.GPIO3,
                TrgenPin.GPIO4,
                TrgenPin.GPIO5,
                TrgenPin.GPIO6,
                TrgenPin.GPIO7
            };

            ResetAllNS();
            ResetAllSA();
            ResetAllGPIO();
            ResetAllTMSO();

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
        /// Invia un marker (segnale di trigger) su una o più porte (NeuroScan, Synamps, GPIO) con opzione di stop automatico.
        /// </summary>
        /// <param name="markerNS">Valore marker per NeuroScan.</param>
        /// <param name="markerSA">Valore marker per Synamps.</param>
        /// <param name="markerGPIO">Valore marker per GPIO.</param>
        /// <param name="LSB">Se true, usa il bit meno significativo come primo pin.</param>
        /// <param name="stop">Se true, ferma automaticamente i trigger dopo l'invio.</param>
        public void SendMarker(int? markerNS = null, int? markerSA = null, int? markerGPIO = null, bool LSB = false, bool stop = false)
        {
            // Se tutti i marker sono null, esci
            if (markerNS == null && markerSA == null && markerGPIO == null)
                return;

            var neuroscanMap = new int[] {
                TrgenPin.NS0, TrgenPin.NS1, TrgenPin.NS2, TrgenPin.NS3,
                TrgenPin.NS4, TrgenPin.NS5, TrgenPin.NS6, TrgenPin.NS7
            };

            var synampsMap = new int[] {
                TrgenPin.SA0, TrgenPin.SA1, TrgenPin.SA2, TrgenPin.SA3,
                TrgenPin.SA4, TrgenPin.SA5, TrgenPin.SA6, TrgenPin.SA7
            };

            var gpioMap = new int[] {
                TrgenPin.GPIO0, TrgenPin.GPIO1, TrgenPin.GPIO2, TrgenPin.GPIO3,
                TrgenPin.GPIO4, TrgenPin.GPIO5, TrgenPin.GPIO6, TrgenPin.GPIO7
            };

            // Reset di tutti i trigger
            ResetAllNS();
            ResetAllSA();
            ResetAllGPIO();
            ResetAllTMSO();

            // Programma NeuroScan triggers
            if (markerNS != null && markerNS.Value != 0)
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

            // Programma Synamps triggers
            if (markerSA != null && markerSA.Value != 0)
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

            // Programma GPIO triggers
            if (markerGPIO != null && markerGPIO.Value != 0)
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

            // Stop automatico se richiesto
            if (stop)
            {
                Stop();
            }
        }
        /// <summary>
        /// Ferma tutti i trigger attivi e resetta lo stato dei pin.
        /// </summary>
        public void StopTrigger()
        {
            Stop();
            ResetAllTMSO();
            ResetAllSA();
            ResetAllGPIO();
            ResetAllNS();
        }
    }

    // duplicate types (Trigger / TrgenImplementation) removed — project contains canonical implementations in other files
}
