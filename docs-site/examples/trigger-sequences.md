# Sequenze di Trigger

## Trigger Multipli

```csharp
using Trgen;
using UnityEngine;
using System.Threading.Tasks;

public class TriggerSequence : MonoBehaviour
{
    private TrgenClient client;

    async void Start()
    {
        client = new TrgenClient();
        await client.ConnectAsync('192.168.1.100', 4000);
        
        await SendSequence();
    }

    private async Task SendSequence()
    {
        // Trigger iniziale
        await client.StartTriggerAsync(TrgenPin.NS0);
        await Task.Delay(100);
        
        // Trigger intermedio
        await client.StartTriggerAsync(TrgenPin.GPIO0);
        await Task.Delay(200);
        
        // Trigger finale
        await client.StartTriggerAsync(TrgenPin.NS1);
        
        Debug.Log('Sequenza completata!');
    }
}
```

