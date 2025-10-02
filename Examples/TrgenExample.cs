using System;
using System.Collections;
using System.Diagnostics;
using Trgen;
using UnityEngine;

public class TrgenExample : MonoBehaviour
{
  // TriggerClient instance is to be kept alive as long as you need it
  TrgenClient client;
    // Metodi di utility per controllare il throttling

  /// <summary>
  /// Verifica e inizializza automaticamente il client se necessario.
  /// </summary>
  /// <returns>True se il client è pronto per l'uso, False altrimenti.</returns>
  private bool EnsureClientReady()
  {
    if (client == null)
    {
      UnityEngine.Debug.LogWarning("Client non inizializzato. Tentativo di connessione automatica...");
      try
      {
        TriggerConnect();
        return client != null && client.Connected;
      }
      catch (Exception ex)
      {
        UnityEngine.Debug.LogError($"Impossibile inizializzare automaticamente il client: {ex.Message}");
        return false;
      }
    }
    
    return client.Connected;
  }

  public void TriggerConnect()
  {
    if (client != null && client.Connected)
      throw new InvalidOperationException("Already connected");
    else
    {
      client = new TrgenClient();
      client.Verbosity = TrgenClient.LogLevel.Debug;
      client.Connect();
    }
    }

  public void TriggerSend()
  {
    if (client == null)
    {
        UnityEngine.Debug.LogError("Client non inizializzato! Chiamare prima TriggerConnect().");
        return;
    }
    
    if (!client.Connected)
        throw new InvalidOperationException("Connection failed");

    // Connected! (:
    client.StartTrigger(TrgenPin.NS5);
    // Reset automatico ora incluso in StartTrigger
  }

  // Versione per chiamate immediate senza throttling (per Button click)
  public void MarkerSend()
  {
    if (client == null)
    {
        UnityEngine.Debug.LogWarning("Client non inizializzato! Chiamare prima TriggerConnect().");
        return;
    }
    
    if (!client.Connected)
        return;

    client.SendMarker(markerNS: 5, stop: false);
    // Reset automatico ora incluso in StartTrigger
    UnityEngine.Debug.Log("Trigger inviato!");
  }

  /// <summary>
  /// Test delle performance della libreria con diversi valori di durata del trigger.
  /// Testa durate da 5µs a 50µs con step di 2µs, misurando i tempi di esecuzione.
  /// Si connette automaticamente se necessario.
  /// </summary>
  public void TestPerformanceWithDifferentDurations()
  {
    // Verifica che il client sia inizializzato e connesso (con auto-connessione)
    if (!EnsureClientReady())
    {
      UnityEngine.Debug.LogError("Impossibile inizializzare o connettere il client!");
      return;
    }

    UnityEngine.Debug.Log("=== INIZIO TEST PERFORMANCE ===");
    UnityEngine.Debug.Log("Testando durate da 5µs a 50µs con step di 2µs");

    var watch = System.Diagnostics.Stopwatch.StartNew();
    int totalIterations = 0;

    // Loop da 5µs a 100µs con step di 1µs
    for (uint duration = 5; duration <= 100; duration += 1)
    {
      // Configura la nuova durata
      client.SetDefaultDuration(duration);
      
      // Misura tempo per questa durata specifica
      var iterationWatch = System.Diagnostics.Stopwatch.StartNew();
      
      try
      {
        // Esegui StartTrigger che userà la durata configurata
        client.StartTrigger(TrgenPin.NS5);
        totalIterations++;
        
        iterationWatch.Stop();
        UnityEngine.Debug.Log($"Durata: {duration}µs - Tempo esecuzione: {iterationWatch.ElapsedMilliseconds}ms ({iterationWatch.ElapsedTicks} ticks)");
        
        // Piccola pausa per evitare sovraccarico del dispositivo
        System.Threading.Thread.Sleep(10);
      }
      catch (Exception ex)
      {
        UnityEngine.Debug.LogError($"Errore con durata {duration}µs: {ex.Message}");
      }
    }

    watch.Stop();
    
    UnityEngine.Debug.Log("=== RISULTATI TEST PERFORMANCE ===");
    UnityEngine.Debug.Log($"Iterazioni completate: {totalIterations}");
    UnityEngine.Debug.Log($"Tempo totale: {watch.ElapsedMilliseconds}ms");
    UnityEngine.Debug.Log($"Tempo medio per iterazione: {(double)watch.ElapsedMilliseconds / totalIterations:F2}ms");
    UnityEngine.Debug.Log($"Durate testate: da 5µs a 50µs (step 2µs)");
  }

