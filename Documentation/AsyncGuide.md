# Guida all'uso delle versioni asincrone di TrgenClient

## Panoramica

Con l'introduzione delle versioni asincrone dei metodi principali, ora puoi scegliere tra due modalità di connessione:

1. **Modalità Sincrona**: `Connect()` + metodi sincroni
2. **Modalità Asincrona**: `ConnectAsync()` + metodi asincroni

## ⚠️ Importante: Quando usare quale modalità

### Problema identificato

Quando usi `ConnectAsync()`, il client avvia un worker loop asincrono (`WorkerLoopAsync()`), ma i metodi sincroni come `StartTrigger()` non aspettano il completamento delle operazioni. Questo può causare:

- Comandi che sembrano inviati ma non sono ancora stati trasmessi al dispositivo
- Mancanza di segnali sui pin di output
- Comportamento imprevedibile

### Soluzione: Usa metodi coerenti con il tipo di connessione

#### ✅ Connessione Sincrona (Raccomandato per Unity)
```csharp
var client = new TrgenClient();
client.Verbosity = LogLevel.Debug;

// Connessione sincrona
client.Connect();

// Usa metodi sincroni
client.StartTrigger(TrgenPin.NS5);
client.SendMarker(markerNS: 5);
client.StopTrigger();
```

#### ✅ Connessione Asincrona (Per applicazioni async/await)
```csharp
var client = new TrgenClient();
client.Verbosity = LogLevel.Debug;

// Connessione asincrona
await client.ConnectAsync();

// Usa metodi asincroni con await
await client.StartTriggerAsync(TrgenPin.NS5);
await client.SendMarkerAsync(markerNS: 5);
await client.StopTriggerAsync();
```

## 🔧 Nuovi metodi asincroni disponibili

### Metodi di base
- `StartAsync()` - Avvia l'esecuzione dei trigger
- `StopAsync()` - Ferma l'esecuzione dei trigger
- `SendTrgenMemoryAsync(TrgenPort t)` - Invia la memoria programmata
- `ProgramDefaultTriggerAsync(TrgenPort t, uint us = 20)` - Programma trigger default

### Metodi di alto livello
- `StartTriggerAsync(int triggerId)` - Trigger singolo
- `StartTriggerListAsync(List<int> triggerIds)` - Trigger multipli
- `SendMarkerAsync(int? markerNS, int? markerSA, int? markerGPIO, bool LSB)` - Marker codificati
- `StopTriggerAsync()` - Stop completo con reset

### Metodi di utility
- `ResetTriggerAsync(TrgenPort t)` - Reset trigger singolo
- `ResetAllAsync(List<int> ids)` - Reset multipli trigger

## 🎯 Esempi pratici

### Esempio 1: Trigger singolo con conferma
```csharp
public class TriggerController : MonoBehaviour
{
    private TrgenClient client;
    
    async void Start()
    {
        client = new TrgenClient();
        client.Verbosity = LogLevel.Debug;
        
        try
        {
            await client.ConnectAsync();
            Debug.Log("Connesso!");
            
            // Invia trigger e aspetta conferma
            await client.StartTriggerAsync(TrgenPin.NS5);
            Debug.Log("Trigger inviato e confermato!");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Errore: {ex.Message}");
        }
    }
}
```

### Esempio 2: Sequenza di trigger con timing preciso
```csharp
public async Task SendTimedSequence()
{
    // Trigger 1
    await client.StartTriggerAsync(TrgenPin.NS0);
    Debug.Log("Trigger 1 confermato");
    
    await Task.Delay(100); // Pausa garantita
    
    // Trigger 2
    await client.StartTriggerAsync(TrgenPin.NS1);
    Debug.Log("Trigger 2 confermato");
    
    await Task.Delay(100);
    
    // Stop finale
    await client.StopTriggerAsync();
    Debug.Log("Sequenza completata");
}
```

### Esempio 3: Test con verifica stato
```csharp
public async Task TestWithStatusCheck()
{
    // Invia marker
    await client.SendMarkerAsync(markerNS: 5);
    
    // Verifica stato (questi metodi rimangono sincroni)
    int level = client.GetLevel();
    bool ns0Active = (level & 0x01) != 0;
    bool ns2Active = (level & 0x04) != 0;
    
    Debug.Log($"NS0 attivo: {ns0Active}, NS2 attivo: {ns2Active}");
    
    // Dovrebbe essere true per entrambi (valore 5 = 101 binario)
    Assert.IsTrue(ns0Active && ns2Active);
}
```

## 🚨 Errori comuni da evitare

### ❌ NON fare questo:
```csharp
// Connessione asincrona ma metodi sincroni
await client.ConnectAsync();
client.StartTrigger(TrgenPin.NS5); // Potrebbe non essere inviato!
```

### ❌ NON fare questo:
```csharp
// Connessione sincrona ma metodi asincroni senza await
client.Connect();
_ = client.StartTriggerAsync(TrgenPin.NS5); // Fire-and-forget pericoloso
```

### ✅ Fai questo invece:
```csharp
// Coerenza: async con async
await client.ConnectAsync();
await client.StartTriggerAsync(TrgenPin.NS5);

// Oppure: sync con sync
client.Connect();
client.StartTrigger(TrgenPin.NS5);
```

## 🧪 Testing e Debug

### Script di test completo
Usa il nuovo `AsyncTriggerTest.cs` per verificare il funzionamento:

1. Attach lo script a un GameObject
2. Configura IP e porta del TriggerBox
3. Osserva i log per verificare la conferma di invio
4. Usa il Context Menu per test aggiuntivi

### Debugging avanzato
```csharp
client.Verbosity = LogLevel.Debug; // Mostra tutti i pacchetti
await client.StartTriggerAsync(TrgenPin.NS5);
// Nei log dovresti vedere:
// - "Sending packet 0x05000001" (SendTrgenMemory)
// - "Sending packet 0x00000002" (Start)
// - "ACK" responses per ogni comando
```

## 📋 Checklist per la migrazione

Se stai migrando da codice esistente:

- [ ] Verifica quale tipo di connessione stai usando
- [ ] Se usi `ConnectAsync()`, aggiungi `await` a tutti i metodi trigger
- [ ] Se usi `Connect()`, mantieni i metodi sincroni
- [ ] Testa con logging Debug abilitato
- [ ] Verifica che i segnali appaiano sui pin fisici
- [ ] Aggiungi gestione errori con try/catch per metodi async

## 🔧 Troubleshooting

### Problema: "I trigger non vengono inviati"
**Soluzione**: Verifica di usare metodi asincroni con `await` se hai usato `ConnectAsync()`

### Problema: "TimeoutException durante l'invio"
**Soluzione**: Controlla la connessione di rete e aumenta il timeout se necessario

### Problema: "Comportamento erratico"
**Soluzione**: Usa sempre `await` con i metodi asincroni, non fire-and-forget