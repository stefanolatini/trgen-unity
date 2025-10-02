# 🔧 Configuration Management

Complete guide for saving, loading, and managing TrGEN trigger configurations.

## Overview

The TrGEN configuration system allows you to:
- **Save** complete trigger setups including memory state
- **Load** previous configurations for experiment replication
- **Share** configurations between researchers
- **Version** experimental paradigms

## Configuration Structure

```csharp
public class TrgenConfiguration
{
    public ConfigurationMetadata Metadata { get; set; }
    public DefaultSettings Defaults { get; set; }
    public Dictionary<string, TriggerPortConfig> TriggerPorts { get; set; }
    public NetworkSettings Network { get; set; }
}
```

## Basic Export/Import

### Export Current Configuration

```csharp
public class ConfigurationExample : MonoBehaviour
{
    [SerializeField] private TrgenClient client;
    
    public void SaveCurrentSetup()
    {
        string savedPath = client.ExportConfiguration(
            "Configurations/EEG_Experiment",
            projectName: "Visual P300 Study",
            description: "Oddball paradigm configuration",
            author: "Dr. Smith"
        );
        
        Debug.Log($"💾 Configuration saved: {savedPath}");
    }
}
```

### Import Configuration

```csharp
public void LoadPreviousSetup()
{
    try
    {
        var config = client.ImportConfiguration("Configurations/EEG_Experiment.trgen");
        Debug.Log($"✅ Loaded: {config.Metadata.ProjectName}");
        Debug.Log($"📅 Created: {config.Metadata.CreatedAt}");
        Debug.Log($"👤 Author: {config.Metadata.Author}");
    }
    catch (Exception ex)
    {
        Debug.LogError($"❌ Failed to load configuration: {ex.Message}");
    }
}
```

## Advanced Configuration

### Custom Port Setup

```csharp
public TrgenConfiguration CreateCustomConfiguration()
{
    var config = new TrgenConfiguration();
    
    // Set metadata
    config.Metadata = new ConfigurationMetadata
    {
        ProjectName = "Custom EEG Setup",
        Author = "Researcher Name",
        Description = "Specialized configuration for oddball paradigm",
        Version = "1.0"
    };
    
    // Configure defaults
    config.Defaults = new DefaultSettings
    {
        DefaultTriggerDurationUs = 15,
        DefaultLogLevel = "Info",
        AutoResetEnabled = true
    };
    
    // Setup specific ports
    config.TriggerPorts["NS0"] = new TriggerPortConfig
    {
        Id = 0,
        Name = "Target Stimulus",
        Type = "NS",
        Enabled = true,
        CustomDurationUs = 10,
        Notes = "Target stimuli for P300 component"
    };
    
    config.TriggerPorts["NS1"] = new TriggerPortConfig
    {
        Id = 1,
        Name = "Standard Stimulus",
        Type = "NS", 
        Enabled = true,
        CustomDurationUs = 10,
        Notes = "Standard stimuli (80% frequency)"
    };
    
    return config;
}
```

### Memory State Preservation

```csharp
public void ConfigurationWithMemory()
{
    // Program some triggers first
    var instructions = new uint[]
    {
        InstructionEncoder.ActiveForUs(50),
        InstructionEncoder.UnactiveForUs(10),
        InstructionEncoder.ActiveForUs(30),
        InstructionEncoder.End()
    };
    
    client.ProgramPortWithInstructions(TrgenPin.NS0, instructions);
    client.ProgramPortWithInstructions(TrgenPin.NS1, instructions);
    
    // Export configuration including memory state
    string path = client.ExportConfiguration(
        "Configurations/MemoryPreserved",
        projectName: "Complex Sequence Study",
        description: "Configuration with programmed trigger sequences"
    );
    
    Debug.Log($"💾 Configuration with memory saved: {path}");
}
```

## File Format

Configuration files use `.trgen` extension with JSON content:

