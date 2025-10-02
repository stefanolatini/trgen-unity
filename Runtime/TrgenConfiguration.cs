using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Trgen
{
    /// <summary>
    /// Rappresenta una configurazione esportabile/importabile per i trigger TrGEN.
    /// Contiene tutti i parametri necessari per salvare e ripristinare lo stato dei trigger.
    /// </summary>
    [Serializable]
    public class TrgenConfiguration
    {
        /// <summary>
        /// Informazioni generali sulla configurazione
        /// </summary>
        public ConfigurationMetadata Metadata { get; set; } = new ConfigurationMetadata();

        /// <summary>
        /// Configurazioni di default per tutti i trigger
        /// </summary>
        public DefaultSettings Defaults { get; set; } = new DefaultSettings();

        /// <summary>
        /// Configurazioni specifiche per ogni porta di trigger
        /// </summary>
        public Dictionary<string, TriggerPortConfig> TriggerPorts { get; set; } = new Dictionary<string, TriggerPortConfig>();

        /// <summary>
        /// Configurazioni di rete per la connessione al dispositivo TrGEN
        /// </summary>
        public NetworkSettings Network { get; set; } = new NetworkSettings();
    }

    /// <summary>
    /// Metadati del file di configurazione
    /// </summary>
    [Serializable]
    public class ConfigurationMetadata
    {
        /// <summary>
        /// Versione del formato di configurazione
        /// </summary>
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// Data di creazione della configurazione
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Data dell'ultima modifica
        /// </summary>
        public DateTime ModifiedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Descrizione opzionale della configurazione
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// Nome del progetto/esperimento associato
        /// </summary>
        public string ProjectName { get; set; } = "";

        /// <summary>
        /// Autore della configurazione
        /// </summary>
        public string Author { get; set; } = "";
    }

    /// <summary>
    /// Impostazioni di default per tutti i trigger
    /// </summary>
    [Serializable]
    public class DefaultSettings
    {
        /// <summary>
        /// Durata standard del trigger in microsecondi
        /// </summary>
        public uint DefaultTriggerDurationUs { get; set; } = 40;

        /// <summary>
        /// Livello di verbosità di default per il logging
        /// </summary>
        public string DefaultLogLevel { get; set; } = "Warn";

        /// <summary>
        /// Timeout di connessione di default in millisecondi
        /// </summary>
        public int DefaultTimeoutMs { get; set; } = 2000;

        /// <summary>
        /// Se abilitare il reset automatico dopo l'invio del trigger
        /// </summary>
        public bool AutoResetEnabled { get; set; } = true;

        /// <summary>
        /// Delay del reset automatico in microsecondi
        /// </summary>
        public uint AutoResetDelayUs { get; set; } = 100;
    }

    /// <summary>
    /// Configurazione specifica per una porta di trigger
    /// </summary>
    [Serializable]
    public class TriggerPortConfig
    {
        /// <summary>
        /// ID della porta (deve corrispondere agli ID in TrgenPin)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nome descrittivo della porta
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Tipo di porta (NS, SA, GPIO, TMS)
        /// </summary>
        public string Type { get; set; } = "";

        /// <summary>
        /// Se la porta è abilitata
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Durata specifica del trigger per questa porta (se diversa dal default)
        /// </summary>
        public uint? CustomDurationUs { get; set; } = null;

        /// <summary>
        /// Array completo della memoria delle istruzioni programmate per questa porta.
        /// Rappresenta l'intero contenuto della memoria del TrgenPort (tipicamente 32 elementi).
        /// Ogni elemento è un'istruzione codificata che definisce il comportamento del trigger.
        /// </summary>
        public uint[] MemoryInstructions { get; set; } = new uint[0];

        /// <summary>
        /// Lunghezza della memoria per questa porta (numero massimo di istruzioni)
        /// </summary>
        public int MemoryLength { get; set; } = 32;

        /// <summary>
        /// Indice dell'ultima istruzione valida nella memoria (-1 se vuota)
        /// </summary>
        public int LastInstructionIndex { get; set; } = -1;

        /// <summary>
        /// Stato della programmazione della porta
        /// </summary>
        public PortProgrammingState ProgrammingState { get; set; } = PortProgrammingState.NotProgrammed;

        /// <summary>
        /// Timestamp dell'ultima programmazione di questa porta
        /// </summary>
        public DateTime LastProgrammedAt { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Note o descrizione specifica per questa porta
        /// </summary>
        public string Notes { get; set; } = "";

        /// <summary>
        /// Configurazioni personalizzate aggiuntive
        /// </summary>
        public Dictionary<string, object> CustomSettings { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Costruttore di default
        /// </summary>
        public TriggerPortConfig()
        {
            MemoryInstructions = new uint[32]; // Default memory size
            MemoryLength = 32;
        }

        /// <summary>
        /// Costruttore con lunghezza memoria specifica
        /// </summary>
        /// <param name="memoryLength">Lunghezza della memoria per questa porta</param>
        public TriggerPortConfig(int memoryLength)
        {
            MemoryLength = memoryLength;
            MemoryInstructions = new uint[memoryLength];
        }

        /// <summary>
        /// Imposta il contenuto completo della memoria da un TrgenPort
        /// </summary>
        /// <param name="trgenPort">TrgenPort da cui copiare la memoria</param>
        public void SetMemoryFromTrgenPort(TrgenPort trgenPort)
        {
            if (trgenPort == null)
                throw new ArgumentNullException(nameof(trgenPort));

            Id = trgenPort.Id;
            Type = trgenPort.Type.ToString();
            MemoryLength = trgenPort.Memory.Length;
            MemoryInstructions = new uint[MemoryLength];
            
            // Copia l'intero array di memoria
            Array.Copy(trgenPort.Memory, MemoryInstructions, MemoryLength);
            
            // Trova l'ultima istruzione valida
            FindLastInstructionIndex();
            
            // Aggiorna stato
            ProgrammingState = LastInstructionIndex >= 0 ? PortProgrammingState.Programmed : PortProgrammingState.NotProgrammed;
            LastProgrammedAt = DateTime.Now;
        }

        /// <summary>
        /// Applica questa configurazione a un TrgenPort
        /// </summary>
        /// <param name="trgenPort">TrgenPort su cui applicare la configurazione</param>
        public void ApplyToTrgenPort(TrgenPort trgenPort)
        {
            if (trgenPort == null)
                throw new ArgumentNullException(nameof(trgenPort));

            if (trgenPort.Id != Id)
                throw new ArgumentException($"ID mismatch: TrgenPort ha ID {trgenPort.Id}, configurazione ha ID {Id}");

            // Applica le istruzioni di memoria
            int copyLength = Math.Min(MemoryInstructions.Length, trgenPort.Memory.Length);
            for (int i = 0; i < copyLength; i++)
            {
                trgenPort.SetInstruction(i, MemoryInstructions[i]);
            }
        }

        /// <summary>
        /// Trova l'indice dell'ultima istruzione valida nella memoria
        /// </summary>
        private void FindLastInstructionIndex()
        {
            LastInstructionIndex = -1;
            for (int i = MemoryInstructions.Length - 1; i >= 0; i--)
            {
                if (MemoryInstructions[i] != 0)
                {
                    LastInstructionIndex = i;
                    break;
                }
            }
        }

        /// <summary>
        /// Verifica se la porta ha istruzioni programmate
        /// </summary>
        /// <returns>True se ha istruzioni programmate</returns>
        public bool HasProgrammedInstructions()
        {
            return LastInstructionIndex >= 0;
        }

        /// <summary>
        /// Ottiene il numero di istruzioni programmate
        /// </summary>
        /// <returns>Numero di istruzioni valide programmate</returns>
        public int GetInstructionCount()
        {
            return HasProgrammedInstructions() ? LastInstructionIndex + 1 : 0;
        }

        /// <summary>
        /// Ottiene una rappresentazione string delle istruzioni programmate
        /// </summary>
        /// <returns>String representation delle istruzioni</returns>
        public string GetInstructionsString()
        {
            if (!HasProgrammedInstructions())
                return "No instructions programmed";

            var instructions = new List<string>();
            for (int i = 0; i <= LastInstructionIndex; i++)
            {
                instructions.Add($"[{i}]: 0x{MemoryInstructions[i]:X8}");
            }
            return string.Join(", ", instructions);
        }
    }

    /// <summary>
    /// Stato della programmazione di una porta
    /// </summary>
    [Serializable]
    public enum PortProgrammingState
    {
        /// <summary>
        /// Porta non programmata (memoria vuota)
        /// </summary>
        NotProgrammed,
        
        /// <summary>
        /// Porta programmata con istruzioni valide
        /// </summary>
        Programmed,
        
        /// <summary>
        /// Porta resettata (memoria azzerata)
        /// </summary>
        Reset,
        
        /// <summary>
        /// Stato sconosciuto o corrotto
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Configurazioni di rete per la connessione
    /// </summary>
    [Serializable]
    public class NetworkSettings
    {
        /// <summary>
        /// Indirizzo IP del dispositivo TrGEN
        /// </summary>
        public string IpAddress { get; set; } = "192.168.123.1";

        /// <summary>
        /// Porta di comunicazione
        /// </summary>
        public int Port { get; set; } = 4242;

        /// <summary>
        /// Timeout di connessione in millisecondi
        /// </summary>
        public int TimeoutMs { get; set; } = 2000;
    }
}