---
home: true
heroImage: /hero.png
heroText: TrGEN Unity
tagline: Libreria Unity professionale per la comunicazione con dispositivi TriggerBox di CoSANLab
actionText: Inizia Subito →
actionLink: /guide/
features:
- title: 🎯 Semplice da Usare
  details: API intuitiva e ben documentata per l'integrazione rapida nei progetti Unity
- title: ⚡ Prestazioni Elevate
  details: Comunicazione asincrona ottimizzata per trigger real-time
- title: 🔧 Configurazione Avanzata
  details: Sistema completo di configurazione con export/import JSON
footer: MIT Licensed | Copyright © 2025 CoSANLab
---

# TrGEN Unity Package

TrGEN Unity è una libreria professionale per Unity che permette la comunicazione
con i dispositivi **TriggerBox** di CoSANLab, utilizzati in ricerca neuroscientifica
per la sincronizzazione di trigger con sistemi EEG, TMS e altri dispositivi.

## 🚀 Installazione Rapida

### Via OpenUPM (Raccomandato)
```bash
# Installa tramite OpenUPM CLI
openupm add com.cosanlab.trgen
```

### Via Package Manager
1. Apri Unity Package Manager
2. Clicca "+" → "Add package from git URL"
3. Inserisci: `https://github.com/stefanolatini/trgen-unity.git`

## 💡 Esempio Veloce

```csharp
using Trgen;
using UnityEngine;

public class TriggerExample : MonoBehaviour
{
    private TrgenClient client;
    
    async void Start()
    {
        // Connessione al dispositivo
        client = new TrgenClient();
        await client.ConnectAsync("192.168.1.100", 4000);
        
        // Invio trigger
        await client.StartTriggerAsync(TrgenPin.NS0);
        
        Debug.Log("Trigger inviato con successo!");
    }
}
```

## 📋 Funzionalità Principali

- **Comunicazione Asincrona**: Connessione TCP non-bloccante
- **Gestione Configurazioni**: Export/Import JSON completo
- **Trigger Multipli**: Supporto per pin NeuroScan, Synamps, TMS, GPIO
- **Programmazione Hardware**: Caricamento automatico delle sequenze
- **Debug Avanzato**: Logging dettagliato e diagnostica errori
- **Unity Integration**: Compatibile con Unity 2021.3 LTS+

## 🔗 Link Utili

- [📖 Guida Completa](/guide/)
- [📚 Documentazione API](/api/)
- [💻 Repository GitHub](https://github.com/stefanolatini/trgen-unity)
- [📦 Package OpenUPM](https://openupm.com/packages/com.cosanlab.trgen/)
- [🧪 Esempi di Codice](/examples/)

## 📞 Supporto

Per supporto tecnico o domande:
- Apri una [GitHub Issue](https://github.com/stefanolatini/trgen-unity/issues)
- Contatta: support@cosanlab.org

## 🏆 Stato del Progetto

![Build Status](https://github.com/stefanolatini/trgen-unity/workflows/📚%20Deploy%20VuePress%20Documentation%20to%20GitHub%20Pages/badge.svg)
![Latest Release](https://img.shields.io/github/v/release/stefanolatini/trgen-unity?label=release)
![OpenUPM](https://img.shields.io/npm/v/com.cosanlab.trgen?label=openupm&registry_uri=https://package.openupm.com)

---

<div style="text-align: center; margin-top: 2rem;">
  <strong>🧠 Sviluppato per la ricerca neuroscientifica di livello mondiale</strong>
</div>