```json
{
  "metadata": {
    "version": "1.0",
    "projectName": "EEG Experiment",
    "author": "Dr. Smith",
    "description": "P300 oddball paradigm setup",
    "createdAt": "2025-10-02T15:30:00"
  },
  "defaults": {
    "defaultTriggerDurationUs": 15,
    "defaultLogLevel": "Warn",
    "autoResetEnabled": true
  },
  "triggerPorts": {
    "NS0": {
      "id": 0,
      "name": "Target Stimulus",
      "type": "NS",
      "enabled": true,
      "customDurationUs": 10,
      "memoryInstructions": [67108873, 196611, 0, ...],
      "programmingState": "Programmed",
      "notes": "Target stimuli for P300 component"
    }
  },
  "network": {
    "ipAddress": "192.168.123.1",
    "port": 4242,
    "timeoutMs": 2000
  }
}
```

## Configuration Templates

### EEG Template

```csharp
public static TrgenConfiguration CreateEEGTemplate()
{
    var config = new TrgenConfiguration();
    config.Metadata.ProjectName = "EEG Standard Template";
    config.Defaults.DefaultTriggerDurationUs = 10; // Optimal for EEG
    
    // Standard EEG ports
    config.TriggerPorts["NS0"] = new TriggerPortConfig
    {
        Id = 0,
        Name = "Target Stimulus",
        Enabled = true,
        CustomDurationUs = 10
    };
    
    config.TriggerPorts["NS1"] = new TriggerPortConfig
    {
        Id = 1,
        Name = "Standard Stimulus",
        Enabled = true,
        CustomDurationUs = 10
    };
    
    return config;
}
```

### fMRI Template

```csharp
public static TrgenConfiguration CreatefMRITemplate()
{
    var config = new TrgenConfiguration();
    config.Metadata.ProjectName = "fMRI Standard Template";
    config.Defaults.DefaultTriggerDurationUs = 100; // Longer for fMRI
    
    // Scanner synchronization
    config.TriggerPorts["GPIO0"] = new TriggerPortConfig
    {
        Id = 18,
        Name = "Scanner Sync",
        Enabled = true,
        CustomDurationUs = 200
    };
    
    return config;
}
```

## Best Practices

### Naming Conventions

```csharp
// Use descriptive, hierarchical names
"Experiments/EEG/P300/Oddball_v1.2.trgen"
"Studies/fMRI/VisualCortex/Block_Design.trgen"
"TMS/MotorCortex/Single_Pulse.trgen"
```

### Version Control

```csharp
public void SaveVersionedConfiguration()
{
    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
    string versionedPath = $"Configurations/Experiment_v{timestamp}";
    
    client.ExportConfiguration(
        versionedPath,
        projectName: "Experiment Version Control",
        description: $"Auto-saved version from {DateTime.Now}"
    );
}
```

### Validation

```csharp
public bool ValidateConfiguration(TrgenConfiguration config)
{
    // Check required fields
    if (string.IsNullOrEmpty(config.Metadata.ProjectName))
    {
        Debug.LogError("❌ Project name is required");
        return false;
    }
    
    // Validate trigger durations
    foreach (var port in config.TriggerPorts.Values)
    {
        if (port.Enabled && port.CustomDurationUs < 5)
        {
            Debug.LogWarning($"⚠️ Port {port.Name}: Duration {port.CustomDurationUs}µs may be too short");
        }
    }
    
    return true;
}
```

## Troubleshooting

### Common Issues

**File Not Found**
```csharp
if (!File.Exists(configPath))
{
    Debug.LogError($"❌ Configuration file not found: {configPath}");
    Debug.LogError("💡 Check file path and ensure .trgen extension");
}
```

**Invalid JSON Format**
```csharp
try
{
    var config = TrgenConfigurationManager.LoadConfiguration(path);
}
catch (JsonException ex)
{
    Debug.LogError($"❌ Invalid configuration format: {ex.Message}");
    Debug.LogError("💡 Check JSON syntax and structure");
}
```

**Memory Conflicts**
```csharp
// Check for memory programming conflicts
var snapshot = client.CreateMemorySnapshot();
foreach (var port in snapshot)
{
    if (port.Value.Any(instruction => instruction != 0))
    {
        Debug.LogWarning($"⚠️ Port {port.Key} has existing memory - will be overwritten");
    }
}
```