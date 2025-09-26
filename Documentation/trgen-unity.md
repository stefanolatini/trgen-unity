# TRGen Unity Package

[![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black?logo=unity)](https://unity.com)
[![GitHub release](https://img.shields.io/github/v/release/stefanolatini/trgen-unity?sort=semver)](https://github.com/stefanolatini/trgen-unity/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Una libreria Unity per la comunicazione Ethernet con il dispositivo TriggerBox CoSANLab tramite socket TCP/IP. Permette l'invio di segnali di trigger precisi per applicazioni di neurofisiologia, stimolazione e sincronizzazione di esperimenti.

---

## 📋 Indice

- [Caratteristiche](#-caratteristiche)
- [Installazione](#-installazione)
- [Guida Rapida](#-guida-rapida)
- [Documentazione API](#-documentazione-api)
- [Esempi Avanzati](#-esempi-avanzati)
- [Risoluzione Problemi](#-risoluzione-problemi)
- [Contribuire](#-contribuire)

---

## ✨ Caratteristiche

### 🔌 Tipi di Pin Supportati
- **NeuroScan (NS0-NS7):** Per amplificatori NeuroScan
- **Synamps (SA0-SA7):** Per amplificatori Synamps  
- **GPIO (GPIO0-GPIO7):** Pin GPIO generici programmabili
- **TMS (TMSO, TMSI):** Per stimolazione magnetica transcranica

### 🚀 Funzionalità Principali
- ✅ **Connessione TCP/IP persistente** con gestione automatica degli errori
- ✅ **Operazioni sincrone e asincrone** per massima flessibilità
- ✅ **Programmazione avanzata dei trigger** con sequenze personalizzate
- ✅ **Invio di marker codificati** su più porte contemporaneamente
- ✅ **Sistema di logging configurabile** per debug e diagnostica
- ✅ **Thread-safe** per uso in applicazioni multi-thread
- ✅ **Gestione automatica della memoria** dei trigger
- ✅ **Supporto per sequenze complesse** con loop e sincronizzazione

---

## 📦 Installazione

### Metodo 1: OpenUPM (Consigliato)

Se hai `openupm-cli` installato:

```bash
openupm add com.cosanlab.trgen
```

### Metodo 2: Package Manager

1. Apri **Window > Package Manager** in Unity
2. Clicca su **"+"** e seleziona **"Add package from git URL..."**
3. Inserisci: `https://github.com/stefanolatini/trgen-unity.git`

### Metodo 3: Manifest Manuale

Aggiungi questa dipendenza al tuo `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.cosanlab.trgen": "1.0.0"
  },
  "scopedRegistries": [
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.cosanlab.trgen"
      ]
    }
  ]
}
```

---

## 🚀 Guida Rapida

### Connessione di Base

```csharp
using Trgen;
using UnityEngine;

public class TriggerBasicExample : MonoBehaviour
{
    private TrgenClient client;
    
    async void Start()
    {
        // Crea il client (IP di default: 192.168.123.1, Porta: 4242)
        client = new TrgenClient();
        
        // Configura il livello di logging (opzionale)
        client.Verbosity = LogLevel.Info;
        
        try
        {
            // Connessione asincrona
            await client.ConnectAsync();
            Debug.Log("Connesso al TriggerBox!");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Errore di connessione: {ex.Message}");
        }
    }
    
    void OnDestroy()
    {
        // Sempre disconnettere quando non serve più
        client?.Disconnect();
    }
}
```

### Invio Trigger Singolo

```csharp
// Verifica disponibilità del dispositivo
if (client.IsAvailable())
{
    // Invia un trigger di 20μs sul pin NeuroScan 5
    client.StartTrigger(TriggerPin.NS5);
    Debug.Log("Trigger inviato!");
}
```

### Invio Trigger Multipli

```csharp
// Lista di trigger da attivare contemporaneamente
var triggers = new List<int> 
{ 
    TriggerPin.NS0, 
    TriggerPin.NS2, 
    TriggerPin.GPIO3 
};

client.StartTriggerList(triggers);
Debug.Log("Trigger multipli inviati!");
```

### Invio Marker Codificati

```csharp
// Invia il valore 5 sui pin NeuroScan (attiva NS0 e NS2)
client.SendMarker(markerNS: 5);

// Invio simultaneo su più tipi di pin
client.SendMarker(
    markerNS: 3,     // NeuroScan: valore 3 (bin: 00000011)
    markerGPIO: 7,   // GPIO: valore 7 (bin: 00000111)  
    LSB: true        // LSB first (bit 0 → pin 0)
);
```

---

## 📚 Documentazione API

### TrgenClient

Classe principale per la gestione della comunicazione con il TriggerBox.

#### Costruttori

```csharp
// Parametri default (IP: 192.168.123.1, Porta: 4242, Timeout: 2000ms)
TrgenClient client = new TrgenClient();

// IP personalizzato
TrgenClient client = new TrgenClient("192.168.1.100");

// Tutti i parametri personalizzati
TrgenClient client = new TrgenClient("192.168.1.100", 4242, 5000);
```

#### Connessione

```csharp
// Connessione asincrona (consigliata)
await client.ConnectAsync();

// Connessione sincrona (bloccante)
client.Connect();

// Disconnessione
client.Disconnect();

// Verifica disponibilità
bool isAvailable = client.IsAvailable();
```

#### Proprietà

```csharp
// Stato connessione
bool connected = client.Connected;

// Livello di logging
client.Verbosity = LogLevel.Debug; // None, Error, Warn, Info, Debug
```

#### Controllo Trigger

```csharp
// Trigger singolo
client.StartTrigger(TriggerPin.NS5);

// Trigger multipli
client.StartTriggerList(new List<int> { TriggerPin.NS0, TriggerPin.SA2 });

// Marker codificati
client.SendMarker(markerNS: 15, markerGPIO: 7, LSB: false);

// Stop completo
client.StopTrigger();
```

#### Controllo Avanzato

```csharp
// Comandi di basso livello
client.Start();              // Avvia esecuzione
client.Stop();               // Ferma esecuzione
client.SetLevel(0b11110000);  // Imposta pin NeuroScan/Synamps
client.SetGpio(0b00001111);   // Imposta pin GPIO

// Lettura stato
int level = client.GetLevel();   // Stato NeuroScan/Synamps
int gpio = client.GetGpio();     // Stato GPIO
int status = client.GetStatus(); // Stato generale
```

### TriggerPin

Classe statica con le costanti per gli identificatori dei pin.

#### Pin Individuali

```csharp
// Pin NeuroScan
TriggerPin.NS0, TriggerPin.NS1, ..., TriggerPin.NS7

// Pin Synamps  
TriggerPin.SA0, TriggerPin.SA1, ..., TriggerPin.SA7

// Pin GPIO
TriggerPin.GPIO0, TriggerPin.GPIO1, ..., TriggerPin.GPIO7

// Pin TMS
TriggerPin.TMSO, TriggerPin.TMSI
```

#### Gruppi Predefiniti

```csharp
// Tutti i pin NeuroScan
client.ResetAll(TriggerPin.AllNs);

// Tutti i pin Synamps
client.ResetAll(TriggerPin.AllSa);

// Tutti i pin GPIO
client.ResetAll(TriggerPin.AllGpio);

// Tutti i pin TMS
client.ResetAll(TriggerPin.AllTMS);
```

### Programmazione Avanzata

#### Creazione di Sequenze Personalizzate

```csharp
var trigger = client.CreateTrgenPort(TriggerPin.NS5);

// Sequenza: attivo 50μs, inattivo 10μs, fine
trigger.SetInstruction(0, InstructionEncoder.ActiveForUs(50));
trigger.SetInstruction(1, InstructionEncoder.UnactiveForUs(10));
trigger.SetInstruction(2, InstructionEncoder.End());

// Invia la programmazione al dispositivo
client.SendTrgenMemory(trigger);
client.Start();
```

#### Loop e Ripetizioni

```csharp
// Sequenza con ripetizione: 3 impulsi da 20μs con pausa di 5μs
trigger.SetInstruction(0, InstructionEncoder.ActiveForUs(20));
trigger.SetInstruction(1, InstructionEncoder.UnactiveForUs(5));
trigger.SetInstruction(2, InstructionEncoder.Repeat(0, 3)); // Ripeti 3 volte
trigger.SetInstruction(3, InstructionEncoder.End());
```

#### Sincronizzazione tra Trigger

```csharp
// Trigger che attende un evento da un altro trigger
trigger.SetInstruction(0, InstructionEncoder.WaitPE(TriggerPin.NS0)); // Attendi positive edge su NS0
trigger.SetInstruction(1, InstructionEncoder.ActiveForUs(30));
trigger.SetInstruction(2, InstructionEncoder.End());
```

---

## 🔧 Esempi Avanzati

### Esempio Completo: Esperimento EEG

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Trgen;

public class EEGExperiment : MonoBehaviour
{
    [Header("TriggerBox Settings")]
    public string deviceIP = "192.168.123.1";
    public LogLevel verbosity = LogLevel.Info;
    
    [Header("Experiment Settings")]  
    public float stimulusDuration = 0.1f;
    public float intervalMin = 1.0f;
    public float intervalMax = 3.0f;
    public int totalTrials = 20;
    
    private TrgenClient client;
    private int currentTrial = 0;
    
    async void Start()
    {
        // Inizializza il client
        client = new TrgenClient(deviceIP);
        client.Verbosity = verbosity;
        
        try
        {
            await client.ConnectAsync();
            Debug.Log("TriggerBox connesso - Inizio esperimento");
            
            // Avvia l'esperimento
            StartCoroutine(RunExperiment());
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Errore di connessione: {ex.Message}");
        }
    }
    
    IEnumerator RunExperiment()
    {
        for (currentTrial = 1; currentTrial <= totalTrials; currentTrial++)
        {
            Debug.Log($"Trial {currentTrial}/{totalTrials}");
            
            // Marker di inizio trial
            client.SendMarker(markerNS: currentTrial);
            yield return new WaitForSeconds(0.01f);
            
            // Presenta lo stimolo per la durata specificata
            yield return StartCoroutine(PresentStimulus());
            
            // Intervallo casuale tra trial
            float interval = Random.Range(intervalMin, intervalMax);
            yield return new WaitForSeconds(interval);
        }
        
        Debug.Log("Esperimento completato");
        
        // Marker di fine esperimento
        client.SendMarker(markerNS: 255);
    }
    
    IEnumerator PresentStimulus()
    {
        // Marker di inizio stimolo
        client.SendMarker(markerNS: 10, markerGPIO: 1);
        
        // Attiva stimulus display
        ShowVisualStimulus(true);
        
        yield return new WaitForSeconds(stimulusDuration);
        
        // Marker di fine stimolo
        client.SendMarker(markerNS: 20, markerGPIO: 0);
        
        // Disattiva stimulus display
        ShowVisualStimulus(false);
    }
    
    void ShowVisualStimulus(bool show)
    {
        // Implementa qui la logica per mostrare/nascondere lo stimolo visivo
        Debug.Log($"Stimolo visivo: {(show ? "ON" : "OFF")}");
    }
    
    void OnDestroy()
    {
        if (client != null && client.Connected)
        {
            // Reset completo prima di disconnettere
            client.StopTrigger();
            client.Disconnect();
        }
    }
    
    void OnGUI()
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label($"TriggerBox Status: {(client?.Connected == true ? "Connected" : "Disconnected")}");
        GUILayout.Label($"Current Trial: {currentTrial}/{totalTrials}");
        
        if (GUILayout.Button("Send Test Trigger"))
        {
            client?.StartTrigger(TriggerPin.NS1);
        }
        
        if (GUILayout.Button("Emergency Stop"))
        {
            client?.StopTrigger();
        }
        GUILayout.EndVertical();
    }
}
```

### Esempio: Sincronizzazione con Unity Timeline

```csharp
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Trgen;

[System.Serializable]
public class TriggerMarker : INotification
{
    public int markerValue = 1;
    public TriggerType triggerType = TriggerType.NeuroScan;
}

public enum TriggerType { NeuroScan, Synamps, GPIO }

public class TriggerReceiver : MonoBehaviour, INotificationReceiver
{
    private TrgenClient client;
    
    async void Start()
    {
        client = new TrgenClient();
        await client.ConnectAsync();
    }
    
    public void OnNotify(Playable origin, INotification notification, object context)
    {
        if (notification is TriggerMarker marker && client.Connected)
        {
            switch (marker.triggerType)
            {
                case TriggerType.NeuroScan:
                    client.SendMarker(markerNS: marker.markerValue);
                    break;
                case TriggerType.Synamps:
                    client.SendMarker(markerSA: marker.markerValue);
                    break;
                case TriggerType.GPIO:
                    client.SendMarker(markerGPIO: marker.markerValue);
                    break;
            }
            
            Debug.Log($"Trigger inviato: {marker.triggerType} = {marker.markerValue}");
        }
    }
}
```

---

## ⚠️ Risoluzione Problemi

### Problemi di Connessione

**Errore: "Timeout connecting to 192.168.123.1:4242"**

- ✅ Verifica che il TriggerBox sia acceso e collegato alla rete
- ✅ Controlla l'indirizzo IP del dispositivo (default: 192.168.123.1)
- ✅ Assicurati che non ci sia un firewall che blocca la porta 4242
- ✅ Testa la connettività con `ping 192.168.123.1`

**Errore: "Connection failed"**

```csharp
// Test di connettività prima della connessione
if (client.IsAvailable())
{
    await client.ConnectAsync();
}
else
{
    Debug.LogError("Dispositivo non raggiungibile");
}
```

### Problemi di Trigger

**I trigger non vengono inviati**

```csharp
// Verifica lo stato della connessione
if (!client.Connected)
{
    Debug.LogError("Client non connesso!");
    return;
}

// Aumenta il livello di logging per debug
client.Verbosity = LogLevel.Debug;
```

**Comportamento erratico dei trigger**

```csharp
// Reset completo prima di nuove operazioni
client.StopTrigger(); // Stop + reset di tutti i trigger

// Attendi un momento prima del prossimo comando
yield return new WaitForSeconds(0.1f);

// Invia il nuovo trigger
client.StartTrigger(TriggerPin.NS5);
```

### Debug e Logging

```csharp
// Abilita logging dettagliato
client.Verbosity = LogLevel.Debug;

// Test dei comandi di base
Debug.Log($"Dispositivo disponibile: {client.IsAvailable()}");
Debug.Log($"Stato connessione: {client.Connected}");
Debug.Log($"Livello pin: {client.GetLevel()}");
Debug.Log($"Stato GPIO: {client.GetGpio()}");
```

---

## 🛠 Best Practices

### Gestione della Connessione

```csharp
// ✅ BUONO: Sempre disconnettere quando non serve più
void OnDestroy()
{
    client?.StopTrigger();  // Reset dei trigger
    client?.Disconnect();   // Chiusura connessione
}

// ✅ BUONO: Gestione errori in connessione
try
{
    await client.ConnectAsync();
}
catch (TimeoutException)
{
    Debug.LogError("Timeout di connessione - verifica la rete");
}
catch (SocketException ex)
{
    Debug.LogError($"Errore di rete: {ex.Message}");
}
```

### Invio Trigger Efficiente

```csharp
// ✅ BUONO: Reset prima di nuove sequenze
client.StopTrigger();
await Task.Delay(10); // Breve pausa
client.StartTrigger(TriggerPin.NS5);

// ✅ BUONO: Uso di marker per codici numerici
client.SendMarker(markerNS: trialNumber);

// ❌ EVITARE: Trigger troppo frequenti senza pause
// client.StartTrigger(TriggerPin.NS1);
// client.StartTrigger(TriggerPin.NS2); // Troppo veloce!
```

### Programmazione Avanzata

```csharp
// ✅ BUONO: Sempre terminare le sequenze con End()
trigger.SetInstruction(0, InstructionEncoder.ActiveForUs(20));
trigger.SetInstruction(1, InstructionEncoder.UnactiveForUs(5));
trigger.SetInstruction(2, InstructionEncoder.End()); // OBBLIGATORIO

// ✅ BUONO: Riempire memoria inutilizzata
for (int i = 3; i < memoryLength; i++)
{
    trigger.SetInstruction(i, InstructionEncoder.NotAdmissible());
}
```

---

## 🤝 Contribuire

Contributi sono benvenuti! Per contribuire:

1. **Fork** il repository
2. Crea un **branch** per la tua feature (`git checkout -b feature/AmazingFeature`)
3. **Commit** le modifiche (`git commit -m 'Add AmazingFeature'`)  
4. **Push** al branch (`git push origin feature/AmazingFeature`)
5. Apri una **Pull Request**

### Sviluppo Locale

```bash
# Clone del repository
git clone https://github.com/stefanolatini/trgen-unity.git

# Importa in Unity (2021.3+)
# Testa con il TriggerBox hardware
```

---

## 📄 Licenza

Questo progetto è distribuito sotto licenza MIT. Vedi [LICENSE](LICENSE) per i dettagli.

---

## 📞 Supporto

- **Issues GitHub:** [github.com/stefanolatini/trgen-unity/issues](https://github.com/stefanolatini/trgen-unity/issues)
- **Email:** stefanoelatini@hotmail.it
- **CoSANLab:** [research.uniroma1.it/laboratorio/144782](https://research.uniroma1.it/laboratorio/144782)

---

## 🔗 Link Utili

- [Documentazione Unity Package Manager](https://docs.unity3d.com/Manual/upm-ui.html)
- [OpenUPM Registry](https://openupm.com/packages/com.cosanlab.trgen/)
- [CoSANLab Roma](https://research.uniroma1.it/laboratorio/144782)

---

<div align="center">

**Made with ❤️ by [CoSANLab Rome](https://research.uniroma1.it/laboratorio/144782)**

</div>
