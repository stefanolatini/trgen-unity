using System;
using System.Collections;
using Trgen;
using UnityEngine;

public class TrgenExample : MonoBehaviour
{
  // TriggerClient instance is to be kept alive as long as you need it
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
          client.Verbosity = LogLevel.Debug;
          await client.ConnectAsync();
        }
    }

  public void TriggerSend()
  {
    if (!client.Connected)
        throw new InvalidOperationException("Connection failed");

    // Connected! (:
    client.StartTrigger(TrgenPin.NS5);
    // opzionalmente reset
    client.ResetAll(TrgenPin.AllNs);
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
      client.StartTrigger(TrgenPin.NS5);
      // opzionalmente reset
      client.ResetAll(TrgenPin.AllNs);
      yield return new WaitForSeconds (1);
    }
  }
}