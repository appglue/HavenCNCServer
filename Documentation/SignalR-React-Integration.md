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
- `timestamp` (string) - When the position update occurred (ISO 8601)
- `messageType` (string) - "DROEvent"
- `message` (string) - Human-readable position summary
- `axis1` (number) - X axis position
- `axis2` (number) - Y axis position
- `axis3` (number) - Z axis position
- `axis4` (number) - A axis position
- `axis5` (number) - B axis position
- `axis6` (number) - C axis position
- `axis7` (number) - Additional axis position
- `axis8` (number) - Additional axis position

---

### MessageEvent (CNC Messages)
**Description:** Status messages, errors, warnings from CNC system

**Properties:**
- `timestamp` (string) - When the message occurred (ISO 8601)
- `messageType` (string) - "MessageEvent"
- `message` (string) - Message text content
- `eventCode` (number) - Numeric error/message code
- `eventType` (string) - Classified message type (e.g., "SystemFault", "StatusMessage")

---

### JobStartedEvent (Job Execution Started)
**Description:** G-code job has started execution

**Properties:**
- `timestamp` (string) - When the job started (ISO 8601)
- `messageType` (string) - "JobStartedEvent"
- `message` (string) - Job start message
- `jobId` (string) - Unique job identifier
- `gCodeLines` (array) - G-code lines for the job
- `totalLines` (number) - Total number of lines in job
- `isStepRunMode` (boolean) - Whether job is in step-run mode
- `filePath` (string) - File path if job loaded from file

---

### JobCompletedEvent (Job Execution Finished)
**Description:** G-code job has completed execution

**Properties:**
- `timestamp` (string) - When the job completed (ISO 8601)
- `messageType` (string) - "JobCompletedEvent"
- `message` (string) - Job completion message
- `jobId` (string) - Unique job identifier
- `success` (boolean) - Whether job completed successfully
- `errorMessage` (string) - Error message if job failed
- `duration` (string) - Job execution duration (TimeSpan format)
- `linesExecuted` (number) - Total lines executed

---

### JobInfoEvent (Job Status Information)
**Description:** Current job execution status and progress

**Properties:**
- `timestamp` (string) - When the info was captured (ISO 8601)
- `messageType` (string) - "JobInfoEvent"
- `message` (string) - Job info message
- `lineNumber` (number) - Current executing line number
- `stackLevel` (number) - Stack level for nested programs/subroutines
- `jobName` (string) - Name of currently running job

---

### StepExecutionEvent (Step Run Progress)
**Description:** Current line being executed in step-run mode

**Properties:**
- `timestamp` (string) - When the step was executed (ISO 8601)
- `messageType` (string) - "StepExecutionEvent"
- `message` (string) - Step execution message
- `jobId` (string) - Unique job identifier
- `lineNumber` (number) - Current line number being executed (1-based)
- `currentLine` (string) - The G-code line being executed
- `totalLines` (number) - Total number of lines in the job
- `isLastStep` (boolean) - Whether this is the last step in the job
- `status` (string) - Execution status ("AboutToExecute", "Executing", "Completed", "Failed", "Skipped")

---

## Usage Example

```javascript
connection.on("ReceiveCNCMessage", (message) => {
    console.log(`Event Type: ${message.eventType}`);
    console.log(`Timestamp: ${message.timestamp}`);
    
    switch(message.eventType) {
        case "DROEvent":
            console.log(`Position - X: ${message.data.axis1}, Y: ${message.data.axis2}, Z: ${message.data.axis3}`);
            break;
            
        case "MessageEvent":
            console.log(`CNC Message: ${message.data.message} (Code: ${message.data.eventCode})`);
            break;
            
        case "JobStartedEvent":
            console.log(`Job Started: ${message.data.jobId} with ${message.data.totalLines} lines`);
            break;
            
        case "JobInfoEvent":
            console.log(`Job Info: Line ${message.data.lineNumber} (Level ${message.data.stackLevel}) - ${message.data.message}`);
            break;
            
        // ... handle other event types
    }
});
```
