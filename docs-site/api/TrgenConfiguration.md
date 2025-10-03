# TrgenConfiguration class

**Generated using Legacy .NET Documentation Tools**

---

## Overview

Rappresenta una configurazione esportabile/importabile per i trigger TrGEN. Contiene tutti i parametri necessari per salvare e ripristinare lo stato dei trigger. 

```csharp
namespace Trgen
{
    public class TrgenConfiguration
}
```

## Constructors

### TriggerPortConfig

Costruttore di default 

**Signature:**
```csharp
public TriggerPortConfig()
```

## Properties

### TriggerPorts

Configurazioni specifiche per ogni porta di trigger 

**Signature:**
```csharp
public Dictionary<string, TriggerPortConfig> TriggerPorts { get; set; } = new Dictionary<string, TriggerPortConfig>();
```

### Version

Versione del formato di configurazione 

**Signature:**
```csharp
public string Version { get; set; } = "1.0";
```

### CreatedAt

Data di creazione della configurazione 

**Signature:**
```csharp
public DateTime CreatedAt { get; set; } = DateTime.Now;
```

### ModifiedAt

Data dell'ultima modifica 

**Signature:**
```csharp
public DateTime ModifiedAt { get; set; } = DateTime.Now;
```

### Description

Descrizione opzionale della configurazione 

**Signature:**
```csharp
public string Description { get; set; } = "";
```

### ProjectName

Nome del progetto/esperimento associato 

**Signature:**
```csharp
public string ProjectName { get; set; } = "";
```

### Author

Autore della configurazione 

**Signature:**
```csharp
public string Author { get; set; } = "";
```

### DefaultTriggerDurationUs

Durata standard del trigger in microsecondi 

**Signature:**
```csharp
public uint DefaultTriggerDurationUs { get; set; } = 40;
```

### DefaultLogLevel

Livello di verbosità di default per il logging 

**Signature:**
```csharp
public string DefaultLogLevel { get; set; } = "Warn";
```

### DefaultTimeoutMs

Timeout di connessione di default in millisecondi 

**Signature:**
```csharp
public int DefaultTimeoutMs { get; set; } = 2000;
```

### AutoResetEnabled

Se abilitare il reset automatico dopo l'invio del trigger 

**Signature:**
```csharp
public bool AutoResetEnabled { get; set; } = true;
```

### AutoResetDelayUs

Delay del reset automatico in microsecondi 

**Signature:**
```csharp
public uint AutoResetDelayUs { get; set; } = 100;
```

### Id

ID della porta (deve corrispondere agli ID in TrgenPin) 

**Signature:**
```csharp
public int Id { get; set; }
```

### Name

Nome descrittivo della porta 

**Signature:**
```csharp
public string Name { get; set; } = "";
```

### Type

Tipo di porta (NS, SA, GPIO, TMS) 

**Signature:**
```csharp
public string Type { get; set; } = "";
```

### Enabled

Se la porta è abilitata 

**Signature:**
```csharp
public bool Enabled { get; set; } = true;
```

### CustomDurationUs

Durata specifica del trigger per questa porta (se diversa dal default) 

**Signature:**
```csharp
public uint? CustomDurationUs { get; set; } = null;
```

### MemoryInstructions

Array completo della memoria delle istruzioni programmate per questa porta. Rappresenta l'intero contenuto della memoria del TrgenPort (tipicamente 32 elementi). Ogni elemento è un'istruzione codificata che definisce il comportamento del trigger. 

**Signature:**
```csharp
public uint[] MemoryInstructions { get; set; } = new uint[0];
```

### MemoryLength

Lunghezza della memoria per questa porta (numero massimo di istruzioni) 

**Signature:**
```csharp
public int MemoryLength { get; set; } = 32;
```

### LastInstructionIndex

Indice dell'ultima istruzione valida nella memoria (-1 se vuota) 

**Signature:**
```csharp
public int LastInstructionIndex { get; set; } = -1;
```

### ProgrammingState

Stato della programmazione della porta 

**Signature:**
```csharp
public PortProgrammingState ProgrammingState { get; set; } = PortProgrammingState.NotProgrammed;
```

### LastProgrammedAt

Timestamp dell'ultima programmazione di questa porta 

**Signature:**
```csharp
public DateTime LastProgrammedAt { get; set; } = DateTime.MinValue;
```

### Notes

Note o descrizione specifica per questa porta 

**Signature:**
```csharp
public string Notes { get; set; } = "";
```

### CustomSettings

Configurazioni personalizzate aggiuntive 

**Signature:**
```csharp
public Dictionary<string, object> CustomSettings { get; set; } = new Dictionary<string, object>();
```

### IpAddress

Indirizzo IP del dispositivo TrGEN 

**Signature:**
```csharp
public string IpAddress { get; set; } = "192.168.123.1";
```

### Port

Porta di comunicazione 

**Signature:**
```csharp
public int Port { get; set; } = 4242;
```

### TimeoutMs

Timeout di connessione in millisecondi 

**Signature:**
```csharp
public int TimeoutMs { get; set; } = 2000;
```

## Methods

### ConfigurationMetadata

Informazioni generali sulla configurazione 

**Signature:**
```csharp
public ConfigurationMetadata Metadata { get; set; } = new ConfigurationMetadata();
```

### DefaultSettings

Configurazioni di default per tutti i trigger 

**Signature:**
```csharp
public DefaultSettings Defaults { get; set; } = new DefaultSettings();
```

### NetworkSettings

Configurazioni di rete per la connessione al dispositivo TrGEN 

**Signature:**
```csharp
public NetworkSettings Network { get; set; } = new NetworkSettings();
```

---

*This documentation was generated using legacy .NET documentation extraction techniques.*
