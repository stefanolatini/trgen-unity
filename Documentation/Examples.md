# Esempi Pratici - TRGen Unity

Questa pagina contiene esempi pratici e script pronti all'uso per il package TRGen Unity.

---

## 📋 Indice

- [Script Base](#script-base)
- [Esperimenti EEG/MEG](#esperimenti-eegmeg)
- [Controllo Stimolatori](#controllo-stimolatori)
- [Sincronizzazione Timeline](#sincronizzazione-timeline)
- [Test e Debug](#test-e-debug)
- [Applicazioni Avanzate](#applicazioni-avanzate)

---

## Script Base

### TriggerManager - Gestore Centralizzato

```csharp
using UnityEngine;
using System.Threading.Tasks;
using Trgen;

[System.Serializable]
public class TriggerSettings
{
    [Header("Connection")]
    public string deviceIP = "192.168.123.1";
    public int devicePort = 4242;
    public int timeoutMs = 2000;
    
    [Header("Logging")]
    public LogLevel verbosity = LogLevel.Info;
    
    [Header("Auto Connect")]
    public bool connectOnStart = true;
    public bool reconnectOnError = true;
}

public class TriggerManager : MonoBehaviour
{
    [Header("Settings")]
    public TriggerSettings settings = new TriggerSettings();
    
    [Header("Status")]
    [SerializeField] private bool isConnected = false;
    
    private TrgenClient client;
    
    public static TriggerManager Instance { get; private set; }
    
    // Eventi
    public System.Action OnConnected;
    public System.Action OnDisconnected;
    public System.Action<string> OnError;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        InitializeClient();
    }
    
    async void Start()
    {
        if (settings.connectOnStart)
        {
            await ConnectAsync();
        }
    }
    
    void InitializeClient()
    {
        client = new TrgenClient(settings.deviceIP, settings.devicePort, settings.timeoutMs);
        client.Verbosity = settings.verbosity;
    }
    
    public async Task<bool> ConnectAsync()
    {
        try
        {
            if (client.Connected)
            {
                Debug.LogWarning("TriggerManager: Already connected");
                return true;
            }
            
            Debug.Log("TriggerManager: Connecting...");
            await client.ConnectAsync();
            
            isConnected = true;
            Debug.Log("TriggerManager: Connected successfully");
            OnConnected?.Invoke();
            
            return true;
        }
        catch (System.Exception ex)
        {
            isConnected = false;
            string error = $"TriggerManager: Connection failed - {ex.Message}";
            Debug.LogError(error);
            OnError?.Invoke(error);
            
            if (settings.reconnectOnError)
            {
                // Riprova dopo 2 secondi
                await Task.Delay(2000);
                return await ConnectAsync();
            }
            
            return false;
        }
    }
    
    public void Disconnect()
    {
        if (client != null && client.Connected)
        {
            client.StopTrigger();
            client.Disconnect();
            isConnected = false;
            Debug.Log("TriggerManager: Disconnected");
            OnDisconnected?.Invoke();
        }
    }
    
    // API pubbliche
    public bool IsConnected => client?.Connected ?? false;
    
    public void SendTrigger(int pinId)
    {
        if (!IsConnected)
        {
            Debug.LogError("TriggerManager: Not connected");
            return;
        }
        
        try
        {
            client.StartTrigger(pinId);
            Debug.Log($"TriggerManager: Trigger sent on pin {pinId}");
        }
        catch (System.Exception ex)
        {
            OnError?.Invoke($"Failed to send trigger: {ex.Message}");
        }
    }
    
    public void SendMarker(int? markerNS = null, int? markerSA = null, int? markerGPIO = null, bool LSB = false)
    {
        if (!IsConnected)
        {
            Debug.LogError("TriggerManager: Not connected");
            return;
        }
        
        try
        {
            client.SendMarker(markerNS, markerSA, markerGPIO, LSB);
            Debug.Log($"TriggerManager: Marker sent - NS:{markerNS} SA:{markerSA} GPIO:{markerGPIO}");
        }
        catch (System.Exception ex)
        {
            OnError?.Invoke($"Failed to send marker: {ex.Message}");
        }
    }
    
    public void EmergencyStop()
    {
        if (client != null && client.Connected)
        {
            client.StopTrigger();
            Debug.Log("TriggerManager: Emergency stop executed");
        }
    }
    
    void OnDestroy()
    {
        Disconnect();
    }
    
    // Inspector GUI
    void OnGUI()
    {
        if (!Application.isPlaying) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label($"TriggerBox Status: {(IsConnected ? "Connected" : "Disconnected")}");
        
        if (!IsConnected)
        {
            if (GUILayout.Button("Connect"))
            {
                _ = ConnectAsync();
            }
        }
        else
        {
            if (GUILayout.Button("Disconnect"))
            {
                Disconnect();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Test Trigger NS5"))
            {
                SendTrigger(TrgenPin.NS5);
            }
            
            if (GUILayout.Button("Test Marker (5)"))
            {
                SendMarker(markerNS: 5);
            }
            
            if (GUILayout.Button("Emergency Stop"))
            {
                EmergencyStop();
            }
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
```

---

## Esperimenti EEG/MEG

### Script per Esperimento Oddball

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Trgen;

public class OddballExperiment : MonoBehaviour
{
    [Header("Experiment Settings")]
    public int totalTrials = 100;
    public float stimulusDuration = 0.1f;
    public float isiMin = 1.0f;
    public float isiMax = 2.0f;
    public float oddballProbability = 0.2f;
    
    [Header("Trigger Codes")]
    public int standardTrigger = 1;
    public int oddballTrigger = 2;
    public int responseTrigger = 10;
    public int startExperimentTrigger = 100;
    public int endExperimentTrigger = 200;
    
    [Header("UI")]
    public GameObject standardStimulus;
    public GameObject oddballStimulus;
    public KeyCode responseKey = KeyCode.Space;
    
    private TriggerManager triggerManager;
    private int currentTrial = 0;
    private bool experimentRunning = false;
    private List<TrialData> trialSequence;
    
    [System.Serializable]
    public class TrialData
    {
        public int trialNumber;
        public bool isOddball;
        public int triggerCode;
        public float isi;
    }
    
    void Start()
    {
        triggerManager = TriggerManager.Instance;
        if (triggerManager == null)
        {
            Debug.LogError("TriggerManager not found! Add TriggerManager to scene.");
            return;
        }
        
        GenerateTrialSequence();
    }
    
    void GenerateTrialSequence()
    {
        trialSequence = new List<TrialData>();
        
        for (int i = 1; i <= totalTrials; i++)
        {
            bool isOddball = Random.value < oddballProbability;
            
            TrialData trial = new TrialData
            {
                trialNumber = i,
                isOddball = isOddball,
                triggerCode = isOddball ? oddballTrigger : standardTrigger,
                isi = Random.Range(isiMin, isiMax)
            };
            
            trialSequence.Add(trial);
        }
        
        Debug.Log($"Generated {trialSequence.Count} trials - {trialSequence.FindAll(t => t.isOddball).Count} oddballs");
    }
    
    public void StartExperiment()
    {
        if (experimentRunning) return;
        
        if (!triggerManager.IsConnected)
        {
            Debug.LogError("TriggerBox not connected!");
            return;
        }
        
        experimentRunning = true;
        currentTrial = 0;
        
        // Trigger di inizio esperimento
        triggerManager.SendMarker(markerNS: startExperimentTrigger);
        
        StartCoroutine(RunExperiment());
    }
    
    IEnumerator RunExperiment()
    {
        Debug.Log("Starting Oddball Experiment");
        
        foreach (var trial in trialSequence)
        {
            currentTrial = trial.trialNumber;
            
            // Pre-stimulus interval
            yield return new WaitForSeconds(trial.isi);
            
            // Send stimulus trigger
            triggerManager.SendMarker(markerNS: trial.triggerCode);
            
            // Show stimulus
            ShowStimulus(trial.isOddball);
            
            // Stimulus duration
            float stimulusStart = Time.time;
            yield return new WaitForSeconds(stimulusDuration);
            
            // Hide stimulus
            HideStimulus();
            
            // Monitor response during response window (1.5 seconds)
            yield return StartCoroutine(MonitorResponse(1.5f, stimulusStart));
            
            // Trial completion log
            Debug.Log($"Trial {trial.trialNumber}: {(trial.isOddball ? "Oddball" : "Standard")}");
        }
        
        // End of experiment
        triggerManager.SendMarker(markerNS: endExperimentTrigger);
        experimentRunning = false;
        
        Debug.Log("Experiment completed!");
    }
    
    IEnumerator MonitorResponse(float responseWindow, float stimulusStart)
    {
        float windowStart = Time.time;
        bool responseDetected = false;
        
        while (Time.time - windowStart < responseWindow && !responseDetected)
        {
            if (Input.GetKeyDown(responseKey))
            {
                float reactionTime = Time.time - stimulusStart;
                
                // Send response trigger
                triggerManager.SendMarker(markerNS: responseTrigger);
                
                Debug.Log($"Response detected - RT: {reactionTime:F3}s");
                responseDetected = true;
            }
            
            yield return null;
        }
    }
    
    void ShowStimulus(bool isOddball)
    {
        if (isOddball && oddballStimulus != null)
        {
            oddballStimulus.SetActive(true);
            standardStimulus?.SetActive(false);
        }
        else if (standardStimulus != null)
        {
            standardStimulus.SetActive(true);
            oddballStimulus?.SetActive(false);
        }
    }
    
    void HideStimulus()
    {
        standardStimulus?.SetActive(false);
        oddballStimulus?.SetActive(false);
    }
    
    public void StopExperiment()
    {
        if (experimentRunning)
        {
            StopAllCoroutines();
            experimentRunning = false;
            HideStimulus();
            
            triggerManager.SendMarker(markerNS: endExperimentTrigger);
            triggerManager.EmergencyStop();
            
            Debug.Log("Experiment stopped manually");
        }
    }
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(Screen.width - 250, 10, 240, 150));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label($"Oddball Experiment");
        GUILayout.Label($"Trial: {currentTrial}/{totalTrials}");
        GUILayout.Label($"Status: {(experimentRunning ? "Running" : "Stopped")}");
        
        if (!experimentRunning)
        {
            if (GUILayout.Button("Start Experiment"))
            {
                StartExperiment();
            }
        }
        else
        {
            if (GUILayout.Button("Stop Experiment"))
            {
                StopExperiment();
            }
        }
        
        if (GUILayout.Button("Regenerate Trials"))
        {
            GenerateTrialSequence();
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
```

### Script per Registrazione EEG Continua

```csharp
using UnityEngine;
using System.Collections;
using Trgen;

public class ContinuousEEG : MonoBehaviour
{
    [Header("Recording Settings")]
    public bool autoStart = true;
    public float markerInterval = 5.0f; // Marker ogni 5 secondi
    
    [Header("Event Triggers")]
    public int startRecordingTrigger = 10;
    public int stopRecordingTrigger = 20;
    public int timestampTrigger = 30;
    public int eventTrigger = 40;
    
    private TriggerManager triggerManager;
    private bool recording = false;
    private Coroutine recordingCoroutine;
    
    void Start()
    {
        triggerManager = TriggerManager.Instance;
        
        if (autoStart && triggerManager != null)
        {
            triggerManager.OnConnected += OnTriggerBoxConnected;
        }
    }
    
    void OnTriggerBoxConnected()
    {
        StartRecording();
    }
    
    public void StartRecording()
    {
        if (recording || !triggerManager.IsConnected) return;
        
        recording = true;
        
        // Trigger di inizio registrazione
        triggerManager.SendMarker(markerNS: startRecordingTrigger);
        
        // Avvia coroutine per marker periodici
        recordingCoroutine = StartCoroutine(RecordingLoop());
        
        Debug.Log("EEG Recording started");
    }
    
    public void StopRecording()
    {
        if (!recording) return;
        
        recording = false;
        
        // Ferma la coroutine
        if (recordingCoroutine != null)
        {
            StopCoroutine(recordingCoroutine);
        }
        
        // Trigger di fine registrazione
        triggerManager.SendMarker(markerNS: stopRecordingTrigger);
        
        Debug.Log("EEG Recording stopped");
    }
    
    IEnumerator RecordingLoop()
    {
        while (recording)
        {
            yield return new WaitForSeconds(markerInterval);
            
            if (recording && triggerManager.IsConnected)
            {
                // Invia marker timestamp
                triggerManager.SendMarker(markerNS: timestampTrigger);
                Debug.Log($"Timestamp marker sent at {Time.time:F2}s");
            }
        }
    }
    
    // Metodi per eventi esterni
    public void SendEventMarker(int eventCode)
    {
        if (recording && triggerManager.IsConnected)
        {
            triggerManager.SendMarker(markerNS: eventCode);
            Debug.Log($"Event marker sent: {eventCode}");
        }
    }
    
    public void OnUserResponse()
    {
        SendEventMarker(eventTrigger);
    }
    
    void OnDestroy()
    {
        StopRecording();
    }
    
    // Controlli da tastiera per test
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (!recording) StartRecording();
            else StopRecording();
        }
        
        if (Input.GetKeyDown(KeyCode.E))
        {
            OnUserResponse();
        }
    }
}
```

---

## Controllo Stimolatori

### Controller TMS

```csharp
using UnityEngine;
using System.Collections;
using Trgen;

public class TMSController : MonoBehaviour
{
    [Header("TMS Settings")]
    public float pulseInterval = 1.0f;
    public int numberOfPulses = 10;
    public float intensityPercent = 50f;
    
    [Header("Safety")]
    public float maxPulsesPerMinute = 60;
    public bool enableSafetyLimits = true;
    
    [Header("Trigger Pins")]
    public int tmsTrgenPin = TrgenPin.TMSO;
    public int intensityPin = TrgenPin.GPIO0;
    
    private TriggerManager triggerManager;
    private bool stimulating = false;
    private int pulsesDelivered = 0;
    private float sessionStartTime;
    
    void Start()
    {
        triggerManager = TriggerManager.Instance;
        sessionStartTime = Time.time;
    }
    
    public void StartStimulation()
    {
        if (stimulating || !triggerManager.IsConnected)
        {
            Debug.LogError("Cannot start stimulation");
            return;
        }
        
        if (enableSafetyLimits && !SafetyCheck())
        {
            Debug.LogError("Safety limits exceeded");
            return;
        }
        
        stimulating = true;
        pulsesDelivered = 0;
        
        StartCoroutine(StimulationSequence());
        Debug.Log($"TMS Stimulation started - {numberOfPulses} pulses at {1/pulseInterval:F1} Hz");
    }
    
    bool SafetyCheck()
    {
        float minutesElapsed = (Time.time - sessionStartTime) / 60f;
        float projectedRate = numberOfPulses / minutesElapsed;
        
        if (projectedRate > maxPulsesPerMinute)
        {
            Debug.LogWarning($"Projected pulse rate ({projectedRate:F1}/min) exceeds safety limit");
            return false;
        }
        
        return true;
    }
    
    IEnumerator StimulationSequence()
    {
        // Impostazione intensità (esempio: controllo tramite GPIO)
        SetStimulatorIntensity(intensityPercent);
        yield return new WaitForSeconds(0.1f);
        
        for (int i = 0; i < numberOfPulses && stimulating; i++)
        {
            // Invia impulso TMS
            triggerManager.SendTrigger(tmsTrgenPin);
            pulsesDelivered++;
            
            Debug.Log($"TMS Pulse {i + 1}/{numberOfPulses} delivered");
            
            // Attendi intervallo tra impulsi
            if (i < numberOfPulses - 1)
            {
                yield return new WaitForSeconds(pulseInterval);
            }
        }
        
        stimulating = false;
        Debug.Log($"TMS Stimulation completed - {pulsesDelivered} pulses delivered");
    }
    
    void SetStimulatorIntensity(float percent)
    {
        // Esempio: controllo intensità tramite pin GPIO
        // L'implementazione dipende dal protocollo dello stimolatore
        int intensityCode = Mathf.RoundToInt(percent);
        triggerManager.SendMarker(markerGPIO: intensityCode);
    }
    
    public void StopStimulation()
    {
        if (stimulating)
        {
            stimulating = false;
            StopAllCoroutines();
            
            // Trigger di emergenza
            triggerManager.EmergencyStop();
            
            Debug.Log($"TMS Stimulation stopped - {pulsesDelivered} pulses delivered");
        }
    }
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 250, 300, 200));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("TMS Controller");
        GUILayout.Label($"Status: {(stimulating ? "Stimulating" : "Ready")}");
        GUILayout.Label($"Pulses: {pulsesDelivered}/{numberOfPulses}");
        GUILayout.Label($"Intensity: {intensityPercent}%");
        
        if (!stimulating)
        {
            if (GUILayout.Button("Start Stimulation"))
            {
                StartStimulation();
            }
        }
        else
        {
            if (GUILayout.Button("STOP (Emergency)"))
            {
                StopStimulation();
            }
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
```

---

## Sincronizzazione Timeline

### Timeline Trigger Track

```csharp
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using Trgen;

// Marker asset per Timeline
[System.Serializable]
public class TriggerMarker : INotification, INotificationOptionProvider
{
    [Header("Trigger Settings")]
    public int triggerCode = 1;
    public TriggerType triggerType = TriggerType.NeuroScan;
    public bool retroactive = false;
    public bool emitOnce = false;
    
    public PropertyName id => new PropertyName("TriggerMarker");
    public NotificationFlags flags => 
        (retroactive ? NotificationFlags.Retroactive : default) |
        (emitOnce ? NotificationFlags.TriggerOnce : default);
}

public enum TriggerType
{
    NeuroScan,
    Synamps, 
    GPIO,
    TMS
}

// Receiver per processare i marker
public class TriggerReceiver : MonoBehaviour, INotificationReceiver
{
    private TriggerManager triggerManager;
    
    void Start()
    {
        triggerManager = TriggerManager.Instance;
    }
    
    public void OnNotify(Playable origin, INotification notification, object context)
    {
        if (notification is TriggerMarker marker && triggerManager?.IsConnected == true)
        {
            switch (marker.triggerType)
            {
                case TriggerType.NeuroScan:
                    triggerManager.SendMarker(markerNS: marker.triggerCode);
                    break;
                case TriggerType.Synamps:
                    triggerManager.SendMarker(markerSA: marker.triggerCode);
                    break;
                case TriggerType.GPIO:
                    triggerManager.SendMarker(markerGPIO: marker.triggerCode);
                    break;
                case TriggerType.TMS:
                    triggerManager.SendTrigger(TrgenPin.TMSO);
                    break;
            }
            
            Debug.Log($"Timeline Trigger: {marker.triggerType} = {marker.triggerCode}");
        }
    }
}

// Clip behaviour per trigger personalizzati
[System.Serializable]
public class TriggerClip : PlayableAsset, ITimelineClipAsset
{
    public TriggerBehaviour template = new TriggerBehaviour();
    
    public ClipCaps clipCaps => ClipCaps.None;
    
    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<TriggerBehaviour>.Create(graph, template);
        return playable;
    }
}

[System.Serializable]
public class TriggerBehaviour : PlayableBehaviour
{
    public int triggerOnStart = 1;
    public int triggerOnEnd = 2;
    public bool sendContinuous = false;
    public float continuousInterval = 1.0f;
    
    private TriggerManager triggerManager;
    private bool hasTriggeredStart = false;
    private float lastContinuousTrigger = 0;
    
    public override void OnPlayableCreate(Playable playable)
    {
        triggerManager = TriggerManager.Instance;
    }
    
    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (!hasTriggeredStart && triggerManager?.IsConnected == true)
        {
            triggerManager.SendMarker(markerNS: triggerOnStart);
            hasTriggeredStart = true;
            Debug.Log($"Timeline Clip Start: {triggerOnStart}");
        }
    }
    
    public override void PrepareFrame(Playable playable, FrameData info)
    {
        if (sendContinuous && triggerManager?.IsConnected == true)
        {
            float currentTime = (float)playable.GetTime();
            
            if (currentTime - lastContinuousTrigger >= continuousInterval)
            {
                triggerManager.SendMarker(markerNS: triggerOnStart);
                lastContinuousTrigger = currentTime;
            }
        }
    }
    
    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        if (hasTriggeredStart && triggerManager?.IsConnected == true)
        {
            triggerManager.SendMarker(markerNS: triggerOnEnd);
            hasTriggeredStart = false;
            Debug.Log($"Timeline Clip End: {triggerOnEnd}");
        }
    }
}
```

---

## Test e Debug

### Trigger Test Suite

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Trgen;

public class TriggerTestSuite : MonoBehaviour
{
    [Header("Test Settings")]
    public bool runTestsOnStart = false;
    public float testDelay = 1.0f;
    
    private TriggerManager triggerManager;
    private List<TestResult> testResults = new List<TestResult>();
    
    public class TestResult
    {
        public string testName;
        public bool passed;
        public string message;
        public float executionTime;
    }
    
    void Start()
    {
        triggerManager = TriggerManager.Instance;
        
        if (runTestsOnStart && triggerManager != null)
        {
            triggerManager.OnConnected += () => StartCoroutine(RunAllTests());
        }
    }
    
    public void StartTests()
    {
        if (triggerManager?.IsConnected == true)
        {
            StartCoroutine(RunAllTests());
        }
        else
        {
            Debug.LogError("TriggerBox not connected - cannot run tests");
        }
    }
    
    IEnumerator RunAllTests()
    {
        Debug.Log("=== Starting TriggerBox Test Suite ===");
        testResults.Clear();
        
        // Test 1: Basic Connection
        yield return StartCoroutine(TestConnection());
        yield return new WaitForSeconds(testDelay);
        
        // Test 2: Single Triggers
        yield return StartCoroutine(TestSingleTriggers());
        yield return new WaitForSeconds(testDelay);
        
        // Test 3: Marker Tests
        yield return StartCoroutine(TestMarkers());
        yield return new WaitForSeconds(testDelay);
        
        // Test 4: Multiple Triggers
        yield return StartCoroutine(TestMultipleTriggers());
        yield return new WaitForSeconds(testDelay);
        
        // Test 5: Advanced Programming
        yield return StartCoroutine(TestAdvancedProgramming());
        yield return new WaitForSeconds(testDelay);
        
        // Test 6: Error Handling
        yield return StartCoroutine(TestErrorHandling());
        
        DisplayTestResults();
    }
    
    IEnumerator TestConnection()
    {
        var test = new TestResult { testName = "Connection Test" };
        float startTime = Time.time;
        
        try
        {
            bool connected = triggerManager.IsConnected;
            test.passed = connected;
            test.message = connected ? "Connection successful" : "Connection failed";
        }
        catch (System.Exception ex)
        {
            test.passed = false;
            test.message = $"Exception: {ex.Message}";
        }
        
        test.executionTime = Time.time - startTime;
        testResults.Add(test);
        
        yield return null;
    }
    
    IEnumerator TestSingleTriggers()
    {
        var test = new TestResult { testName = "Single Triggers" };
        float startTime = Time.time;
        
        try
        {
            // Test tutti i tipi di pin
            int[] testPins = { TrgenPin.NS0, TrgenPin.SA0, TrgenPin.GPIO0 };
            
            foreach (int pin in testPins)
            {
                triggerManager.SendTrigger(pin);
                yield return new WaitForSeconds(0.1f);
            }
            
            test.passed = true;
            test.message = $"Successfully tested {testPins.Length} pin types";
        }
        catch (System.Exception ex)
        {
            test.passed = false;
            test.message = $"Exception: {ex.Message}";
        }
        
        test.executionTime = Time.time - startTime;
        testResults.Add(test);
    }
    
    IEnumerator TestMarkers()
    {
        var test = new TestResult { testName = "Marker Tests" };
        float startTime = Time.time;
        
        try
        {
            // Test diversi valori di marker
            int[] testValues = { 1, 5, 15, 255 };
            
            foreach (int value in testValues)
            {
                triggerManager.SendMarker(markerNS: value);
                yield return new WaitForSeconds(0.1f);
                
                triggerManager.SendMarker(markerGPIO: value);
                yield return new WaitForSeconds(0.1f);
            }
            
            // Test LSB vs MSB
            triggerManager.SendMarker(markerNS: 5, LSB: true);
            yield return new WaitForSeconds(0.1f);
            triggerManager.SendMarker(markerNS: 5, LSB: false);
            
            test.passed = true;
            test.message = $"Successfully tested {testValues.Length * 2 + 2} marker combinations";
        }
        catch (System.Exception ex)
        {
            test.passed = false;
            test.message = $"Exception: {ex.Message}";
        }
        
        test.executionTime = Time.time - startTime;
        testResults.Add(test);
    }
    
    IEnumerator TestMultipleTriggers()
    {
        var test = new TestResult { testName = "Multiple Triggers" };
        float startTime = Time.time;
        
        try
        {
            // Test trigger simultanei
            var triggerList = new List<int> { TrgenPin.NS1, TrgenPin.NS3, TrgenPin.GPIO5 };
            triggerManager.TriggerManager.Instance.client.StartTriggerList(triggerList);
            
            yield return new WaitForSeconds(0.5f);
            
            // Test marker simultanei
            triggerManager.SendMarker(markerNS: 7, markerGPIO: 3);
            
            test.passed = true;
            test.message = "Multiple trigger test completed";
        }
        catch (System.Exception ex)
        {
            test.passed = false;
            test.message = $"Exception: {ex.Message}";
        }
        
        test.executionTime = Time.time - startTime;
        testResults.Add(test);
    }
    
    IEnumerator TestAdvancedProgramming()
    {
        var test = new TestResult { testName = "Advanced Programming" };
        float startTime = Time.time;
        
        try
        {
            // Test programmazione personalizzata
            var client = TriggerManager.Instance.client;
            var trigger = client.CreateTrgenPort(TrgenPin.NS7);
            
            // Sequenza: 30μs on, 10μs off, repeat 3 times
            trigger.SetInstruction(0, InstructionEncoder.ActiveForUs(30));
            trigger.SetInstruction(1, InstructionEncoder.UnactiveForUs(10));
            trigger.SetInstruction(2, InstructionEncoder.Repeat(0, 3));
            trigger.SetInstruction(3, InstructionEncoder.End());
            
            client.SendTrgenMemory(trigger);
            client.Start();
            
            yield return new WaitForSeconds(1.0f);
            client.Stop();
            
            test.passed = true;
            test.message = "Advanced programming test completed";
        }
        catch (System.Exception ex)
        {
            test.passed = false;
            test.message = $"Exception: {ex.Message}";
        }
        
        test.executionTime = Time.time - startTime;
        testResults.Add(test);
    }
    
    IEnumerator TestErrorHandling()
    {
        var test = new TestResult { testName = "Error Handling" };
        float startTime = Time.time;
        
        try
        {
            // Test emergency stop
            triggerManager.EmergencyStop();
            yield return new WaitForSeconds(0.1f);
            
            // Test invalid operations (dovrebbero essere gestite senza crash)
            triggerManager.SendTrigger(-1); // Invalid pin
            yield return new WaitForSeconds(0.1f);
            
            triggerManager.SendMarker(markerNS: 999); // Value out of range
            yield return new WaitForSeconds(0.1f);
            
            test.passed = true;
            test.message = "Error handling test completed - no crashes";
        }
        catch (System.Exception ex)
        {
            test.passed = false;
            test.message = $"Unexpected exception: {ex.Message}";
        }
        
        test.executionTime = Time.time - startTime;
        testResults.Add(test);
    }
    
    void DisplayTestResults()
    {
        Debug.Log("=== Test Results ===");
        
        int passed = 0;
        int total = testResults.Count;
        
        foreach (var result in testResults)
        {
            string status = result.passed ? "PASS" : "FAIL";
            Debug.Log($"[{status}] {result.testName}: {result.message} ({result.executionTime:F3}s)");
            
            if (result.passed) passed++;
        }
        
        Debug.Log($"=== Summary: {passed}/{total} tests passed ===");
        
        if (passed == total)
        {
            Debug.Log("🎉 All tests passed! TriggerBox is working correctly.");
        }
        else
        {
            Debug.LogWarning($"⚠️ {total - passed} tests failed. Check configuration and connections.");
        }
    }
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(Screen.width - 300, Screen.height - 150, 290, 140));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("TriggerBox Test Suite");
        
        if (GUILayout.Button("Run All Tests"))
        {
            StartTests();
        }
        
        if (testResults.Count > 0)
        {
            int passed = testResults.FindAll(r => r.passed).Count;
            GUILayout.Label($"Results: {passed}/{testResults.Count} passed");
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
```

---

## Applicazioni Avanzate

### Eye-Tracking Synchronization

```csharp
using UnityEngine;
using System.Collections;
using Trgen;

public class EyeTrackingSynchronizer : MonoBehaviour
{
    [Header("Synchronization")]
    public float syncInterval = 0.1f; // 10Hz sync rate
    public int eyeTrackingTrigger = 50;
    public int gazeFixationTrigger = 51;
    public int saccadeTrigger = 52;
    
    [Header("Gaze Detection")]
    public float fixationThreshold = 2.0f; // degrees
    public float saccadeThreshold = 30.0f; // degrees/second
    public float minFixationDuration = 0.15f; // seconds
    
    private TriggerManager triggerManager;
    private Vector2 lastGazePosition;
    private Vector2 currentGazePosition;
    private float lastGazeTime;
    private bool inFixation = false;
    private float fixationStartTime;
    
    // Simulazione eye-tracker (sostituire con SDK reale)
    public Transform gazeTarget;
    
    void Start()
    {
        triggerManager = TriggerManager.Instance;
        StartCoroutine(SynchronizationLoop());
    }
    
    IEnumerator SynchronizationLoop()
    {
        while (true)
        {
            if (triggerManager?.IsConnected == true)
            {
                // Ottieni posizione gaze corrente
                UpdateGazePosition();
                
                // Invia trigger di sincronizzazione
                triggerManager.SendMarker(markerNS: eyeTrackingTrigger);
                
                // Analizza movimento oculare
                AnalyzeEyeMovement();
            }
            
            yield return new WaitForSeconds(syncInterval);
        }
    }
    
    void UpdateGazePosition()
    {
        lastGazePosition = currentGazePosition;
        lastGazeTime = Time.time;
        
        // Esempio: converte posizione world di gazeTarget in coordinate schermo
        if (gazeTarget != null)
        {
            Vector3 screenPoint = Camera.main.WorldToScreenPoint(gazeTarget.position);
            currentGazePosition = new Vector2(screenPoint.x, screenPoint.y);
        }
        else
        {
            // Simulazione movimento casuale per test
            currentGazePosition += Random.insideUnitCircle * 50f * Time.deltaTime;
            currentGazePosition = new Vector2(
                Mathf.Clamp(currentGazePosition.x, 0, Screen.width),
                Mathf.Clamp(currentGazePosition.y, 0, Screen.height)
            );
        }
    }
    
    void AnalyzeEyeMovement()
    {
        float distance = Vector2.Distance(currentGazePosition, lastGazePosition);
        float velocity = distance / syncInterval;
        
        // Rilevamento saccade
        if (velocity > saccadeThreshold)
        {
            if (inFixation)
            {
                inFixation = false;
                Debug.Log("Saccade detected");
                triggerManager.SendMarker(markerNS: saccadeTrigger);
            }
        }
        // Rilevamento fissazione
        else if (distance < fixationThreshold)
        {
            if (!inFixation)
            {
                inFixation = true;
                fixationStartTime = Time.time;
            }
            else if (Time.time - fixationStartTime >= minFixationDuration)
            {
                // Fissazione stabile
                triggerManager.SendMarker(markerNS: gazeFixationTrigger);
                Debug.Log($"Fixation at {currentGazePosition}");
            }
        }
    }
    
    public void OnStimulusPresented(Vector2 stimulusPosition, int stimulusId)
    {
        // Marker per presentazione stimolo con posizione
        triggerManager.SendMarker(markerNS: stimulusId, markerGPIO: (int)stimulusPosition.x);
        
        Debug.Log($"Stimulus {stimulusId} presented at {stimulusPosition}");
    }
}
```

### Multi-Modal Synchronization

```csharp
using UnityEngine;
using System.Collections.Generic;
using Trgen;

public class MultiModalSync : MonoBehaviour
{
    [System.Serializable]
    public class ModalityConfig
    {
        public string name;
        public int syncTrigger;
        public float sampleRate;
        public bool enabled;
    }
    
    [Header("Modalities")]
    public ModalityConfig eeg = new ModalityConfig { name = "EEG", syncTrigger = 10, sampleRate = 1000f, enabled = true };
    public ModalityConfig eyeTracker = new ModalityConfig { name = "Eye-Tracker", syncTrigger = 20, sampleRate = 120f, enabled = true };
    public ModalityConfig fmri = new ModalityConfig { name = "fMRI", syncTrigger = 30, sampleRate = 0.5f, enabled = false };
    public ModalityConfig physiological = new ModalityConfig { name = "Physiological", syncTrigger = 40, sampleRate = 100f, enabled = true };
    
    [Header("Master Clock")]
    public float masterClockRate = 1000f; // 1kHz master clock
    public int masterClockTrigger = 1;
    
    [Header("Events")]
    public int eventStartTrigger = 100;
    public int eventEndTrigger = 200;
    
    private TriggerManager triggerManager;
    private Dictionary<string, float> lastSyncTimes = new Dictionary<string, float>();
    private int masterClockCounter = 0;
    
    void Start()
    {
        triggerManager = TriggerManager.Instance;
        
        // Inizializza timestamp
        var modalities = new[] { eeg, eyeTracker, fmri, physiological };
        foreach (var modality in modalities)
        {
            if (modality.enabled)
            {
                lastSyncTimes[modality.name] = 0f;
            }
        }
        
        // Avvia master clock
        InvokeRepeating(nameof(SendMasterClock), 0f, 1f / masterClockRate);
        
        // Avvia sincronizzazione modalità
        StartModalitySynchronization();
    }
    
    void SendMasterClock()
    {
        if (triggerManager?.IsConnected == true)
        {
            // Master clock trigger (alta frequenza)
            triggerManager.SendTrigger(TrgenPin.NS0);
            masterClockCounter++;
            
            // Ogni secondo invia anche marker timestamp
            if (masterClockCounter % (int)masterClockRate == 0)
            {
                int timestamp = Mathf.RoundToInt(Time.time);
                triggerManager.SendMarker(markerNS: masterClockTrigger, markerGPIO: timestamp);
            }
        }
    }
    
    void StartModalitySynchronization()
    {
        var modalities = new[] { eeg, eyeTracker, fmri, physiological };
        
        foreach (var modality in modalities)
        {
            if (modality.enabled)
            {
                StartCoroutine(ModalitySyncCoroutine(modality));
            }
        }
    }
    
    System.Collections.IEnumerator ModalitySyncCoroutine(ModalityConfig modality)
    {
        while (true)
        {
            if (triggerManager?.IsConnected == true)
            {
                triggerManager.SendMarker(markerNS: modality.syncTrigger);
                lastSyncTimes[modality.name] = Time.time;
                
                Debug.Log($"{modality.name} sync trigger sent");
            }
            
            yield return new WaitForSeconds(1f / modality.sampleRate);
        }
    }
    
    public void StartExperimentalBlock(int blockId)
    {
        if (triggerManager?.IsConnected == true)
        {
            // Timestamp preciso di inizio
            float preciseTiming = Time.unscaledTime;
            
            // Trigger di start con ID blocco
            triggerManager.SendMarker(markerNS: eventStartTrigger, markerGPIO: blockId);
            
            // Log per tutte le modalità
            foreach (var kvp in lastSyncTimes)
            {
                Debug.Log($"Block {blockId} started - {kvp.Key} last sync: {preciseTiming - kvp.Value:F4}s ago");
            }
        }
    }
    
    public void EndExperimentalBlock(int blockId)
    {
        if (triggerManager?.IsConnected == true)
        {
            triggerManager.SendMarker(markerNS: eventEndTrigger, markerGPIO: blockId);
            Debug.Log($"Block {blockId} ended");
        }
    }
    
    public void SendCustomEvent(string eventName, int eventCode)
    {
        if (triggerManager?.IsConnected == true)
        {
            triggerManager.SendMarker(markerNS: eventCode);
            Debug.Log($"Custom event '{eventName}' sent with code {eventCode}");
        }
    }
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 400, 300));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("Multi-Modal Synchronization");
        GUILayout.Label($"Master Clock: {masterClockCounter} pulses");
        
        GUILayout.Space(10);
        
        foreach (var kvp in lastSyncTimes)
        {
            float timeSinceSync = Time.time - kvp.Value;
            GUILayout.Label($"{kvp.Key}: {timeSinceSync:F3}s ago");
        }
        
        GUILayout.Space(10);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Start Block"))
        {
            StartExperimentalBlock(Random.Range(1, 100));
        }
        
        if (GUILayout.Button("End Block"))
        {
            EndExperimentalBlock(0);
        }
        GUILayout.EndHorizontal();
        
        if (GUILayout.Button("Custom Event"))
        {
            SendCustomEvent("TestEvent", Random.Range(50, 99));
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
```

---

Questi esempi forniscono una base solida per implementare applicazioni complesse con il TriggerBox. Ogni script può essere personalizzato secondo le specifiche esigenze sperimentali e può essere facilmente integrato in progetti Unity esistenti.