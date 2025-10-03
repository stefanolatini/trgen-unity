# Connessione Base

## Connessione Semplice

```csharp
using Trgen;
using UnityEngine;

public class BasicConnection : MonoBehaviour
{
    private TrgenClient client;

    async void Start()
    {
        client = new TrgenClient();
        
        try
        {
            await client.ConnectAsync('192.168.1.100', 4000);
            Debug.Log('Connessione stabilita!');
            
            var impl = await client.RequestImplementationAsync();
            Debug.Log($'Pin GPIO: {impl.GpioNum}');
        }
        catch (System.Exception ex)
        {
            Debug.LogError($'Errore: {ex.Message}');
        }
    }
}
```

