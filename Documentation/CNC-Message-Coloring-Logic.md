# CNC Message Coloring Logic

This document describes the color-coding system used for Centroid CNC messages in the HavenCNCServer application.

## Overview

CNC messages are color-coded based on their **event code** (error number) and **message type** to provide quick visual identification of message severity and category. The color scheme follows Centroid's error code ranges and message classifications.

## Color Scheme

### Primary Colors by Severity

| Severity | Color | RGB | Hex | Usage |
|----------|-------|-----|-----|-------|
| **Error** | Red | 255, 0, 0 | #FF0000 | System faults, errors, and critical issues |
| **Warning** | Orange | 255, 165, 0 | #FFA500 | Syntax errors, parameter errors, minor issues |
| **Success** | Green | 0, 128, 0 | #008000 | Job completion, successful operations |
| **Info** | Blue | 0, 0, 255 | #0000FF | Status messages, job info, general information |
| **DRO** | Purple | 128, 0, 128 | #800080 | Position updates (DRO events) |
| **Step** | Dark Green | 0, 100, 0 | #006400 | Step execution progress |
| **Normal** | Black | 0, 0, 0 | #000000 | Unclassified or default messages |

## Message Classification by Error Code Ranges

### Configuration Messages (Special Codes)
**Color: Info (Blue)**

Specific error codes that indicate configuration changes:
- `111` - Configuration message
- `444` - Configuration message
- `555` - Configuration message
- `556` - Configuration message
- `777` - Configuration message
- `888` - Configuration message
- `999` - Configuration message

### Startup Messages (100-199)
**Color: Error (Red) for errors, Info (Blue) for status**

| Code Range | Description | Classification |
|------------|-------------|----------------|
| 102-106 | Startup errors | StartupError (Red) |
| 199 | "CNC started" | StatusMessage (Blue) |

### Exit Messages (200-299)
**Color: Info (Blue)**

| Code Range | Description | Classification |
|------------|-------------|----------------|
| 201-204 | Exit messages | ExitMessage (Blue) |
| 222 | Status message | StatusMessage (Blue) |

### Status Messages (300-399)
**Color: Varies by specific code**

| Code | Description | Classification | Color |
|------|-------------|----------------|-------|
| 301 | "Stopped" | StatusMessage | Blue |
| 302 | "Moving..." | StatusMessage | Blue |
| 303 | "Paused..." | StatusMessage | Blue |
| 304 | "MDI..." | StatusMessage | Blue |
| 305 | "Processing..." | StatusMessage | Blue |
| 306 | "Job Finished" | JobCompleted | **Green** |
| 307 | "Operator abort: job canceled" | JobCancelled | Blue |
| 318-337 | Probing errors | ProbeError | **Red** |
| 320-330 | Cancellation reasons | SystemFault | **Red** |
| 338 | "Job Cancelled" | JobCancelled | Blue |
| 347 | "Reset Cleared" | StatusMessage | Blue |

### Fault Messages (400-499)
**Color: Error (Red)**

| Code | Description | Classification |
|------|-------------|----------------|
| 401 | "PLC failure detected" | SystemFault |
| 404 | "Spindle drive fault detected" | SystemFault |
| 405 | "Lubricant level low" | SystemFault |
| 406 | "Emergency Stop detected" | SystemFault |
| 407 | "limit (#) tripped" | LimitError |
| 409-447 | Various axis faults | AxisFault |
| 449-460 | Various system faults | SystemFault |
| 452-453 | Communication errors | CommunicationError |
| 490 | "Reset Initiated. Press Reset to Clear" | SystemFault |

**All messages in 400-499 range display in RED**

### Syntax Errors (500-599)
**Color: Warning (Orange)**

| Code Range | Description | Classification |
|------------|-------------|----------------|
| 501-552 | G-code syntax errors | SyntaxError |

Examples:
- Invalid G-code format
- Undefined commands
- Syntax violations

### Cutter Compensation Errors (600-699)
**Color: Warning (Orange)**

| Code Range | Description | Classification |
|------------|-------------|----------------|
| 601-608 | Cutter compensation calculation errors | CutterCompensationError |

### Parameter Setting Errors (700-799)
**Color: Warning (Orange)**

| Code Range | Description | Classification |
|------------|-------------|----------------|
| 701-705 | Parameter configuration errors | ParameterSettingError |

### Canned Cycle Errors (800-899)
**Color: Warning (Orange)**

| Code Range | Description | Classification |
|------------|-------------|----------------|
| 801-807 | Drill, tap, and canned cycle errors | CannedCycleError |

### Miscellaneous Errors (900-999)
**Color: Error (Red)**

| Code Range | Description | Classification |
|------------|-------------|----------------|
| 901-949 | Miscellaneous system errors | MiscellaneousError |

### Scaling/Mirroring Errors (1000-1199)
**Color: Warning (Orange)**

| Code Range | Description | Classification |
|------------|-------------|----------------|
| 1001-1199 | Scaling and mirroring operation errors | ScalingError |

## Special Message Types

### Job Information Messages
**Color: Blue**

Format: `[HH:mm:ss.fff] JOB: Line {LineNumber} (Level {StackLevel}) - {Message}`

Examples:
```
[11:11:12.953] JOB: Line 10 (Level 0) - C:\cncr\ncfiles\sample_atlanta.cnc
[11:19:47.330] JOB: Line 6 (Level 0) - C:\cncr\ncfiles\sample_atlanta.cnc
```

### DRO (Digital ReadOut) Messages
**Color: Purple**

Format: `[HH:mm:ss.fff] DRO: X:{X} Y:{Y} Z:{Z}`

Examples:
```
[11:11:12.531] DRO: X:-25.7681 Y:-29.0930 Z:0.0000
[11:20:16.786] DRO: X:-29.3483 Y:-35.0000 Z:0.0000
```