  /// <summary>
  /// Test della funzione SendMarker con diversi valori per NS, SA e GPIO.
  /// </summary>
  public void TestSendMarker()
  {
    // Verifica che il client sia inizializzato e connesso (con auto-connessione)
    if (!EnsureClientReady())
    {
      UnityEngine.Debug.LogError("Impossibile inizializzare o connettere il client!");
      return;
    }

    UnityEngine.Debug.Log("=== INIZIO TEST SEND MARKER ===");

    try
    {
      // Test 1: Marker solo su NeuroScan (valore 5 = 0b00000101 = NS0 + NS2)
      UnityEngine.Debug.Log("Test 1: Invio marker NS=5 (NS0+NS2)");
      client.SendMarker(markerNS: 5, stop: true);
      System.Threading.Thread.Sleep(100);

      // Test 2: Marker solo su Synamps (valore 3 = 0b00000011 = SA0 + SA1)
      UnityEngine.Debug.Log("Test 2: Invio marker SA=3 (SA0+SA1)");
      client.SendMarker(markerSA: 3, stop: true);
      System.Threading.Thread.Sleep(100);

      // Test 3: Marker solo su GPIO (valore 7 = 0b00000111 = GPIO0+GPIO1+GPIO2)
      UnityEngine.Debug.Log("Test 3: Invio marker GPIO=7 (GPIO0+GPIO1+GPIO2)");
      client.SendMarker(markerGPIO: 7, stop: true);
      System.Threading.Thread.Sleep(100);

      // Test 4: Marker combinato su tutte le porte
      UnityEngine.Debug.Log("Test 4: Invio marker combinato NS=1, SA=2, GPIO=4");
      client.SendMarker(markerNS: 1, markerSA: 2, markerGPIO: 4, stop: true);
      System.Threading.Thread.Sleep(100);

      // Test 5: Test con LSB=true (ordine bit invertito)
      UnityEngine.Debug.Log("Test 5: Invio marker NS=1 con LSB=true");
      client.SendMarker(markerNS: 1, LSB: true, stop: true);
      System.Threading.Thread.Sleep(100);

      // Test 6: Marker senza stop automatico (richiede stop manuale)
      UnityEngine.Debug.Log("Test 6: Invio marker NS=255 senza stop automatico");
      client.SendMarker(markerNS: 255, stop: false);
      System.Threading.Thread.Sleep(200);
      client.Stop(); // Stop manuale
      
      UnityEngine.Debug.Log("=== TEST SEND MARKER COMPLETATO ===");
    }
    catch (Exception ex)
    {
      UnityEngine.Debug.LogError($"Errore durante test SendMarker: {ex.Message}");
    }
  }

  #region Configuration Export/Import Examples

  /// <summary>
  /// Esempio di esportazione della configurazione corrente in un file .trgen
  /// Ora include la memoria programmata di tutte le porte
  /// </summary>
  public void ExportCurrentConfiguration()
  {
    // Verifica che il client sia inizializzato
    if (!EnsureClientReady())
    {
      UnityEngine.Debug.LogError("Impossibile esportare: client non connesso!");
      return;
    }

    try
    {
      // Prima programma alcune porte per avere dati interessanti da esportare
      ProgramSamplePorts();

      // Percorso del file (Unity salverà nella cartella del progetto)
      string filePath = "Configurations/EsperimentoEEG_ConMemoria";
      
      // Esporta la configurazione corrente CON la memoria programmata
      string savedPath = client.ExportConfiguration(
        filePath,
        projectName: "Esperimento EEG - Con Memoria Programmata",
        description: "Configurazione completa con istruzioni di memoria per trigger NS2, NS3 e GPIO0",
        author: "Laboratorio CoSANLab"
      );

      UnityEngine.Debug.Log($"Configurazione con memoria esportata con successo in: {savedPath}");
      
      // Mostra un riassunto della memoria esportata
      ShowMemorySnapshot();
    }
    catch (System.Exception ex)
    {
      UnityEngine.Debug.LogError($"Errore durante l'esportazione: {ex.Message}");
    }
  }

