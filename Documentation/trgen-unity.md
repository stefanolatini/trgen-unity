# trgen-unity
A Unity Package used to communicate with TriggerBox


## Getting started

To install this package add this package inside the `Packages/manifest.json` file

```json
{
  "dependencies": {
    ...
    ...
    "com.cosanlab.trgen": "1.0.0",
  }
```

## Example


```cs
using UnityEngine;
using System.Collections.Generic;

namespace Trgen.Demo
{
    public class TriggerBoxExample : MonoBehaviour
    {
        private TrgenClient client;

        void Start()
        {
            // Default IP "192.168.123.1"
            // Default Port = 4242
            client = new TrgenClient();

            // Check availability
            Debug.Log("TriggerBox available: " + client.IsAvailable());

            // Example: start single TrgenPort Pin
            client.StartTrigger(TriggerPin.NS3);

            // Example 2: start multiple TrgenPort Pins
            var pins = new List<int> { TriggerPin.GPIO0, TriggerPin.SA2 };
            client.StartTriggerList(pins);

            // Read state
            int level = client.GetLevel();
            Debug.Log("Level: " + level);
        }
    }
}

```
