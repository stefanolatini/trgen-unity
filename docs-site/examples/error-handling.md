# Gestione Errori

## Try-Catch Avanzato

```csharp
using Trgen;
using UnityEngine;
using System;

public class ErrorHandling : MonoBehaviour
{
    async void SafeConnection()
    {
        var client = new TrgenClient();
        
        try
        {
            await client.ConnectAsync('192.168.1.100', 4000);
            Debug.Log('Connesso con successo!');
        }
        catch (TimeoutException ex)
        {
            Debug.LogError($'Timeout: {ex.Message}');
        }
        catch (Exception ex)
        {
            Debug.LogError($'Errore generico: {ex.Message}');
        }
        finally
        {
            client?.Dispose();
        }
    }
}
```

