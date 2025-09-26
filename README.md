<h1 align="center">TRGen Unity Package</h1><h1 align="center">TRGen Unity Package</h1>



<p align="center"><p align="center">

  <img src="images/banner.png" alt="TriggerBox Banner" width="600px" height="300px">  <img src="images/banner.png" alt="TriggerBox Banner" width="600px" height="300px">

</p></p>



<h3 align="center">A Unity library for Ethernet communication with CoSANLab TriggerBox device</h3><h3 align="center">Una libreria Unity per la comunicazione Ethernet con il dispositivo CoSANLab TriggerBox</h3>



<p align="center"><p align="center">

  <a href="https://unity.com"><img src="https://img.shields.io/badge/Unity-2021.3%2B-black?logo=unity" alt="Unity Version"></a>  <a href="https://unity.com"><img src="https://img.shields.io/badge/Unity-2021.3%2B-black?logo=unity" alt="Unity Version"></a>

  <a href="https://github.com/stefanolatini/trgen-unity/releases"><img src="https://img.shields.io/github/v/release/stefanolatini/trgen-unity?sort=semver" alt="GitHub Release"></a>  <a href="https://github.com/stefanolatini/trgen-unity/releases"><img src="https://img.shields.io/github/v/release/stefanolatini/trgen-unity?sort=semver" alt="GitHub Release"></a>

  <a href="https://opensource.org/licenses/MIT"><img src="https://img.shields.io/badge/License-MIT-yellow.svg" alt="License: MIT"></a>  <a href="https://opensource.org/licenses/MIT"><img src="https://img.shields.io/badge/License-MIT-yellow.svg" alt="License: MIT"></a>

  <a href="https://openupm.com/packages/com.cosanlab.trgen/"><img src="https://img.shields.io/npm/v/com.cosanlab.trgen?label=openupm&registry_uri=https://package.openupm.com" alt="OpenUPM"></a>  <a href="https://openupm.com/packages/com.cosanlab.trgen/"><img src="https://img.shields.io/npm/v/com.cosanlab.trgen?label=openupm&registry_uri=https://package.openupm.com" alt="OpenUPM"></a>

</p></p>



<div align="center">---

