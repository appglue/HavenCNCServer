# SignalR and API Changes - November 10, 2025

## Overview
Recent updates have improved message ordering, delivery performance, and job tracking capabilities. This document outlines the changes that affect frontend integration.

---

## 1. SignalR Message Delivery Architecture

### Previous Implementation
- Messages were sent using `Task.Run()` for each event
- Could arrive out of order due to parallel execution
- No guaranteed ordering mechanism

### New Implementation
- **Dedicated message queue** with bounded capacity (1000 messages)
- **Single processing thread** ensures FIFO (first-in, first-out) delivery
- **Higher priority thread** (`AboveNormal`) for real-time responsiveness
- **Non-blocking ingestion** - events are queued immediately without blocking

### Benefits
- ✅ **Guaranteed message ordering** - messages arrive in the exact order they were generated
- ✅ **Faster delivery** - reduced overhead from task scheduling
- ✅ **Better performance** - dedicated thread with higher priority
- ✅ **Overflow protection** - bounded queue prevents memory issues

---

## 2. SignalR Group Simplification

### Previous Implementation
- Messages sent to two groups:
  - `"CNCClients"` - all connected clients
  - `"MessageType_{EventType}"` - per-message-type groups (e.g., "MessageType_DROEvent")

### New Implementation
- **Single group only**: `"CNCClients"`
- All messages sent to all connected clients
- Client-side filtering handles event routing

### Migration Required
**Remove per-message-type subscriptions:**
```typescript
// ❌ Old - No longer needed
await connection.invoke("SubscribeToMessageType", "DROEvent");
await connection.invoke("SubscribeToMessageType", "HeartbeatEvent");

// ✅ New - Just join CNCClients group
await connection.invoke("JoinGroup", "CNCClients");
```

### Benefits
- Simpler server architecture
- Reduced network overhead (no duplicate sends)
- Client-side filtering is more flexible

---

## 3. Message Structure and Timestamps

### All SignalR Messages Include:
```typescript
interface SignalRMessage {
  EventType: string;        // e.g., "DROEvent", "JobCompletedEvent"
  Timestamp: string;        // UTC ISO 8601 when sent to SignalR
  Data: any;                // Event-specific data
}
```

### Individual Events Also Have Timestamps:
Each event in the `Data` field has its own `Timestamp` property marking when the event was created.

**Example:**
```typescript
{
  EventType: "DROEvent",
  Timestamp: "2025-11-10T18:30:42.500Z",  // SignalR send time
  Data: {
    Timestamp: "2025-11-10T18:30:42.485Z",  // Event creation time
    Axis1: 5.0000,
    Axis2: -151.0000,
    // ... other DRO data
  }
}
```

**Use Cases:**
- Use `EventType` for message routing/filtering
- Use message `Timestamp` to detect delivery delays
- Use `Data.Timestamp` for precise event timing
- Messages arrive in order by creation time

---

## 4. Server Status Messages (Heartbeat)

### Configuration
- **Frequency**: Every 2 seconds (hardcoded for reliability)
- **Event Type**: `"ServerStatus"` 
- **Delivery**: Via `ReceiveCNCMessage` (same as all other events)
- **Purpose**: Periodic status updates with connection state and current position

### Message Structure
The server status is sent through the standard message wrapper:
```typescript
{
  EventType: "ServerStatus",
  Timestamp: "2025-11-10T18:30:42.500Z",  // When sent
  Data: {
    Timestamp: "2025-11-10T18:30:42.500Z",  // UTC
    ServerTime: "2025-11-10T12:50:42.123",  // Local
    IsConnected: boolean,                    // CNC connection status
    Status: "Connected" | "Disconnected",
    MessageType: "ServerStatus",
    IsApiRestricted: boolean,                // Whether API is in restricted mode
    Position: {                              // null if disconnected
      X: number,
      Y: number,
      Z: number,
      A: number
    } | null
  }
}
```

