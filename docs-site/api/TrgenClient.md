# TrgenClient class

**Generated using Legacy .NET Documentation Tools**

---

## Overview

Gestisce la connessione, la comunicazione e il controllo dei trigger hardware tramite il protocollo TrGEN. Permette di programmare, resettare e inviare segnali di trigger su diversi tipi di porte (NeuroScan, Synamaps, GPIO). 

```csharp
namespace Trgen
{
    public class TrgenClient
}
```

## Methods

### Connect

Tenta di connettersi al dispositivo TrGEN e aggiorna la configurazione interna. 

**Signature:**
```csharp
public void Connect()
```

### StopTrigger

Ferma tutti i trigger attivi e resetta lo stato dei pin. 

**Signature:**
```csharp
public void StopTrigger()
```

---

*This documentation was generated using legacy .NET documentation extraction techniques.*