  /// <summary>
  /// Programma alcune porte con istruzioni di esempio per dimostrare l'export della memoria
  /// </summary>
  private void ProgramSamplePorts()
  {
    try
    {
      UnityEngine.Debug.Log("Programmando porte di esempio...");

      // Programma NS2 con durata personalizzata di 20µs
      var ns2Instructions = new uint[] {
        InstructionEncoder.ActiveForUs(20),    // Attivo per 20µs
        InstructionEncoder.UnactiveForUs(3),   // Inattivo per 3µs 
        InstructionEncoder.End()               // Fine sequenza
      };
      client.ProgramPortWithInstructions(TrgenPin.NS2, ns2Instructions);

      // Programma NS3 per marker di blocco (50µs)
      var ns3Instructions = new uint[] {
        InstructionEncoder.ActiveForUs(50),    // Attivo per 50µs
        InstructionEncoder.UnactiveForUs(3),   // Inattivo per 3µs
        InstructionEncoder.End()               // Fine sequenza
      };
      client.ProgramPortWithInstructions(TrgenPin.NS3, ns3Instructions);

      // Programma GPIO0 per LED di stato (200µs per visibilità)
      var gpio0Instructions = new uint[] {
        InstructionEncoder.ActiveForUs(200),   // Attivo per 200µs
        InstructionEncoder.UnactiveForUs(3),   // Inattivo per 3µs
        InstructionEncoder.End()               // Fine sequenza
      };
      client.ProgramPortWithInstructions(TrgenPin.GPIO0, gpio0Instructions);

      UnityEngine.Debug.Log("Programmazione completata - Pronto per export!");
    }
    catch (System.Exception ex)
    {
      UnityEngine.Debug.LogError($"Errore nella programmazione delle porte: {ex.Message}");
    }
  }

  /// <summary>
  /// Mostra un riassunto della memoria di tutte le porte
  /// </summary>
  public void ShowMemorySnapshot()
  {
    if (!EnsureClientReady())
    {
      UnityEngine.Debug.LogError("Client non connesso!");
      return;
    }

    try
    {
      var snapshot = client.CreateMemorySnapshot();
      
      UnityEngine.Debug.Log("=== SNAPSHOT MEMORIA PORTE ===");
      
      foreach (var portEntry in snapshot)
      {
        var portName = portEntry.Key;
        var memory = portEntry.Value;
        
        // Conta istruzioni non-zero
        int nonZeroInstructions = 0;
        for (int i = 0; i < memory.Length; i++)
        {
          if (memory[i] != 0)
            nonZeroInstructions = i + 1; // L'ultima istruzione valida
        }

        if (nonZeroInstructions > 0)
        {
          UnityEngine.Debug.Log($"{portName}: {nonZeroInstructions} istruzioni programmate");
          for (int i = 0; i < nonZeroInstructions; i++)
          {
            UnityEngine.Debug.Log($"  [{i}]: 0x{memory[i]:X8}");
          }
        }
        else
        {
          UnityEngine.Debug.Log($"{portName}: Memoria vuota");
        }
      }
      
      UnityEngine.Debug.Log("=== FINE SNAPSHOT ===");
    }
    catch (System.Exception ex)
    {
      UnityEngine.Debug.LogError($"Errore nel creare snapshot: {ex.Message}");
    }
  }

  /// <summary>
  /// Esempio di importazione di una configurazione da file .trgen
  /// Ora include il ripristino della memoria programmata
  /// </summary>
  public void ImportConfiguration()
  {
    try
    {
      string filePath = "Configurations/EEG_Template_WithMemory.trgen";
      
      // Verifica che il client sia pronto
      if (!EnsureClientReady())
      {
        UnityEngine.Debug.LogError("Impossibile importare: client non connesso!");
        return;
      }

      UnityEngine.Debug.Log("=== STATO PRIMA DELL'IMPORT ===");
      ShowMemorySnapshot();

      // Importa e applica la configurazione CON la memoria programmata
      var config = client.ImportConfiguration(filePath, applyNetworkSettings: false);
      
      UnityEngine.Debug.Log($"Configurazione '{config.Metadata.ProjectName}' importata con successo!");
      UnityEngine.Debug.Log($"Autor: {config.Metadata.Author}");
      UnityEngine.Debug.Log($"Descrizione: {config.Metadata.Description}");
      UnityEngine.Debug.Log($"Porte configurate: {config.TriggerPorts.Count}");
      UnityEngine.Debug.Log($"Durata trigger default: {config.Defaults.DefaultTriggerDurationUs}µs");

      // Conta porte con istruzioni programmate
      int portsWithMemory = 0;
      foreach (var port in config.TriggerPorts.Values)
      {
        if (port.HasProgrammedInstructions())
        {
          portsWithMemory++;
          UnityEngine.Debug.Log($"Porta {port.Name} (ID:{port.Id}) ha {port.LastInstructionIndex + 1} istruzioni programmate");
        }
      }
      
      UnityEngine.Debug.Log($"Porte con memoria programmata: {portsWithMemory}");

      UnityEngine.Debug.Log("=== STATO DOPO L'IMPORT ===");
      ShowMemorySnapshot();
    }
    catch (System.Exception ex)
    {
      UnityEngine.Debug.LogError($"Errore durante l'importazione: {ex.Message}");
    }
  }

