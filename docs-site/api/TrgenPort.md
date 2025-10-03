# TrgenPort class

**Generated using Legacy .NET Documentation Tools**

---

## Overview

Rappresenta un trigger programmabile, con memoria interna per le istruzioni. 

```csharp
namespace Trgen
{
    public class TrgenPort
}
```

## Properties

### Id

Identificatore del trigger. 

**Signature:**
```csharp
public int Id { get; }
```

### Type

Tipologia di porta associata al trigger. Es. NS, SA, TMSI, TMSO, GPIO. 

**Signature:**
```csharp
public TriggerType Type { get; set; }
```

### Memory

Memoria delle istruzioni del trigger. 

**Signature:**
```csharp
public uint[] Memory { get; }
```

---

*This documentation was generated using legacy .NET documentation extraction techniques.*
