# API Reference

Documentazione completa delle classi TrGEN Unity generate con tecniche legacy .NET.

## Classi Principali

### [TrgenImplementation](./TrgenImplementation.md)

Rappresenta la configurazione hardware e le capacità specifiche di un dispositivo TriggerBox. Questa classe incapsula le informazioni sulla configurazione hardware del dispositivo, inclusi il numero di pin disponibili per ogni tipo e la dimensione della memoria programmabile per i trigger. I dati vengono ottenuti direttamente dal dispositivo durante il processo di connessione. 

**Membri documentati:** 0

---

### [TrgenConfiguration](./TrgenConfiguration.md)

Rappresenta una configurazione esportabile/importabile per i trigger TrGEN. Contiene tutti i parametri necessari per salvare e ripristinare lo stato dei trigger. 

**Membri documentati:** 31

---

### [TrgenClient](./TrgenClient.md)

Gestisce la connessione, la comunicazione e il controllo dei trigger hardware tramite il protocollo TrGEN. Permette di programmare, resettare e inviare segnali di trigger su diversi tipi di porte (NeuroScan, Synamaps, GPIO). 

**Membri documentati:** 2

---

### [TrgenPort](./TrgenPort.md)

Rappresenta un trigger programmabile, con memoria interna per le istruzioni. 

**Membri documentati:** 3

---

## Informazioni Tecniche

Questa documentazione è stata generata utilizzando:

- **XML Documentation Comments**: Standard .NET (`/// <summary>`)
- **Pattern Matching**: Analisi testuale con regex
- **Legacy XML Processing**: Parsing ElementTree classico
- **VuePress Output**: Formato moderno per il web

