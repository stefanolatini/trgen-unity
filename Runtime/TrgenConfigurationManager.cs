using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Trgen
{
    /// <summary>
    /// Gestisce l'esportazione e l'importazione delle configurazioni TrGEN in formato JSON.
    /// Supporta file con estensione personalizzata .trgen.
    /// Utilizza JSON nativo di Unity per massima compatibilità senza dipendenze esterne.
    /// </summary>
    public static class TrgenConfigurationManager
    {
        /// <summary>
        /// Esporta la configurazione attuale del client TrGEN in un file .trgen (formato JSON)
        /// </summary>
        /// <param name="client">Client TrGEN da cui esportare la configurazione</param>
        /// <param name="filePath">Percorso del file (senza estensione o con .trgen)</param>
        /// <param name="projectName">Nome del progetto/esperimento</param>
        /// <param name="description">Descrizione della configurazione</param>
        /// <param name="author">Autore della configurazione</param>
        /// <returns>Percorso completo del file salvato</returns>
        public static string ExportConfiguration(TrgenClient client, string filePath, 
            string projectName = "", string description = "", string author = "")
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            // Assicurati che il file abbia l'estensione .trgen
            string finalPath = EnsureTrgenExtension(filePath);

            var config = CreateConfigurationFromClient(client, projectName, description, author);
            
            try
            {
                // Crea la directory se non esiste
                string directory = Path.GetDirectoryName(finalPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Serializza in JSON (più leggibile con pretty print)
                string jsonContent = JsonUtility.ToJson(config, true);
                
                // Aggiungi header personalizzato
                string header = GenerateFileHeader(config);
                string finalContent = header + jsonContent;

                // Salva il file
                File.WriteAllText(finalPath, finalContent);

                Debug.Log($"[TRGEN] Configurazione esportata con successo: {finalPath}");
                return finalPath;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TRGEN] Errore durante l'esportazione: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Importa una configurazione da un file .trgen e la applica al client
        /// </summary>
        /// <param name="client">Client TrGEN su cui applicare la configurazione</param>
        /// <param name="filePath">Percorso del file .trgen da importare</param>
        /// <param name="applyNetworkSettings">Se applicare anche le impostazioni di rete</param>
        /// <returns>Configurazione importata</returns>
        public static TrgenConfiguration ImportConfiguration(TrgenClient client, string filePath, bool applyNetworkSettings = false)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File di configurazione non trovato: {filePath}");

            if (!filePath.EndsWith(".trgen", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Il file deve avere estensione .trgen");

            try
            {
                // Leggi il contenuto del file
                string fileContent = File.ReadAllText(filePath);
                
                // Rimuovi l'header se presente
                string jsonContent = RemoveFileHeader(fileContent);

                // Deserializza da JSON
                var config = JsonUtility.FromJson<TrgenConfiguration>(jsonContent);
                if (config == null)
                    throw new Exception("Impossibile deserializzare la configurazione dal file");

                // Applica la configurazione al client
                ApplyConfigurationToClient(client, config, applyNetworkSettings);

                Debug.Log($"[TRGEN] Configurazione importata con successo da: {filePath}");
                return config;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TRGEN] Errore durante l'importazione: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Carica una configurazione da file senza applicarla ad alcun client
        /// </summary>
        /// <param name="filePath">Percorso del file .trgen</param>
        /// <returns>Configurazione caricata</returns>
        public static TrgenConfiguration LoadConfiguration(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File di configurazione non trovato: {filePath}");

            if (!filePath.EndsWith(".trgen", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Il file deve avere estensione .trgen");

            try
            {
                string fileContent = File.ReadAllText(filePath);
                string jsonContent = RemoveFileHeader(fileContent);
                var config = JsonUtility.FromJson<TrgenConfiguration>(jsonContent);
                
                if (config == null)
                    throw new Exception("Impossibile deserializzare la configurazione dal file");
                    
                return config;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TRGEN] Errore durante il caricamento: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Salva una configurazione in un file .trgen
        /// </summary>
        /// <param name="config">Configurazione da salvare</param>
        /// <param name="filePath">Percorso del file</param>
        /// <returns>Percorso completo del file salvato</returns>
        public static string SaveConfiguration(TrgenConfiguration config, string filePath)
        {
            string finalPath = EnsureTrgenExtension(filePath);
            
            try
            {
                string directory = Path.GetDirectoryName(finalPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Aggiorna timestamp di modifica
                config.Metadata.ModifiedAt = DateTime.Now;

                string jsonContent = JsonUtility.ToJson(config, true);
                string header = GenerateFileHeader(config);
                string finalContent = header + jsonContent;

                File.WriteAllText(finalPath, finalContent);

                Debug.Log($"[TRGEN] Configurazione salvata con successo: {finalPath}");
                return finalPath;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TRGEN] Errore durante il salvataggio: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Lista tutti i file .trgen in una directory
        /// </summary>
        /// <param name="directory">Directory da esplorare</param>
        /// <param name="recursive">Se cercare ricorsivamente nelle sottocartelle</param>
        /// <returns>Lista dei percorsi dei file .trgen trovati</returns>
        public static List<string> ListConfigurationFiles(string directory, bool recursive = false)
        {
            if (!Directory.Exists(directory))
                return new List<string>();

            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            return Directory.GetFiles(directory, "*.trgen", searchOption).ToList();
        }

        #region Private Methods

        private static string EnsureTrgenExtension(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("Percorso file non può essere vuoto", nameof(filePath));

            return filePath.EndsWith(".trgen", StringComparison.OrdinalIgnoreCase) 
                ? filePath 
                : filePath + ".trgen";
        }

        private static TrgenConfiguration CreateConfigurationFromClient(TrgenClient client, 
            string projectName, string description, string author)
        {
            var config = new TrgenConfiguration();

            // Metadati
            config.Metadata.ProjectName = projectName;
            config.Metadata.Description = description;
            config.Metadata.Author = author;
            config.Metadata.CreatedAt = DateTime.Now;
            config.Metadata.ModifiedAt = DateTime.Now;

            // Impostazioni default (valori estratti se disponibili dal client)
            config.Defaults.DefaultTriggerDurationUs = 40; // Valore default
            config.Defaults.DefaultLogLevel = "Warn"; // Valore default

            // Configurazioni delle porte
            CreatePortConfigurations(config);

            // Impostazioni di rete (valori di default)
            config.Network.IpAddress = "192.168.123.1";
            config.Network.Port = 4242;
            config.Network.TimeoutMs = 2000;

            return config;
        }

        private static void CreatePortConfigurations(TrgenConfiguration config)
        {
            // NeuroScan Ports (NS0-NS7)
            for (int i = 0; i <= 7; i++)
            {
                var portConfig = new TriggerPortConfig(32) // 32 è la lunghezza standard della memoria
                {
                    Id = i,
                    Name = $"NeuroScan {i}",
                    Type = "NS",
                    Enabled = true
                };
                config.TriggerPorts.Add($"NS{i}", portConfig);
            }

            // Synamps Ports (SA0-SA7)
            for (int i = 0; i <= 7; i++)
            {
                var portConfig = new TriggerPortConfig(32)
                {
                    Id = 8 + i,
                    Name = $"Synamps {i}",
                    Type = "SA",
                    Enabled = true
                };
                config.TriggerPorts.Add($"SA{i}", portConfig);
            }

            // TMS Ports
            config.TriggerPorts.Add("TMSO", new TriggerPortConfig(32)
            {
                Id = 16,
                Name = "TMS Output",
                Type = "TMS",
                Enabled = true
            });

            config.TriggerPorts.Add("TMSI", new TriggerPortConfig(32)
            {
                Id = 17,
                Name = "TMS Input",
                Type = "TMS",
                Enabled = true
            });

            // GPIO Ports (GPIO0-GPIO7)
            for (int i = 0; i <= 7; i++)
            {
                var portConfig = new TriggerPortConfig(32)
                {
                    Id = 18 + i,
                    Name = $"GPIO {i}",
                    Type = "GPIO",
                    Enabled = true
                };
                config.TriggerPorts.Add($"GPIO{i}", portConfig);
            }
        }

        private static void ApplyConfigurationToClient(TrgenClient client, TrgenConfiguration config, bool applyNetworkSettings)
        {
            // Applica impostazioni default
            client.SetDefaultDuration(config.Defaults.DefaultTriggerDurationUs);
            
            // Applica livello di verbosità se parsabile
            if (Enum.TryParse<TrgenClient.LogLevel>(config.Defaults.DefaultLogLevel, out var logLevel))
            {
                client.Verbosity = logLevel;
            }

            // Applica le configurazioni delle porte CON le istruzioni di memoria
            ApplyPortConfigurationsToClient(client, config);

            if (applyNetworkSettings)
            {
                Debug.LogWarning("[TRGEN] Le impostazioni di rete richiedono la creazione di un nuovo client per essere applicate.");
            }

            Debug.Log($"[TRGEN] Configurazione applicata: {config.TriggerPorts.Count} porte configurate con memoria");
        }

        private static void ApplyPortConfigurationsToClient(TrgenClient client, TrgenConfiguration config)
        {
            int portsApplied = 0;
            int portsWithInstructions = 0;

            foreach (var portPair in config.TriggerPorts)
            {
                var portKey = portPair.Key;
                var portConfig = portPair.Value;

                try
                {
                    // Crea il TrgenPort e applica la configurazione
                    var trgenPort = client.CreateTrgenPort(portConfig.Id);
                    portConfig.ApplyToTrgenPort(trgenPort);

                    // Se la porta ha istruzioni programmate, invia la memoria al dispositivo
                    if (portConfig.HasProgrammedInstructions() && portConfig.Enabled)
                    {
                        client.SendTrgenMemory(trgenPort);
                        portsWithInstructions++;
                        Debug.Log($"[TRGEN] Applicata configurazione con memoria per {portKey}: {portConfig.GetInstructionsString()}");
                    }

                    portsApplied++;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[TRGEN] Errore applicando configurazione per {portKey}: {ex.Message}");
                }
            }

            Debug.Log($"[TRGEN] Configurazioni applicate: {portsApplied} porte, {portsWithInstructions} con istruzioni programmate");
        }

        private static string GenerateFileHeader(TrgenConfiguration config)
        {
            return $@"# TrGEN Configuration File (JSON Format)
# Generated by trgen-unity library
# Project: {config.Metadata.ProjectName}
# Author: {config.Metadata.Author}
# Created: {config.Metadata.CreatedAt:yyyy-MM-dd HH:mm:ss}
# Description: {config.Metadata.Description}
# 
# This file contains trigger configuration data for TrGEN hardware
# File format version: {config.Metadata.Version}
# Format: JSON (Unity native compatibility)
#

";
        }

        private static string RemoveFileHeader(string content)
        {
            var lines = content.Split('\n');
            int startIndex = 0;

            // Trova la prima riga che non inizia con # o è vuota
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (!line.StartsWith("#") && !string.IsNullOrWhiteSpace(line))
                {
                    startIndex = i;
                    break;
                }
            }

            return string.Join("\n", lines.Skip(startIndex));
        }

        #endregion
    }
}