### Step Execution Messages
**Color: Dark Green**

Format: `[HH:mm:ss.fff] STEP ({Current}/{Total}) [{Status}]: {GCodeLine}`

Examples:
```
[11:20:15.678] STEP (3/3) [Executing]: G0 X10.0000 Y0.0000 Z0.5080
[11:20:16.232] STEP (3/3) [Executing]: G0 X10.0000 Y0.0000 Z0.5080
```

## Message Format

Standard message format includes:
```
[HH:mm:ss.fff] [ErrorCode] (EventType) Message
```

Components:
- **Timestamp**: High-resolution time (hours:minutes:seconds.milliseconds)
- **ErrorCode**: 3-4 digit numeric code in brackets (e.g., `[490]`, `[307]`)
- **EventType**: Classification in parentheses (e.g., `(SystemFault)`, `(StatusMessage)`)
- **Message**: The actual message text

## Content-Based Fallback Classification

When no error code is present, messages are classified by content keywords:

| Keywords | Classification | Color |
|----------|----------------|-------|
| "job" + ("start"\|"begin") | JobStarted | Blue |
| "job" + ("finish"\|"complete"\|"done") | JobCompleted | Green |
| "job" + ("cancel"\|"abort") | JobCancelled | Blue |
| "limit" | LimitError | Red |
| "probe" | ProbeError | Red |
| "axis" + "fault" | AxisFault | Red |
| "syntax" \| "invalid" | SyntaxError | Orange |
| "parameter" + "error" | ParameterError | Orange |
| "compensation" | CutterCompensationError | Orange |
| "error" \| "fault" | SystemFault | Red |
| "config" \| "modified" | ConfigurationChange | Blue |

## Implementation Reference

### TypeScript/JavaScript Example

```typescript
enum MessageSeverity {
  Normal = 'normal',
  Info = 'info',
  Success = 'success',
  Warning = 'warning',
  Error = 'error',
  DRO = 'dro',
  Step = 'step'
}

const messageColors = {
  error: '#FF0000',    // Red
  warning: '#FFA500',  // Orange
  success: '#008000',  // Green
  info: '#0000FF',     // Blue
  dro: '#800080',      // Purple
  step: '#006400',     // DarkGreen
  normal: '#000000'    // Black
};

function getMessageColor(eventCode: number, eventType: string): string {
  // Configuration messages
  if ([111, 444, 555, 556, 777, 888, 999].includes(eventCode)) {
    return messageColors.info;
  }
  
  // Status and Info (300-399, excluding specific error codes)
  if (eventCode >= 300 && eventCode <= 399) {
    if (eventCode === 306) return messageColors.success; // Job Finished
    if (eventCode >= 318 && eventCode <= 337) return messageColors.error; // Probe errors
    if (eventCode >= 320 && eventCode <= 330) return messageColors.error; // Cancellations
    return messageColors.info;
  }
  
  // Faults (400-499) - All RED
  if (eventCode >= 400 && eventCode <= 499) {
    return messageColors.error;
  }
  
  // Syntax errors (500-599) - Orange
  if (eventCode >= 500 && eventCode <= 599) {
    return messageColors.warning;
  }
  
  // Cutter compensation errors (600-699) - Orange
  if (eventCode >= 600 && eventCode <= 699) {
    return messageColors.warning;
  }
  
  // Parameter errors (700-799) - Orange
  if (eventCode >= 700 && eventCode <= 799) {
    return messageColors.warning;
  }
  
  // Canned cycle errors (800-899) - Orange
  if (eventCode >= 800 && eventCode <= 899) {
    return messageColors.warning;
  }
  
  // Miscellaneous errors (900-999) - Red
  if (eventCode >= 900 && eventCode <= 999) {
    return messageColors.error;
  }
  
  // Scaling errors (1000-1199) - Orange
  if (eventCode >= 1000 && eventCode <= 1199) {
    return messageColors.warning;
  }
  
  // Default based on event type
  if (eventType.includes('Error') || eventType.includes('Fault')) {
    return messageColors.error;
  }
  if (eventType.includes('Completed')) {
    return messageColors.success;
  }
  if (eventType.includes('Job') || eventType.includes('Status')) {
    return messageColors.info;
  }
  
  return messageColors.normal;
}
```

### CSS Classes

```css
.message-error { color: #FF0000; font-weight: 500; }
.message-warning { color: #FFA500; }
.message-success { color: #008000; font-weight: 500; }
.message-info { color: #0000FF; }
.message-dro { color: #800080; opacity: 0.8; }
.message-step { color: #006400; }
.message-normal { color: #000000; }
```

## Quick Reference Table

| Event Code Range | Color | Use Case |
|------------------|-------|----------|
| 111, 444, 555, 777, 888, 999 | Blue | Configuration |
| 100-199 | Red/Blue | Startup |
| 200-299 | Blue | Exit messages |
| 300-399 | Blue/Green/Red | Status (mostly blue, 306=green, some red) |
| 400-499 | **Red** | **All faults and errors** |
| 500-599 | Orange | Syntax errors |
| 600-699 | Orange | Cutter compensation |
| 700-799 | Orange | Parameter errors |
| 800-899 | Orange | Canned cycles |
| 900-999 | Red | Miscellaneous errors |
| 1000-1199 | Orange | Scaling/mirroring |

## Notes

1. **DRO messages** are high-frequency position updates and may need throttling or filtering in the UI
2. **Step execution messages** appear during step-by-step program execution
3. Error code ranges are based on **Centroid CNC12 documentation**
4. When an error code is not present, **content-based classification** is used as fallback
5. The color scheme prioritizes **visual distinction** between critical errors (red) and warnings (orange)
