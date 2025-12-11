# Monitored I/O Configuration

## Overview

The system now stores which inputs and outputs to monitor in a configuration file (`data/monitored_io.json`) instead of parsing the PLC .src file each time. This provides better performance and allows the frontend to control which I/Os are displayed.

## How It Works

### 1. During PLC Installation

When compiling and installing a PLC via `/api/PLC/CompileAndInstallPLC`, you can specify which inputs and outputs to monitor:

```json
{
  "plcLines": ["...", "..."],
  "messageLines": ["...", "..."],
  "estopInputNumber": 1,
  "invertEstopInput": false,
  "inputsToMonitor": [
    { "number": 1, "name": "EStopOk" },
    { "number": 2, "name": "ProbeInput" },
    { "number": 3, "name": "LimitX" }
  ],
  "outputsToMonitor": [
    { "number": 1, "name": "SpindleEnable" },
    { "number": 2, "name": "SpindleFWD" },
    { "number": 3, "name": "Coolant" }
  ]
}
```

### 2. Storage

The monitored I/O configuration is saved to `data/monitored_io.json`:

```json
{
  "Inputs": [
    { "Number": 1, "Name": "EStopOk" },
    { "Number": 2, "Name": "ProbeInput" },
    { "Number": 3, "Name": "LimitX" }
  ],
  "Outputs": [
    { "Number": 1, "Name": "SpindleEnable" },
    { "Number": 2, "Name": "SpindleFWD" },
    { "Number": 3, "Name": "Coolant" }
  ],
  "LastUpdated": "2025-12-11T10:30:00Z"
}
```

### 3. Retrieving Monitored I/O States

The following endpoints now use the stored configuration instead of parsing .src files:

- `GET /api/CNCIO/GetDefinedInputs` - Returns monitored inputs with current states
- `GET /api/CNCIO/GetDefinedOutputs` - Returns monitored outputs with current states

Response example:
```json
[
  { "number": 1, "name": "EStopOk", "state": true },
  { "number": 2, "name": "ProbeInput", "state": false },
  { "number": 3, "name": "LimitX", "state": false }
]
```

## Benefits

1. **Performance**: No need to parse large .src files on every request
2. **Flexibility**: Frontend controls which I/Os are monitored
3. **Persistence**: Configuration survives server restarts
4. **Accuracy**: No parsing errors from .src file format changes

## Migration Notes

- Old behavior: Parsed `C:\cncr\acorn_router_plc.src` for I/O definitions
- New behavior: Uses stored configuration in `data/monitored_io.json`
- If no configuration file exists, endpoints return empty arrays
- Configuration is updated each time PLC is installed with `inputsToMonitor` and `outputsToMonitor`

## API Changes

### CompileAndInstallPLCRequest

Added properties:
- `InputsToMonitor` (MonitoredIODefinition[]) - List of inputs to monitor
- `OutputsToMonitor` (MonitoredIODefinition[]) - List of outputs to monitor

### PLCInstallationResult

Added property:
- `MonitoredIOSaved` (bool) - Whether the monitored I/O configuration was saved successfully

### MonitoredIODefinition

New class:
```csharp
public class MonitoredIODefinition
{
    public int Number { get; set; }
    public string Name { get; set; }
}
```