### Example Usage
```typescript
// Listen for all messages including server status
connection.on("ReceiveCNCMessage", (message) => {
  if (message.EventType === "ServerStatus") {
    const status = message.Data;
    console.log(`Server alive - CNC Status: ${status.Status}`);
    updateLastHeartbeat(message.Timestamp);
    
    // Update position if available
    if (status.Position) {
      updatePositionDisplay(status.Position);
    }
  }
  
  if (message.EventType === "DROEvent") {
    // Real-time position updates during movement
    updatePositionDisplay(message.Data);
  }
});
```

**Purpose:**
- **Connection keepalive** (every 2 seconds) - detect disconnections quickly
- **Connection status** - Connected/disconnected state
- **Position snapshot** - Current position every 2 seconds (null when disconnected)
- **Baseline updates** - Ensures position is updated even when not moving

**Why This Design:**
- ✅ **Consistent** - Uses same message channel as all other events
- ✅ **Complete** - Includes both status and position data
- ✅ **Reliable** - Regular 2-second updates ensure fresh data
- ✅ **Complementary** - Works with DRO events for real-time movement updates

---

## 5. Job Tracking with JobId and FilePath

### Problem
Previously, there was no reliable way to match a job creation with its completion event.

### Solution
Both the API response and all job-related events include:
- **`JobId`** - Primary tracking key (always present, unique per job)
- **`FilePath`** - Supplementary information (file location, useful for display)

---

## 5.1 API Changes - RunGCode Response

### Endpoint
`POST /api/CNCProgram/RunGCode`

### Response Model (Updated)
```typescript
interface RunGCodeResponse {
  Success: boolean;
  JobId: string;           // e.g., "abc123-def456"
  Message: string;
  Job: JobDetails;
  FilePath?: string;       // ⭐ NEW: Full path to created file
  Error?: string;
}
```

### Example Response
```json
{
  "Success": true,
  "JobId": "abc123-def456-789ghi",
  "Message": "Job created and started successfully",
  "FilePath": "C:\\CNC12\\cncm\\job_abc123-def456-789ghi_20251110_143022.cnc",
  "Job": {
    "JobId": "abc123-def456-789ghi",
    "LineNumber": 0,
    "IsRunning": true,
    // ... other job details
  }
}
```

---

## 5.2 SignalR Job Events

### JobStartedEvent
```typescript
interface JobStartedEvent {
  Timestamp: string;         // ISO 8601 timestamp
  Message: string;           
  MessageType: "JobStartedEvent";
  JobId: string;             // ⭐ Primary tracking key
  FilePath?: string;         // Optional: file location
  GCodeLines: string[];      // The G-code being executed
  TotalLines: number;        // Total number of lines
  IsStepRunMode: boolean;    // Whether running in step mode
}
```

### StepExecutionEvent
```typescript
interface StepExecutionEvent {
  Timestamp: string;
  MessageType: "StepExecutionEvent";
  JobId: string;             // ⭐ Links to job
  LineNumber: number;        // Current line being executed
  CurrentLine: string;       // The G-code line text
  TotalLines: number;
  IsLastStep: boolean;
  Status: string;            // "Executing" | "Completed" | "Error"
}
```

### JobCompletedEvent
### JobCompletedEvent
```typescript
interface JobCompletedEvent {
  Timestamp: string;         // ISO 8601 timestamp
  Message: string;           // e.g., "Job abc123 completed"
  MessageType: "JobCompletedEvent";
  JobId: string;             // ⭐ Primary tracking key
  Success: boolean;          // true if completed successfully
  ErrorMessage?: string;     // Error details if failed
  Duration: string;          // Timespan: "00:02:15.123"
  LinesExecuted: number;     // Total lines executed
  FilePath?: string;         // Optional: file location
}
```

