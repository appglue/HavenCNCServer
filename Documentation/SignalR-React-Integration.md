# HavenCNC SignalR Events Reference

This document lists all CNC events sent via SignalR and their properties.

## Message Structure

All SignalR messages follow this wrapper format:
```json
{
  "eventType": "DROEvent",           // Class name of the event
  "timestamp": "2025-10-17T...",     // UTC timestamp
  "data": { ... }                    // Event-specific properties listed below
}
```

## Event Types

### DROEvent (Position Updates)
**Description:** Machine axis position changes

**Properties:**
- `Timestamp` (DateTime) - When the position update occurred
- `MessageType` (string) - "DROEvent"
- `Message` (string) - Human-readable position summary
- `Axis1` (number) - X axis position
- `Axis2` (number) - Y axis position
- `Axis3` (number) - Z axis position
- `Axis4` (number) - A axis position
- `Axis5` (number) - B axis position
- `Axis6` (number) - C axis position
- `Axis7` (number) - Additional axis position
- `Axis8` (number) - Additional axis position

---

### MessageEvent (CNC Messages)
**Description:** Status messages, errors, warnings from CNC system

**Properties:**
- `Timestamp` (DateTime) - When the message occurred
- `MessageType` (string) - "MessageEvent"
- `Message` (string) - Message text content
- `EventCode` (number) - Numeric error/message code
- `EventType` (string) - Classified message type (e.g., "SystemFault", "StatusMessage")

---

### JobStartedEvent (Job Execution Started)
**Description:** G-code job has started execution

**Properties:**
- `Timestamp` (DateTime) - When the job started
- `MessageType` (string) - "JobStartedEvent"
- `Message` (string) - Job start message
- `JobId` (string) - Unique job identifier
- `GCodeLines` (array) - G-code lines for the job
- `TotalLines` (number) - Total number of lines in job
- `IsStepRunMode` (boolean) - Whether job is in step-run mode
- `FilePath` (string) - File path if job loaded from file

---

### JobCompletedEvent (Job Execution Finished)
**Description:** G-code job has completed execution

**Properties:**
- `Timestamp` (DateTime) - When the job completed
- `MessageType` (string) - "JobCompletedEvent"
- `Message` (string) - Job completion message
- `JobId` (string) - Unique job identifier
- `Success` (boolean) - Whether job completed successfully
- `ErrorMessage` (string) - Error message if job failed
- `Duration` (TimeSpan) - Job execution duration
- `LinesExecuted` (number) - Total lines executed

---

### JobInfoEvent (Job Status Information)
**Description:** Current job execution status and progress

**Properties:**
- `Timestamp` (DateTime) - When the info was captured
- `MessageType` (string) - "JobInfoEvent"
- `Message` (string) - Job info message
- `LineNumber` (number) - Current executing line number
- `JobName` (string) - Name of currently running job

---

### StepExecutionEvent (Step Run Progress)
**Description:** Current line being executed in step-run mode

**Properties:**
- `Timestamp` (DateTime) - When the step was executed
- `MessageType` (string) - "StepExecutionEvent"
- `Message` (string) - Step execution message
- `JobId` (string) - Unique job identifier
- `LineNumber` (number) - Current line number being executed (1-based)
- `GCodeLine` (string) - The G-code line being executed
- `Status` (string) - Execution status of the step

---

## Usage Example

```javascript
connection.on("ReceiveCNCMessage", (message) => {
    console.log(`Event Type: ${message.eventType}`);
    console.log(`Timestamp: ${message.timestamp}`);
    
    switch(message.eventType) {
        case "DROEvent":
            console.log(`Position - X: ${message.data.Axis1}, Y: ${message.data.Axis2}, Z: ${message.data.Axis3}`);
            break;
            
        case "MessageEvent":
            console.log(`CNC Message: ${message.data.Message} (Code: ${message.data.EventCode})`);
            break;
            
        case "JobStartedEvent":
            console.log(`Job Started: ${message.data.JobId} with ${message.data.TotalLines} lines`);
            break;
            
        // ... handle other event types
    }
});
```