[![🇺🇸 English](https://img.shields.io/badge/🇺🇸-English-blue?style=for-the-badge)](README.md) [![🇮🇹 Italiano](https://img.shields.io/badge/🇮🇹-Italiano-green?style=for-the-badge)](README_ITA.md)

</div>

**🌐 Language / Lingua**## ✨ Caratteristiche


- 🔌 **Supporto completo per tutti i tipi di pin**: NeuroScan, Synamps, GPIO, TMS

- ⚡ **Operazioni sincrone e asincrone** per massima flessibilità  

- 🎯 **Trigger di precisione** con controllo temporale in microsecondi

- 📡 **Connessione TCP/IP persistente** con gestione automatica degli errori

---- 🔄 **Thread-safe** per applicazioni multi-thread

- 📝 **Sistema di logging configurabile** per debug e diagnostica

## ✨ Features- 🧩 **Programmazione avanzata** con sequenze, loop e sincronizzazione



- 🔌 **Complete support for all pin types**: NeuroScan, Synamps, GPIO, TMS## 📦 Installazione

- ⚡ **Synchronous and asynchronous operations** for maximum flexibility  

- 🎯 **Precision triggers** with microsecond timing control### Metodo 1: OpenUPM (Consigliato)

- 📡 **Persistent TCP/IP connection** with automatic error handling

- 🔄 **Thread-safe** for multi-threaded applications```bash

- 📝 **Configurable logging system** for debugging and diagnosticsopenupm add com.cosanlab.trgen

- 🧩 **Advanced programming** with sequences, loops and synchronization```



## 📦 Installation### Metodo 2: Package Manager Unity



### Method 1: OpenUPM (Recommended)1. Apri **Window > Package Manager**

2. Clicca **"+"** → **"Add package from git URL..."**  

```bash3. Inserisci: `https://github.com/stefanolatini/trgen-unity.git`

openupm add com.cosanlab.trgen

```### Metodo 3: Manifest Manuale



### Method 2: Unity Package ManagerAggiungi al tuo `Packages/manifest.json`:



1. Open **Window > Package Manager**```json

2. Click **"+"** → **"Add package from git URL..."**  {

3. Enter: `https://github.com/stefanolatini/trgen-unity.git`  "dependencies": {

    "com.cosanlab.trgen": "1.0.0"

### Method 3: Manual Manifest  },

  "scopedRegistries": [

Add to your `Packages/manifest.json`:    {

      "name": "OpenUPM", 

```json      "url": "https://package.openupm.com",

{      "scopes": ["com.cosanlab.trgen"]

  "dependencies": {    }

    "com.cosanlab.trgen": "1.0.0"  ]

  },}

  "scopedRegistries": [```

    {

      "name": "OpenUPM", ## 🚀 Guida Rapida

      "url": "https://package.openupm.com",

      "scopes": ["com.cosanlab.trgen"]### Connessione Base

    }

  ]

}```csharp

```using Trgen;

using UnityEngine;

## 🚀 Quick Start

public class BasicTriggerExample : MonoBehaviour

### Basic Connection{

    private TrgenClient client;

```csharp    

using Trgen;    async void Start()

using UnityEngine;    {

        // Crea client con IP di default (192.168.123.1:4242)

public class BasicTriggerExample : MonoBehaviour        client = new TrgenClient();

{        client.Verbosity = LogLevel.Info; // Log opzionale

    private TrgenClient client;        

            try

    async void Start()        {

    {            await client.ConnectAsync();

        // Create client with default IP (192.168.123.1:4242)            Debug.Log("TriggerBox connesso!");

        client = new TrgenClient();        }

        client.Verbosity = LogLevel.Info; // Optional logging        catch (System.Exception ex)

                {

        try            Debug.LogError($"Connessione fallita: {ex.Message}");

        {        }

            await client.ConnectAsync();    }

            Debug.Log("TriggerBox connected!");    

        }    void OnDestroy()

        catch (System.Exception ex)    {

        {        client?.Disconnect(); // Sempre disconnettere

            Debug.LogError($"Connection failed: {ex.Message}");    }

        }}

    }```

    

    void OnDestroy()### Invio Trigger Singolo

    {

        client?.Disconnect(); // Always disconnect```csharp

    }// Trigger singolo su pin NeuroScan 5 (impulso 20μs)

}client.StartTrigger(TrgenPin.NS5);

``````



### Send Single Trigger### Invio Marker Codificati



```csharp```csharp

// Single trigger on NeuroScan pin 5 (20μs pulse)// Invia valore 5 sui pin NeuroScan (attiva NS0 e NS2)

client.StartTrigger(TrgenPin.NS5);client.SendMarker(markerNS: 5);

```

// Invio simultaneo su più porte

### Send Encoded Markersclient.SendMarker(

    markerNS: 3,     // NeuroScan: valore 3  

```csharp    markerGPIO: 7,   // GPIO: valore 7

// Send value 5 on NeuroScan pins (activates NS0 and NS2)    LSB: true        // LSB first

client.SendMarker(markerNS: 5););

```

// Simultaneous send on multiple ports

client.SendMarker(### Esempio Avanzato: Sequenza Personalizzata

    markerNS: 3,     // NeuroScan: value 3  

    markerGPIO: 7,   // GPIO: value 7```csharp

    LSB: true        // LSB first// Crea trigger personalizzato

);var trigger = client.CreateTrgenPort(TrgenPin.NS5);

```

// Programma sequenza: attivo 50μs, inattivo 10μs, fine

### Advanced Example: Custom Sequencetrigger.SetInstruction(0, InstructionEncoder.ActiveForUs(50));

trigger.SetInstruction(1, InstructionEncoder.UnactiveForUs(10));

```csharptrigger.SetInstruction(2, InstructionEncoder.End());

// Create custom trigger

var trigger = client.CreateTrgenPort(TrgenPin.NS5);// Invia al dispositivo ed esegui

client.SendTrgenMemory(trigger);

// Program sequence: active 50μs, inactive 10μs, endclient.Start();

trigger.SetInstruction(0, InstructionEncoder.ActiveForUs(50));```

trigger.SetInstruction(1, InstructionEncoder.UnactiveForUs(10));

trigger.SetInstruction(2, InstructionEncoder.End());## 📚 Documentazione Completa



// Send to device and executePer documentazione dettagliata, esempi avanzati e API reference:

client.SendTrgenMemory(trigger);

client.Start();**👉 [Leggi la Documentazione Completa](Documentation/trgen-unity.md)**

```

### Sommario Rapido

## 📚 Complete Documentation

- **[Caratteristiche dettagliate](Documentation/trgen-unity.md#-caratteristiche)**

For detailed documentation, advanced examples and API reference:- **[Guida all'installazione](Documentation/trgen-unity.md#-installazione)**  

- **[Documentazione API completa](Documentation/trgen-unity.md#-documentazione-api)**

**👉 [Read Complete Documentation](Documentation/trgen-unity.md)**- **[Esempi pratici](Documentation/trgen-unity.md#-esempi-avanzati)**

- **[Risoluzione problemi](Documentation/trgen-unity.md#-risoluzione-problemi)**

### Quick Summary

## 🎯 Tipi di Pin Supportati

- **[Detailed features](Documentation/trgen-unity.md#-characteristics)**

- **[Installation guide](Documentation/trgen-unity.md#-installation)**  | Tipo | Pin | Descrizione | Utilizzo Tipico |

- **[Complete API documentation](Documentation/trgen-unity.md#-api-documentation)**|------|-----|-------------|-----------------|

- **[Practical examples](Documentation/trgen-unity.md#-advanced-examples)**| **NeuroScan** | NS0-NS7 | Amplificatori NeuroScan | EEG, MEG, trigger di sincronizzazione |

- **[Troubleshooting](Documentation/trgen-unity.md#-troubleshooting)**| **Synamps** | SA0-SA7 | Amplificatori Synamps | EEG ad alta densità, ricerca |  

| **GPIO** | GPIO0-GPIO7 | Pin generici programmabili | Controllo dispositivi esterni |

## 🎯 Supported Pin Types| **TMS** | TMSO, TMSI | Stimolazione magnetica | TMS, controllo stimolatori |



| Type | Pins | Description | Typical Usage |## ⚡ Esempi d'Uso Rapidi

|------|------|-------------|---------------|

| **NeuroScan** | NS0-NS7 | NeuroScan amplifiers | EEG, MEG, synchronization triggers |### Esperimento EEG

| **Synamps** | SA0-SA7 | Synamps amplifiers | High-density EEG, research |  

| **GPIO** | GPIO0-GPIO7 | Generic programmable pins | External device control |```csharp

| **TMS** | TMSO, TMSI | Magnetic stimulation | TMS, stimulator control |// Marker di inizio trial

client.SendMarker(markerNS: trialNumber);

## ⚡ Quick Usage Examples

// Presenta stimolo e invia trigger

### EEG Experimentclient.StartTrigger(TrgenPin.NS1);

ShowStimulus();

```csharp

// Trial start marker// Marker di risposta utente  

client.SendMarker(markerNS: trialNumber);client.SendMarker(markerNS: responseCode);

```

// Present stimulus and send trigger

client.StartTrigger(TrgenPin.NS1);### Controllo Stimolatore TMS

ShowStimulus();

```csharp

// User response marker  // Attiva stimolatore tramite GPIO

client.SendMarker(markerNS: responseCode);client.StartTrigger(TrgenPin.GPIO0);

```

// Trigger di sincronizzazione su TMS

### TMS Stimulator Controlclient.StartTrigger(TrgenPin.TMSO);

```

```csharp

// Activate stimulator via GPIO### Sincronizzazione Multi-dispositivo

client.StartTrigger(TrgenPin.GPIO0);

```csharp

// Synchronization trigger on TMS// Trigger simultaneo su più sistemi

client.StartTrigger(TrgenPin.TMSO);client.StartTriggerList(new List<int> {

```    TrgenPin.NS0,    // EEG

    TrgenPin.GPIO3,  // Eye-tracker  

### Multi-device Synchronization    TrgenPin.SA5     // fMRI

});

```csharp```

// Simultaneous trigger on multiple systems

client.StartTriggerList(new List<int> {## 🔧 Requisiti Sistema

    TrgenPin.NS0,    // EEG

    TrgenPin.GPIO3,  // Eye-tracker  - **Unity:** 2021.3 o superiore

    TrgenPin.SA5     // fMRI- **Framework:** .NET Standard 2.1

});- **Piattaforme:** Windows, macOS, Linux

```- **TriggerBox:** Dispositivo CoSANLab con firmware compatibile

- **Rete:** Connessione Ethernet TCP/IP

## 🔧 System Requirements

## 🤝 Contribuire

- **Unity:** 2021.3 or higher

- **Framework:** .NET Standard 2.1I contributi sono benvenuti! Per contribuire:

- **Platforms:** Windows, macOS, Linux

- **TriggerBox:** CoSANLab device with compatible firmware1. Fork del repository

- **Network:** Ethernet TCP/IP connection2. Crea branch feature (`git checkout -b feature/AmazingFeature`)

3. Commit modifiche (`git commit -m 'Add AmazingFeature'`)

## 🤝 Contributing4. Push branch (`git push origin feature/AmazingFeature`)  

5. Apri Pull Request

Contributions are welcome! To contribute:

## 📄 Licenza

1. Fork the repository

2. Create feature branch (`git checkout -b feature/AmazingFeature`)Distribuito sotto licenza MIT. Vedi `LICENSE` per dettagli.

3. Commit changes (`git commit -m 'Add AmazingFeature'`)

4. Push branch (`git push origin feature/AmazingFeature`)  ## 📞 Supporto & Contatti

5. Open Pull Request

- **🐛 Issues:** [GitHub Issues](https://github.com/stefanolatini/trgen-unity/issues)

## 📄 License- **📧 Email:** stefanoelatini@hotmail.it  

- **🏛️ Lab:** [CoSANLab Roma](https://research.uniroma1.it/laboratorio/144782)

Distributed under MIT License. See `LICENSE` for details.- **📚 Docs:** [Documentazione Completa](Documentation/trgen-unity.md)



## 📞 Support & Contacts---



- **🐛 Issues:** [GitHub Issues](https://github.com/stefanolatini/trgen-unity/issues)<div align="center">

- **📧 Email:** stefanoelatini@hotmail.it  

- **🏛️ Lab:** [CoSANLab Rome](https://research.uniroma1.it/laboratorio/144782)**Sviluppato con ❤️ da [CoSANLab - Università di Roma La Sapienza](https://research.uniroma1.it/laboratorio/144782)**

- **📚 Docs:** [Complete Documentation](Documentation/trgen-unity.md)

*Per ricerca in neuroscienze cognitive e applicazioni di stimolazione cerebrale*

---

</div>

<div align="center">

**Developed with ❤️ by [CoSANLab - University of Rome La Sapienza](https://research.uniroma1.it/laboratorio/144782)**

*For cognitive neuroscience research and brain stimulation applications*

</div>