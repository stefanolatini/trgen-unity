# Guida Rapida

## Primo Utilizzo

```csharp
using Trgen;
using UnityEngine;

public class MyTriggerController : MonoBehaviour
{
    private TrgenClient client;

    async void Start()
    {
        client = new TrgenClient();
        await client.ConnectAsync('192.168.1.100', 4000);
        Debug.Log('Connesso!');
    }

    async void SendTrigger()
    {
        await client.StartTriggerAsync(TrgenPin.NS0);
    }
}
```