  /// <summary>
  /// Esempio di creazione di una configurazione personalizzata e salvataggio
  /// Ora include programmazione specifica della memoria
  /// </summary>
  public void CreateCustomConfiguration()
  {
    try
    {
      // Crea una nuova configurazione
      var config = new Trgen.TrgenConfiguration();
      
      // Imposta metadati
      config.Metadata.ProjectName = "Configurazione Personalizzata con Memoria";
      config.Metadata.Author = "Test User";
      config.Metadata.Description = "Configurazione di esempio con istruzioni di memoria personalizzate";
      
      // Imposta parametri custom
      config.Defaults.DefaultTriggerDurationUs = 25; // 25 microsecondi
      config.Defaults.DefaultLogLevel = "Info";
      config.Defaults.AutoResetEnabled = true;
      config.Defaults.AutoResetDelayUs = 50;
      
      // Configura solo alcune porte specifiche CON memoria programmata
      config.TriggerPorts.Clear();
      
      // NS0 - Stimolo Visivo con programmazione specifica
      var ns0Config = new Trgen.TriggerPortConfig(32)
      {
        Id = 0,
        Name = "Stimolo Visivo",
        Type = "NS",
        Enabled = true,
        CustomDurationUs = 30,
        Notes = "Trigger per stimoli visivi con sequenza personalizzata"
      };
      
      // Programma la memoria di NS0 con una sequenza custom
      ns0Config.MemoryInstructions[0] = InstructionEncoder.ActiveForUs(30);    // 30µs attivo
      ns0Config.MemoryInstructions[1] = InstructionEncoder.UnactiveForUs(5);   // 5µs inattivo
      ns0Config.MemoryInstructions[2] = InstructionEncoder.ActiveForUs(10);    // 10µs attivo di nuovo
      ns0Config.MemoryInstructions[3] = InstructionEncoder.UnactiveForUs(3);   // 3µs inattivo
      ns0Config.MemoryInstructions[4] = InstructionEncoder.End();              // Fine
      ns0Config.LastInstructionIndex = 4;
      ns0Config.ProgrammingState = Trgen.PortProgrammingState.Programmed;
      ns0Config.LastProgrammedAt = System.DateTime.Now;
      
      config.TriggerPorts["NS0"] = ns0Config;
      
      // NS1 - Stimolo Auditivo
      var ns1Config = new Trgen.TriggerPortConfig(32)
      {
        Id = 1,
        Name = "Stimolo Auditivo",
        Type = "NS", 
        Enabled = true,
        CustomDurationUs = 40,
        Notes = "Trigger per stimoli auditivi - Impulso singolo"
      };
      
      // Programmazione semplice per NS1
      ns1Config.MemoryInstructions[0] = InstructionEncoder.ActiveForUs(40);    // 40µs attivo
      ns1Config.MemoryInstructions[1] = InstructionEncoder.UnactiveForUs(3);   // 3µs inattivo  
      ns1Config.MemoryInstructions[2] = InstructionEncoder.End();              // Fine
      ns1Config.LastInstructionIndex = 2;
      ns1Config.ProgrammingState = Trgen.PortProgrammingState.Programmed;
      ns1Config.LastProgrammedAt = System.DateTime.Now;
      
      config.TriggerPorts["NS1"] = ns1Config;
      
      // NS5 - Marker Risposta (non programmato)
      config.TriggerPorts["NS5"] = new Trgen.TriggerPortConfig(32)
      {
        Id = 5,
        Name = "Marker Risposta",
        Type = "NS",
        Enabled = true,
        Notes = "Marker per registrare le risposte del soggetto - Memoria vuota",
        ProgrammingState = Trgen.PortProgrammingState.NotProgrammed
      };

      // GPIO0 per controlli esterni - Con sequenza lunga per LED
      var gpio0Config = new Trgen.TriggerPortConfig(32)
      {
        Id = 18,
        Name = "LED Controllo",
        Type = "GPIO",
        Enabled = true,
        CustomDurationUs = 100,
        Notes = "Controllo LED di stato con pattern di blink"
      };
      
      // Pattern di blink per LED
      gpio0Config.MemoryInstructions[0] = InstructionEncoder.ActiveForUs(100);   // LED ON 100µs
      gpio0Config.MemoryInstructions[1] = InstructionEncoder.UnactiveForUs(50);  // LED OFF 50µs
      gpio0Config.MemoryInstructions[2] = InstructionEncoder.ActiveForUs(100);   // LED ON 100µs
      gpio0Config.MemoryInstructions[3] = InstructionEncoder.UnactiveForUs(50);  // LED OFF 50µs
      gpio0Config.MemoryInstructions[4] = InstructionEncoder.ActiveForUs(100);   // LED ON 100µs
      gpio0Config.MemoryInstructions[5] = InstructionEncoder.UnactiveForUs(3);   // LED OFF 3µs
      gpio0Config.MemoryInstructions[6] = InstructionEncoder.End();              // Fine
      gpio0Config.LastInstructionIndex = 6;
      gpio0Config.ProgrammingState = Trgen.PortProgrammingState.Programmed;
      gpio0Config.LastProgrammedAt = System.DateTime.Now;
      
      config.TriggerPorts["GPIO0"] = gpio0Config;

      // Salva la configurazione
      string savedPath = Trgen.TrgenConfigurationManager.SaveConfiguration(
        config, 
        "Configurations/ConfigPersonalizzataConMemoria"
      );
      
      UnityEngine.Debug.Log($"Configurazione personalizzata con memoria creata e salvata in: {savedPath}");
      
      // Mostra dettagli delle programmazioni
      UnityEngine.Debug.Log("=== DETTAGLI MEMORIA CONFIGURAZIONE ===");
      foreach (var portPair in config.TriggerPorts)
      {
        var port = portPair.Value;
        if (port.HasProgrammedInstructions())
        {
          UnityEngine.Debug.Log($"{portPair.Key}: {port.GetInstructionsString()}");
        }
        else
        {
          UnityEngine.Debug.Log($"{portPair.Key}: Memoria vuota");
        }
      }
    }
    catch (System.Exception ex)
    {
      UnityEngine.Debug.LogError($"Errore durante la creazione della configurazione: {ex.Message}");
    }
  }

