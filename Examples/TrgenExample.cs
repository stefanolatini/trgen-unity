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


}