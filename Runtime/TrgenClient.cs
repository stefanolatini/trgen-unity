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

        // Costanti per i comandi dei pacchetti
        private const int CMD_PACKET_PROGRAM = 0x01;
        private const int CMD_PACKET_START = 0x02;
        private const int CMD_SET_GPIO = 0x03;
        private const int CMD_REQ_IMPL = 0x04;
        private const int CMD_REQ_STATUS = 0x05;
        private const int CMD_SET_LEVEL = 0x06;
        private const int CMD_REQ_GPIO = 0x07;
        private const int CMD_REQ_LEVEL = 0x08;
        private const int CMD_STOP_TRGEN = 0x09;

        /// <summary>
        /// Sequenza di istruzioni predefinita per TMSO con trigger NE (Negative Edge) su TMSI
        /// </summary>
        public static uint[] TmsoWaitNeSequence => new uint[]
        {
            InstructionEncoder.WaitNE(TrgenPin.TMSI),    // Attende fronte di discesa su TMSI
            InstructionEncoder.ActiveForUs(20),          // 20µs attivo
            InstructionEncoder.UnactiveForUs(20),        // 20µs inattivo
            InstructionEncoder.End()                     // Fine sequenza
        };

        /// <summary>
        /// Sequenza di istruzioni predefinita per TMSO con trigger PE (Positive Edge) su TMSI
        /// </summary>
        public static uint[] TmsoWaitPeSequence => new uint[]
        {
            InstructionEncoder.WaitPE(TrgenPin.TMSI),    // Attende fronte di salita su TMSI
            InstructionEncoder.ActiveForUs(20),          // 20µs attivo
            InstructionEncoder.UnactiveForUs(20),        // 20µs inattivo
            InstructionEncoder.End()                     // Fine sequenza
        };


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
                var portConfig = CreatePortConfigWithMemory(8 + i, $"Synamaps {i}", "SA", $"Synamps trigger port {i}");
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
        /// Configura TMSO per rispondere ai segnali di input su TMSI
        /// </summary>
        /// <param name="ne">Se true, attende fronte negativo; se false, fronte positivo</param>
        public void InputBNCOutput(bool ne = false)
        {
            var instructions = new uint[]
            {
                ne ? InstructionEncoder.WaitNE(TrgenPin.TMSI) : InstructionEncoder.WaitPE(TrgenPin.TMSI),
                InstructionEncoder.ActiveForUs(20),
                InstructionEncoder.UnactiveForUs(20),
                InstructionEncoder.End()
            };
            ProgramPortWithInstructions(TrgenPin.TMSO, instructions);
            Log(LogLevel.Info, "🔧 TMSO configurato per rispondere a TMSI");
        }


        /// <summary>
        /// Configura TMSO per rispondere ai segnali di input su un pin GPIO specifico
        /// </summary>
        /// <param name="ne">Se true, attende fronte negativo; se false, fronte positivo</param>
        /// <param name="gpioId">ID del pin GPIO da utilizzare come input</param>
        public void InputGPIOTriggerTMSOBehaviour(bool ne = false, int gpioId = TrgenPin.GPIO0)
        {
            if (!TrgenPin.AllGpio.Contains(gpioId))
            {
                Log(LogLevel.Error, $"Pin GPIO non valido: {gpioId}. Utilizzare TrgenPin.GPIO0-GPIO7");
                return;
            }

            var instructions = new uint[]
            {
                ne ? InstructionEncoder.WaitNE(gpioId) : InstructionEncoder.WaitPE(gpioId),
                InstructionEncoder.ActiveForUs(20),
                InstructionEncoder.UnactiveForUs(20),
                InstructionEncoder.End()
            };
            ProgramPortWithInstructions(TrgenPin.TMSO, instructions);
            Log(LogLevel.Info, $"🔧 TMSO configurato per rispondere a GPIO{gpioId - TrgenPin.GPIO0}");
        }

        /// <summary>
        /// Configura un pin di output personalizzato per rispondere a un pin di input specifico
        /// </summary>
        /// <param name="inputPortId">ID del pin di input (TMSI o GPIO)</param>
        /// <param name="outputPortId">ID del pin di output</param>
        /// <param name="instructions">Istruzioni personalizzate (opzionale)</param>
        /// <param name="ne">Se true, attende fronte negativo; se false, fronte positivo</param>
        public void InputTriggerCustomPin(int inputPortId, int outputPortId, uint[] instructions = null, bool ne = false)
        {
            // Validazione pin di input
            if (inputPortId != TrgenPin.TMSI && !TrgenPin.AllGpio.Contains(inputPortId))
            {
                Log(LogLevel.Error, $"Pin di input non valido: {inputPortId}. Solo TMSI e GPIO sono supportati");
                return;
            }

            // Validazione pin di output
            if (outputPortId == inputPortId)
            {
                Log(LogLevel.Error, "Il pin di output non può essere uguale al pin di input");
                return;
            }

            if (outputPortId == TrgenPin.TMSI || TrgenPin.AllGpio.Contains(outputPortId))
            {
                Log(LogLevel.Error, $"Pin di output non valido: {outputPortId}. TMSI e GPIO non possono essere usati come output");
                return;
            }

            // Usa istruzioni di default se non fornite
            if (instructions == null || instructions.Length == 0)
            {
                instructions = new uint[]
                {
                    ne ? InstructionEncoder.WaitNE(inputPortId) : InstructionEncoder.WaitPE(inputPortId),
                    InstructionEncoder.ActiveForUs(20),
                    InstructionEncoder.UnactiveForUs(20),
                    InstructionEncoder.End()
                };
            }
            else
            {
                // Validazione istruzioni personalizzate
                ValidateInstructions(instructions);
            }

            ProgramPortWithInstructions(outputPortId, instructions);
            Log(LogLevel.Info, $"🔧 Pin {outputPortId} configurato per rispondere al pin {inputPortId}");
        }

        private void ValidateInstructions(uint[] instructions)
        {
            for (int i = 0; i < instructions.Length; i++)
            {
                if (!InstructionEncoder.IsValidInstruction(instructions[i]))
                {
                    throw new ArgumentException($"Istruzione non valida all'indice {i}: 0x{instructions[i]:X8}");
                }
            }

            // Verifica presenza istruzione End()
            bool hasEndInstruction = Array.Exists(instructions, InstructionEncoder.IsEndInstruction);
            if (!hasEndInstruction)
            {
                Log(LogLevel.Warn, "Aggiunta automatica dell'istruzione End() alla sequenza");
            }
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

            int index = 0;
            // Programma le istruzioni
            for (int i = 0; i < Math.Min(instructions.Length, trgenPort.Memory.Length); i++)
            {
                trgenPort.SetInstruction(i, instructions[i]);
                index++;
            }
            for (int i = index; i < _memoryLength; i++)
            {
                trgenPort.SetInstruction(i, InstructionEncoder.NotAdmissible());
                // Aggiunge l'istruzione di fine programma se c'è spazio
            }
            // Invia la memoria al dispositivo
            SetTrgenMemory(trgenPort);

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
        public void Start() => SendPacket(CMD_PACKET_START);
        public void Stop() => SendPacket(CMD_STOP_TRGEN);

        /// <summary>
        /// Imposta il livello delle porte di output tramite maschera di bit.
        /// </summary>
        /// <param name="mask">Maschera di bit per impostare lo stato delle porte (1=attivo, 0=inattivo).</param>
        public void SetLevel(uint mask) => SendPacket(CMD_SET_LEVEL, new uint[] { mask });

        public void SetGpio(uint mask) => SendPacket(CMD_SET_GPIO, new uint[] { mask });

        /// <summary>
        /// Richiede il livello attuale delle porte di output.
        /// </summary>
        /// <returns>Valore intero rappresentante lo stato corrente delle porte come maschera di bit.</returns>
        public int GetLevel() => ParseAckValue(SendPacket(CMD_REQ_LEVEL), CMD_REQ_LEVEL);

        public int GetStatus() => ParseAckValue(SendPacket(CMD_REQ_STATUS), CMD_REQ_STATUS);
        public int GetGpio() => ParseAckValue(SendPacket(CMD_REQ_GPIO), CMD_REQ_GPIO);

        public void SetTrgenMemory(TrgenPort t)
        {
            int id = t.Id;
            int packetId = CMD_PACKET_PROGRAM | (id << 24);
            SendPacket(packetId, t.Memory);
        }

        public int RequestImplementation()
        {
            var ack = SendPacket(CMD_REQ_IMPL);
            return ParseAckValue(ack, CMD_REQ_IMPL);
        }

        public void ResetTrigger(TrgenPort t)
        {
            t.SetInstruction(0, InstructionEncoder.End());
            for (int i = 1; i < _memoryLength; i++)
                t.SetInstruction(i, InstructionEncoder.NotAdmissible());
            SetTrgenMemory(t);
        }

        public void ResetAllTMS()
        {
            var tout = CreateTrgenPort(TrgenPin.TMSO);
            tout.SetInstruction(0, InstructionEncoder.End());
            for (int i = 1; i < _memoryLength; i++)
                tout.SetInstruction(i, InstructionEncoder.NotAdmissible());
            SetTrgenMemory(tout);
            var tin = CreateTrgenPort(TrgenPin.TMSI);
            tin.SetInstruction(0, InstructionEncoder.End());
            for (int i = 1; i < _memoryLength; i++)
                tin.SetInstruction(i, InstructionEncoder.NotAdmissible());
            SetTrgenMemory(tin);
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
                SetTrgenMemory(sa);
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
                SetTrgenMemory(gpio);
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
                SetTrgenMemory(ns);
            }
        }

        public void ProgramDefaultTrigger(TrgenPort t, uint us)
        {
            t.SetInstruction(0, InstructionEncoder.ActiveForUs(us));
            t.SetInstruction(1, InstructionEncoder.UnactiveForUs(3));
            t.SetInstruction(2, InstructionEncoder.End());
            for (int i = 3; i < _memoryLength; i++)
                t.SetInstruction(i, InstructionEncoder.NotAdmissible());
            SetTrgenMemory(t);
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
        public void SendMarker(int? markerNS = null, int? markerSA = null, int? markerGPIO = null, int? markerTMSO = null, bool LSB = false)
        {
            // Se tutti i marker sono null, esci
            if (markerNS == null && markerSA == null && markerGPIO == null && markerTMSO == null)
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

            var TMSOMap = new int[] {
                TrgenPin.TMSO
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
            ResetAllTMS();
            
            if (markerTMSO != null)
            {   
                if(markerNS < 0)
                    UnityEngine.Debug.LogWarning("[TRGEN] Il marker NS non può essere negativo. Valore fornito: " + markerNS.Value);
                else{
                    // Gestione speciale per TMSO (singolo pin)
                    // NOTA: la mappatura TMSO utilizza un solo pin, quindi non è necessario fare il bitmasking.
                    if(markerTMSO.Value > 1)
                        UnityEngine.Debug.LogWarning("[TRGEN] Il marker TMSO può essere solo 0 o 1. Valore fornito: " + markerTMSO.Value);  
                    var tmsx = CreateTrgenPort(TMSOMap[0]);
                    ProgramDefaultTrigger(tmsx);
                }
            }

            if (markerNS != null)
            {
                if(markerNS < 0)
                    UnityEngine.Debug.LogWarning("[TRGEN] Il marker NS non può essere negativo. Valore fornito: " + markerNS.Value);
                else{
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
                
            }

            if (markerSA != null)
            {
                if(markerSA < 0)
                    UnityEngine.Debug.LogWarning("[TRGEN] Il marker SA non può essere negativo. Valore fornito: " + markerSA.Value);
                else
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
            }

            if (markerGPIO != null)
            {
                if(markerGPIO < 0)
                    UnityEngine.Debug.LogWarning("[TRGEN] Il marker GPIO non può essere negativo. Valore fornito: " + markerGPIO.Value);
                else
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
        public void SendMarker(int? markerNS = null, int? markerSA = null, int? markerGPIO = null, int? markerTMSO = null, bool LSB = false, bool stop = false)
        {
            // Se tutti i marker sono null, esci
            if (markerNS == null && markerSA == null && markerGPIO == null && markerTMSO == null)
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
            ResetAllTMS();

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
            ResetAllTMS();
            ResetAllSA();
            ResetAllGPIO();
            ResetAllNS();
        }

        /// <summary>
        /// Invia un trigger personalizzato basato su un segnale di input.
        /// </summary>
        /// <param name="ne">Se True, attende fronte negativo; se False, fronte positivo</param>
        /// <param name="trgenPinList">Lista di pin TrgenPin da triggerare. Se null, usa BNCO</param>
        /// <param name="inputPin">ID del pin di input (BNCI o GPIO)</param>
        /// <param name="customInstructions">Lista di istruzioni personalizzate (opzionale)</param>
        public void CallbackCustomTrigger(bool ne = false, List<int> trgenPinList = null, int? inputPin = null, uint[] customInstructions = null)
        {
            // Validazione pin di input
            if (!inputPin.HasValue)
            {
                if (Verbosity >= LogLevel.Error)
                    Debug.LogError("❌ Pin di input non specificato");
                return;
            }

            // Validazione lista trigger
            if (trgenPinList != null)
            {
                if (trgenPinList.Count == 0)
                {
                    if (Verbosity >= LogLevel.Error)
                        Debug.LogError("❌ La lista dei trigger non può essere vuota");
                    return;
                }

                // Verifica che BNCI non sia nella lista (è solo input)
                if (trgenPinList.Contains(TrgenPin.BNCI))
                {
                    if (Verbosity >= LogLevel.Error)
                        Debug.LogError("❌ BNCI è solo input e non può essere usato nella lista trigger");
                    return;
                }

                // Verifica conflitto GPIO input/output
                var validGpio = new HashSet<int> { 
                    TrgenPin.GPIO0, TrgenPin.GPIO1, TrgenPin.GPIO2, TrgenPin.GPIO3,
                    TrgenPin.GPIO4, TrgenPin.GPIO5, TrgenPin.GPIO6, TrgenPin.GPIO7
                };

                if (validGpio.Contains(inputPin.Value) && trgenPinList.Contains(inputPin.Value))
                {
                    if (Verbosity >= LogLevel.Error)
                        Debug.LogError("❌ GPIO non può essere usato contemporaneamente come input e output");
                    return;
                }
            }

            // Configurazione GPIO se necessario
            if (IsGpioPin(inputPin.Value))
            {
                ConfigureGpioAsInput(inputPin.Value);
            }

            if (trgenPinList == null)
            {
                // Usa BNCO come default
                var bnco = CreateTrgenPort(TrgenPin.BNCO);
                ConfigureTriggerPort(bnco, inputPin.Value, ne, customInstructions);
                WriteTrgenMemory(bnco);
            }
            else
            {
                // Configura ogni pin della lista
                foreach (var triggerPin in trgenPinList)
                {
                    var trigger = CreateTrgenPort(triggerPin);
                    ProgramDefaultTrigger(trigger);
                    ConfigureTriggerPort(trigger, inputPin.Value, ne, customInstructions);
                    WriteTrgenMemory(trigger);
                }
            }

            if (Verbosity >= LogLevel.Info)
                Debug.Log($"✅ Callback trigger configurato per input pin {inputPin}");
        }

        /// <summary>
        /// Callback per marker su connettori NS, SA e/o GPIO basato su trigger di input.
        /// </summary>
        /// <param name="markerNS">Marker per NeuroScan (0-255)</param>
        /// <param name="markerSA">Marker per Synamps (0-255)</param>
        /// <param name="markerGPIO">Marker per GPIO (0-255)</param>
        /// <param name="inputPin">ID del pin di input (BNCI o GPIO)</param>
        /// <param name="lsb">Se True, usa il bit meno significativo per primo</param>
        /// <param name="ne">Se True, attende fronte negativo; se False, fronte positivo</param>
        public void CallbackMarker(int markerNS = 0, int markerSA = 0, int markerGPIO = 0, int? inputPin = null, bool lsb = false, bool ne = false)
        {
            // Validazione pin di input
            if (!inputPin.HasValue)
            {
                if (Verbosity >= LogLevel.Error)
                    Debug.LogError("❌ Pin di input non specificato");
                return;
            }

            var validGpio = new HashSet<int> { 
                TrgenPin.GPIO0, TrgenPin.GPIO1, TrgenPin.GPIO2, TrgenPin.GPIO3,
                TrgenPin.GPIO4, TrgenPin.GPIO5, TrgenPin.GPIO6, TrgenPin.GPIO7
            };

            // Validazione conflitto GPIO
            if (IsGpioPin(inputPin.Value) && markerGPIO != 0)
            {
                if (Verbosity >= LogLevel.Error)
                    Debug.LogError("❌ GPIO non può essere usato contemporaneamente come input e output");
                return;
            }

            // Validazione pin di input supportati
            if (inputPin.Value != TrgenPin.BNCI && !validGpio.Contains(inputPin.Value))
            {
                if (Verbosity >= LogLevel.Error)
                    Debug.LogError($"❌ Pin di input non valido: {inputPin}. Solo BNCI e GPIO sono supportati");
                return;
            }

            // Verifica che almeno un marker sia specificato
            if (markerNS == 0 && markerSA == 0 && markerGPIO == 0)
            {
                if (Verbosity >= LogLevel.Warn)
                    Debug.LogWarning("⚠️ Nessun marker specificato");
                return;
            }

            // Configurazione GPIO se necessario
            if (IsGpioPin(inputPin.Value))
            {
                ConfigureGpioAsInput(inputPin.Value);
            }

            // Pin mappings
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

            // Reset di tutti i port
            ResetAllTrgenPorts();

            // Converti marker in bitmask
            var maskNS = ConvertToBitmask(markerNS, lsb);
            var maskSA = ConvertToBitmask(markerSA, lsb);
            var maskGPIO = ConvertToBitmask(markerGPIO, lsb);

            // Configura NeuroScan markers
            ConfigureMarkersForPorts(maskNS, neuroscanMap, inputPin.Value, ne, "NS");
            
            // Configura Synamps markers
            ConfigureMarkersForPorts(maskSA, synampsMap, inputPin.Value, ne, "SA");
            
            // Configura GPIO markers
            ConfigureMarkersForPorts(maskGPIO, gpioMap, inputPin.Value, ne, "GPIO");

            if (Verbosity >= LogLevel.Info)
                Debug.Log($"✅ Callback markers configurati per input pin {inputPin}");
        }

        // Helper methods

        private void ConfigureTriggerPort(TrgenPort port, int inputPin, bool ne, uint[] customInstructions)
        {
            // Configura istruzione di attesa
            if (ne)
                port.SetInstruction(0, InstructionEncoder.WaitNE(inputPin));
            else
                port.SetInstruction(0, InstructionEncoder.WaitPE(inputPin));

            if (customInstructions == null || customInstructions.Length == 0)
            {
                // Istruzioni default
                port.SetInstruction(1, InstructionEncoder.ActiveForUs(20));
                port.SetInstruction(2, InstructionEncoder.UnactiveForUs(20));
                port.SetInstruction(3, InstructionEncoder.End());
                
                // Riempi il resto con istruzioni non ammissibili
                for (int i = 4; i < MEMORY_LENGTH; i++)
                {
                    port.SetInstruction(i, InstructionEncoder.NotAdmissible());
                }
            }
            else
            {
                // Usa istruzioni personalizzate
                for (int i = 0; i < customInstructions.Length && i + 1 < MEMORY_LENGTH; i++)
                {
                    port.SetInstruction(i + 1, customInstructions[i]);
                }
                
                // Riempi il resto se necessario
                for (int i = customInstructions.Length + 1; i < MEMORY_LENGTH; i++)
                {
                    port.SetInstruction(i, InstructionEncoder.NotAdmissible());
                }
            }
        }

        private bool[] ConvertToBitmask(int marker, bool lsb)
        {
            var binaryStr = Convert.ToString(marker, 2).PadLeft(8, '0');
            var mask = new bool[8];
            
            for (int i = 0; i < 8; i++)
            {
                mask[i] = binaryStr[i] == '1';
            }
            
            if (lsb)
            {
                Array.Reverse(mask);
            }
            
            return mask;
        }

        private void ConfigureMarkersForPorts(bool[] mask, int[] portMap, int inputPin, bool ne, string portType)
        {
            for (int idx = 0; idx < mask.Length; idx++)
            {
                if (mask[idx])
                {
                    var port = CreateTrgenPort(portMap[idx]);
                    
                    // Configura istruzione di attesa
                    if (ne)
                        port.SetInstruction(0, InstructionEncoder.WaitNE(inputPin));
                    else
                        port.SetInstruction(0, InstructionEncoder.WaitPE(inputPin));
                    
                    // Istruzioni standard per marker
                    port.SetInstruction(1, InstructionEncoder.ActiveForUs(20));
                    port.SetInstruction(2, InstructionEncoder.UnactiveForUs(20));
                    port.SetInstruction(3, InstructionEncoder.End());
                    
                    // Riempi il resto con istruzioni non ammissibili
                    for (int i = 4; i < MEMORY_LENGTH; i++)
                    {
                        port.SetInstruction(i, InstructionEncoder.NotAdmissible());
                    }
                    
                    // Programma il trigger
                    WriteTrgenMemory(port);
                    
                    if (Verbosity >= LogLevel.Debug)
                        Debug.Log($"🔧 Configurato {portType}{idx} per marker callback");
                }
            }
        }

        private bool IsGpioPin(int pin)
        {
            return pin >= TrgenPin.GPIO0 && pin <= TrgenPin.GPIO7;
        }

        private void ConfigureGpioAsInput(int gpioPin)
        {
            // Implementa la configurazione GPIO come input
            // Questa implementazione dipende dal protocollo hardware specifico
            if (Verbosity >= LogLevel.Debug)
                Debug.Log($"🔧 Configurato GPIO{gpioPin - TrgenPin.GPIO0} come input");
        }

        private void ResetAllTrgenPorts()
        {
            // Reset di tutti i port disponibili
            ResetAll(TrgenPin.AllNs);
            ResetAll(TrgenPin.AllSa);
            ResetAll(TrgenPin.AllGpio);
            
            if (Verbosity >= LogLevel.Debug)
                Debug.Log("🔄 Reset di tutti i TrgenPorts completato");
        }
        private void WriteTrgenMemory(TrgenPort port)
        {
            SetTrgenMemory(port);
            if (Verbosity >= LogLevel.Debug)
                Debug.Log($"💾 Memoria TrgenPort ID {port.Id} scritta");
        }


    }

    // duplicate types (Trigger / TrgenImplementation) removed — project contains canonical implementations in other files
}