### Example Event
```json
{
  "EventType": "JobCompletedEvent",
  "Timestamp": "2025-11-10T18:32:45.789Z",
  "Data": {
    "Timestamp": "2025-11-10T18:32:45.785Z",
    "Message": "Job abc123-def456-789ghi completed",
    "MessageType": "JobCompletedEvent",
    "JobId": "abc123-def456-789ghi",
    "Success": true,
    "ErrorMessage": null,
    "Duration": "00:02:23.567",
    "LinesExecuted": 145,
    "FilePath": "C:\\CNC12\\cncm\\job_abc123-def456-789ghi_20251110_143022.cnc"
  }
}
```

---

## 5.3 Job Tracking Pattern

### Complete Implementation Example

```typescript
class JobTracker {
  private pendingJobs = new Map<string, JobInfo>();
  
  async startJob(gcode: string[]) {
    // 1. Start the job
    const response = await api.post('/api/CNCProgram/RunGCode', {
      GCodeLines: gcode,
      StartImmediately: true
    });
    
    if (response.Success) {
      // 2. Store the JobId as tracking key
      this.pendingJobs.set(response.JobId, {
        jobId: response.JobId,
        filePath: response.FilePath,  // Optional: for display
        startedAt: new Date(),
        onComplete: null
      });
      
      console.log(`Job started: ${response.JobId}`);
      return response.JobId; // Return the tracking key
    }
  }
  
  setupSignalR(connection) {
    // 3. Listen for job events
    connection.on("ReceiveCNCMessage", (message) => {
      if (message.EventType === "JobStartedEvent") {
        const event = message.Data;
        console.log(`Job ${event.JobId} started, ${event.TotalLines} lines`);
      }
      
      if (message.EventType === "StepExecutionEvent") {
        const event = message.Data;
        // Update progress for this job
        this.updateJobProgress(event.JobId, event.LineNumber, event.TotalLines);
      }
      
      if (message.EventType === "JobCompletedEvent") {
        const event = message.Data;
        
        // 4. Match using JobId
        if (this.pendingJobs.has(event.JobId)) {
          const jobInfo = this.pendingJobs.get(event.JobId);
          
          console.log(`Job ${event.JobId} completed!`);
          console.log(`Success: ${event.Success}`);
          console.log(`Duration: ${event.Duration}`);
          console.log(`Lines: ${event.LinesExecuted}`);
          if (event.FilePath) {
            console.log(`File: ${event.FilePath}`);
          }
          
          // 5. Handle completion
          this.handleJobComplete(event);
          this.pendingJobs.delete(event.JobId);
        }
      }
    });
  }
  
  updateJobProgress(jobId: string, current: number, total: number) {
    if (this.pendingJobs.has(jobId)) {
      const progress = (current / total) * 100;
      console.log(`Job ${jobId}: ${progress.toFixed(1)}% (${current}/${total})`);
      // Update UI progress bar, etc.
    }
  }
  
  handleJobComplete(event: JobCompletedEvent) {
    if (event.Success) {
      showNotification(`Job completed successfully in ${event.Duration}!`);
    } else {
      showError(`Job failed: ${event.ErrorMessage}`);
    }
  }
}
```

### Why JobId Is the Primary Key

**JobId is the recommended tracking key:**
- ✅ **Always present** - In all job-related events (Started, StepExecution, Completed)
- ✅ **Unique per job** - GUID format ensures uniqueness
- ✅ **Consistent** - Same ID throughout job lifecycle
- ✅ **Simple** - Direct string comparison for matching

**FilePath is supplementary:**
- Optional field (can be null in some contexts)
- Useful for display and debugging
- Provides file system location if needed
- Good for logging but not for tracking

---

## 6. DRO Event Throttling

### Current Status
**Throttling is DISABLED** by default to verify frontend behavior without rate limiting.

### Configuration
```csharp
// In DROEvent.cs
private const bool EnableThrottling = false; // Set to true to enable
```

### When Enabled (Future)
- Maximum 1 DRO event sent every 100ms
- Latest position always sent (older positions discarded)
- Reduces frontend load during rapid motion

