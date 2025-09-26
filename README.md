<h1 align="center">TriggerBox CLI</h1>

![Unity](https://img.shields.io/badge/Unity-2022.3%2B-black?logo=unity)
[![GitHub release](https://img.shields.io/github/v/release/stefanolatini/trgen-unity?sort=semver)](https://github.com/stefanolatini/trgen-unity/releases)
![openupm](https://img.shields.io/badge/dynamic/json?color=brightgreen&label=downloads&query=%24.downloads&suffix=%2Fmonth&url=https%3A%2F%2Fpackage.openupm.com%2Fdownloads%2Fpoint%2Flast-month%2Fcom.cosanlab.trgen-importer)
![downloads](https://img.shields.io/badge/dynamic/json?color=brightgreen&label=downloads&query=%24.downloads&suffix=%2Fmonth&url=https%3A%2F%2Fpackage.openupm.com%2Fdownloads%2Fpoint%2Flast-month%2Fcom.cosanlab.trgen-importer)

<p align="center">
  <img src="images/banner.png" alt="TriggerBox Banner" width="600px" height="300px">
</p>

<h3 align="center"> A Unity library that manages Ethernet socket communication with the CoSANLab TriggerBox device </h3>

---

## Installation

You can install **TRGen** via [OpenUPM](https://openupm.com) using one of the following methods:

### Using the OpenUPM CLI

If you have `openupm-cli` installed:

```bash
openupm add com.cosanlab.trgen


## Getting started

To install this package add this package inside the `Packages/manifest.json` file

```json
{
  "name": "OpenUPM",
  "url": "https://package.openupm.com",
  "scopes": [
    "com.cosanlab.trgen"
  ]
}
```

## Example


```cs
using System;
using System.Collections;
using Trgen;
using UnityEngine;

public class TrgenExample : MonoBehaviour
{
  // TrgenClient instance is to be kept alive as long as you need it
  TrgenClient client;
  private String timeStr = "";
  private bool isTimerGoing;
  private string timePlaying_Str;
  private TimeSpan timePlaying;
  private float sectionCurrentTime;
    public void StopTimer()
    {
        isTimerGoing = false;
        timeStr = "00:00.0";
    }

  public async void TriggerConnect()
    {
        if (client != null && client.Connected)
            throw new InvalidOperationException("Already connected");
        else
        {
            client = new TrgenClient();
            //client.Connect();
            client.Verbosity = LogLevel.Debug;
            await client.ConnectAsync();
        }
    }

  public void TriggerSend()
  {
    if (!client.Connected)
        throw new InvalidOperationException("Connection failed");

    // Connected! (:
    client.StartTrigger(TriggerPin.NS5);
    // opzionalmente reset
    client.ResetAll(TriggerPin.AllNs);
  }

  public void TriggerSendLoop()
  {
    StartCoroutine(SendLoop());
  }

  private IEnumerator SendLoop()
  {
    if (!client.Connected)
      throw new InvalidOperationException("Connection failed");
    while (isTimerGoing)
    {
      sectionCurrentTime += Time.deltaTime;
      timePlaying = TimeSpan.FromSeconds(sectionCurrentTime);
      timePlaying_Str = timePlaying.ToString("mm':'ss'.'f");
      timeStr = timePlaying_Str;
      client.StartTrigger(TriggerPin.NS5);
      // opzionalmente reset
      client.ResetAll(TriggerPin.AllNs);
      yield return new WaitForSeconds (1);
    }
  }
}

```