  /// <summary>
  /// Lista tutte le configurazioni .trgen disponibili in una cartella
  /// </summary>
  public void ListAvailableConfigurations()
  {
    try
    {
      string configDir = "Configurations";
      var configs = Trgen.TrgenConfigurationManager.ListConfigurationFiles(configDir, recursive: true);
      
      if (configs.Count == 0)
      {
        UnityEngine.Debug.Log($"Nessuna configurazione .trgen trovata in: {configDir}");
        return;
      }
      
      UnityEngine.Debug.Log($"Configurazioni disponibili ({configs.Count}):");
      for (int i = 0; i < configs.Count; i++)
      {
        try
        {
          var config = Trgen.TrgenConfigurationManager.LoadConfiguration(configs[i]);
          UnityEngine.Debug.Log($"{i + 1}. {System.IO.Path.GetFileName(configs[i])}");
          UnityEngine.Debug.Log($"   Progetto: {config.Metadata.ProjectName}");
          UnityEngine.Debug.Log($"   Autore: {config.Metadata.Author}");
          UnityEngine.Debug.Log($"   Creato: {config.Metadata.CreatedAt:yyyy-MM-dd HH:mm}");
        }
        catch (System.Exception ex)
        {
          UnityEngine.Debug.LogWarning($"Errore nel leggere {configs[i]}: {ex.Message}");
        }
      }
    }
    catch (System.Exception ex)
    {
      UnityEngine.Debug.LogError($"Errore durante la ricerca delle configurazioni: {ex.Message}");
    }
  }

  /// <summary>
  /// Applica una configurazione precaricata al client corrente
  /// </summary>
  public void ApplyPresetConfiguration()
  {
    if (!EnsureClientReady())
    {
      UnityEngine.Debug.LogError("Client non connesso!");
      return;
    }

    try
    {
      // Crea una configurazione preset ottimizzata per EEG
      var config = new Trgen.TrgenConfiguration();
      
      // Configurazione ottimale per EEG
      config.Defaults.DefaultTriggerDurationUs = 10; // Trigger molto brevi per EEG
      config.Defaults.DefaultLogLevel = "Warn"; // Log minimali durante registrazione
      config.Defaults.AutoResetEnabled = true;
      
      // Applica al client corrente
      client.ApplyConfiguration(config);
      
      UnityEngine.Debug.Log("Configurazione preset per EEG applicata con successo!");
      UnityEngine.Debug.Log($"Durata trigger impostata a: {config.Defaults.DefaultTriggerDurationUs}µs");
    }
    catch (System.Exception ex)
    {
      UnityEngine.Debug.LogError($"Errore nell'applicare la configurazione preset: {ex.Message}");
    }
  }

  #endregion


}