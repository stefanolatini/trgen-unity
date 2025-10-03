# Gestione Configurazioni

## Import Configurazione JSON

```csharp
using Trgen;
using UnityEngine;
using System.IO;

public class ConfigManager : MonoBehaviour
{
    async void LoadConfiguration()
    {
        var client = new TrgenClient();
        await client.ConnectAsync('192.168.1.100', 4000);
        
        string configPath = Path.Combine(
            Application.streamingAssetsPath, 
            'trigger_config.json'
        );
        
        bool success = await client.ImportConfiguration(
            configPath,
            applyNetworkSettings: false,
            programPorts: true
        );
        
        if (success)
        {
            Debug.Log('Configurazione caricata!');
        }
    }
}
```