### To Re-enable
1. Set `EnableThrottling = true` in `DROEvent.cs`
2. Rebuild application
3. Test frontend can handle 10 events/second

---

## 7. Migration Checklist

### Required Changes
- [ ] Remove `SubscribeToMessageType` calls
- [ ] Update to only join `"CNCClients"` group
- [ ] Store `JobId` from `RunGCode` API response
- [ ] Match job events using `JobId` (Started, StepExecution, Completed)
- [ ] Update heartbeat handler for 2-second interval
- [ ] Handle `Position: null` in heartbeat when disconnected

### Recommended Changes
- [ ] Use message `Timestamp` to detect stale/delayed messages
- [ ] Implement client-side event filtering by `EventType`
- [ ] Add connection status indicator using heartbeat
- [ ] Display job completion notifications
- [ ] Log job duration and lines executed on completion
- [ ] Track job progress using `StepExecutionEvent`
- [ ] Use `FilePath` for display purposes (file location)

### Testing
- [ ] Verify message ordering during rapid events
- [ ] Confirm job completion events match started jobs by `JobId`
- [ ] Test heartbeat connection monitoring
- [ ] Validate position updates during movement
- [ ] Check error handling for failed jobs
- [ ] Verify `StepExecutionEvent` progress tracking

---

## 8. Event Types Reference

### Core Events
| Event Type | Frequency | Purpose | Delivery Method |
|------------|-----------|---------|-----------------|
| `Heartbeat` | Every 2s | Keepalive ping, connection status | Direct SignalR method `connection.on("Heartbeat")` |
| `DROEvent` | ~10Hz (100ms)* | Real-time position updates | `ReceiveCNCMessage` with `EventType = "DROEvent"` |
| `JobStartedEvent` | On start | Job start notification with JobId | `ReceiveCNCMessage` |
| `StepExecutionEvent` | Per line | Line-by-line execution progress | `ReceiveCNCMessage` |
| `JobCompletedEvent` | On completion | Job finish notification | `ReceiveCNCMessage` |
| `ConnectionStatusChanged` | On change | CNC connection state changes | Direct SignalR method `connection.on("ConnectionStatusChanged")` |
| `MessageEvent` | Variable | CNC12 messages and alerts | `ReceiveCNCMessage` |

*When throttling is enabled (currently disabled)

**Note:** Heartbeat and ConnectionStatusChanged use dedicated SignalR methods for better reliability. All other events use the standard `ReceiveCNCMessage` wrapper.

---

## 9. Breaking Changes Summary

| Change | Impact | Migration |
|--------|--------|-----------|
| Single SignalR group | Low | Remove per-message subscriptions |
| JobId in all job events | None | Use JobId for job tracking |
| FilePath in responses | None | Optional - use for display |
| Heartbeat every 2s | Low | Update interval expectations |
| Message ordering guarantee | None | Can rely on FIFO order now |

---

## 10. Benefits Summary

### For Frontend Developers
- ✅ **Reliable job tracking** - Use JobId as unique key across all events
- ✅ **Progress monitoring** - StepExecutionEvent for line-by-line tracking
- ✅ **Predictable ordering** - Messages arrive in sequence
- ✅ **Better performance** - Faster message delivery
- ✅ **Simpler code** - Single group, client-side filtering
- ✅ **Real-time status** - Heartbeat every 2 seconds

### For Users
- ✅ **More responsive UI** - Faster position updates
- ✅ **Accurate feedback** - Ordered message display
- ✅ **Job tracking** - Know when jobs complete
- ✅ **Connection monitoring** - Immediate disconnect detection

---

## Support

For questions or issues with these changes:
- Check message console logs for `EventType` values
- Verify SignalR connection to `"CNCClients"` group
- Confirm `JobId` is being received in all job events
- Use `JobId` for tracking, `FilePath` for display
- Test heartbeat reception (should arrive every 2 seconds)

**Document Version**: 1.0  
**Last Updated**: November 10, 2025
