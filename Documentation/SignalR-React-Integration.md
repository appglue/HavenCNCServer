# SignalR Integration with React

This guide explains how to integrate the HavenCNC Server SignalR hub into a React application to receive real-time CNC events.

## Table of Contents

- [Overview](#overview)
- [Installation](#installation)
- [Connection Setup](#connection-setup)
- [Event Types & Data Structures](#event-types--data-structures)
- [React Hook Example](#react-hook-example)
- [Component Examples](#component-examples)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

---

## Overview

The HavenCNC Server exposes a SignalR hub at `/cncHub` that broadcasts real-time CNC events to connected clients. These events include:

- **DRO Updates** - Machine position changes (X, Y, Z axes)
- **Messages** - CNC status messages, errors, warnings
- **Job Events** - Job start, completion, step execution
- **System Events** - Configuration changes, faults, etc.

### Hub Endpoint

```
http://localhost:5000/cncHub
```

### Message Format

All messages follow this structure:

```typescript
{
  MessageType: string,      // Event class name (e.g., "DROEvent", "MessageEvent")
  Timestamp: string,        // ISO 8601 timestamp
  Data: object             // Event-specific data (see below)
}
```

---

## Installation

Install the SignalR client library:

```bash
npm install @microsoft/signalr
```

For TypeScript projects, the types are included.

---

## Connection Setup

### Basic Connection

```typescript
import * as signalR from '@microsoft/signalr';

const connection = new signalR.HubConnectionBuilder()
  .withUrl('http://localhost:5000/cncHub')
  .withAutomaticReconnect()
  .configureLogging(signalR.LogLevel.Information)
  .build();

// Start the connection
connection.start()
  .then(() => console.log('Connected to CNC Hub'))
  .catch(err => console.error('Connection failed:', err));
```

### Connection with Error Handling

```typescript
const connection = new signalR.HubConnectionBuilder()
  .withUrl('http://localhost:5000/cncHub')
  .withAutomaticReconnect({
    nextRetryDelayInMilliseconds: retryContext => {
      // Exponential backoff: 0s, 2s, 10s, 30s
      if (retryContext.previousRetryCount === 0) return 0;
      if (retryContext.previousRetryCount === 1) return 2000;
      if (retryContext.previousRetryCount === 2) return 10000;
      return 30000;
    }
  })
  .configureLogging(signalR.LogLevel.Information)
  .build();

// Connection lifecycle events
connection.onreconnecting(error => {
  console.log('Reconnecting...', error);
});

connection.onreconnected(connectionId => {
  console.log('Reconnected:', connectionId);
});

connection.onclose(error => {
  console.log('Connection closed:', error);
});
```

---

## Event Types & Data Structures

### 1. DRO Event (Machine Position Updates)

**MessageType:** `"DROEvent"`

Sent when machine axes positions change.

```typescript
interface DROEvent {
  Timestamp: string;
  Axis1: number;        // X axis position
  Axis2: number;        // Y axis position
  Axis3: number;        // Z axis position
  Axis4: number;        // A axis position (rotary)
  Axis5: number;        // B axis position
  Axis6: number;        // C axis position
  Axis7: number;        // Additional axis
  Axis8: number;        // Additional axis
  Message: string;      // Human-readable position summary
}
```

**Example:**
```json
{
  "MessageType": "DROEvent",
  "Timestamp": "2025-10-16T17:57:08.123Z",
  "Data": {
    "Timestamp": "2025-10-16T17:57:08.123Z",
    "Axis1": 12.5000,
    "Axis2": -5.2500,
    "Axis3": 0.0000,
    "Axis4": 0.0000,
    "Axis5": 0.0000,
    "Axis6": 0.0000,
    "Axis7": 0.0000,
    "Axis8": 0.0000,
    "Message": "DRO positions updated: 12.5000, -5.2500, 0.0000"
  }
}
```

---

### 2. Message Event (Status, Errors, Warnings)

**MessageType:** `"MessageEvent"`

Sent when CNC system displays a message.

```typescript
enum MessageEventType {
  // Status messages (300-399)
  StatusMessage = 0,
  JobStarted = 1,
  JobCompleted = 2,
  JobCancelled = 3,
  
  // Errors (400-999)
  SystemFault = 4,
  AxisFault = 5,
  LimitError = 6,
  ProbeError = 7,
  CommunicationError = 8,
  StartupError = 9,
  SyntaxError = 10,
  GCodeError = 11,
  ParameterError = 12,
  CutterCompensationError = 13,
  ParameterSettingError = 14,
  CannedCycleError = 15,
  MiscellaneousError = 16,
  ScalingError = 17,
  
  // Other
  ConfigurationChange = 18,
  ExitMessage = 19,
  Unknown = 20
}

interface MessageEvent {
  Timestamp: string;
  EventCode: number;              // Centroid error code (e.g., 306, 407)
  Message: string;                // Error/status message text
  EventType: MessageEventType;    // Classified message type
}
```

**Example (Job Completed):**
```json
{
  "MessageType": "MessageEvent",
  "Timestamp": "2025-10-16T17:57:10.456Z",
  "Data": {
    "Timestamp": "2025-10-16T17:57:10.456Z",
    "EventCode": 306,
    "Message": "Job Finished",
    "EventType": 2
  }
}
```

**Example (Limit Error):**
```json
{
  "MessageType": "MessageEvent",
  "Timestamp": "2025-10-16T17:57:15.789Z",
  "Data": {
    "Timestamp": "2025-10-16T17:57:15.789Z",
    "EventCode": 407,
    "Message": "Limit (X+) tripped",
    "EventType": 6
  }
}
```

---

### 3. Job Info Event

**MessageType:** `"JobInfoEvent"`

Sent when job execution information changes (line number, stack level).

```typescript
interface JobInfoEvent {
  Timestamp: string;
  LineNumber: number;     // Current executing line number
  StackLevel: number;     // Stack level for nested programs
  Message: string;        // Job message text
  JobName: string;        // Name of current job
}
```

**Example:**
```json
{
  "MessageType": "JobInfoEvent",
  "Timestamp": "2025-10-16T17:57:12.345Z",
  "Data": {
    "Timestamp": "2025-10-16T17:57:12.345Z",
    "LineNumber": 42,
    "StackLevel": 1,
    "Message": "circle_pattern.cnc",
    "JobName": "circle_pattern.cnc"
  }
}
```

---

### 4. Job Started Event

**MessageType:** `"JobStartedEvent"`

Sent when a new job begins execution.

```typescript
interface JobStartedEvent {
  Timestamp: string;
  Message: string;        // Description
  JobId: string;          // Unique job identifier
  GCodeLines: string[];   // Array of G-code lines
  TotalLines: number;     // Total lines in job
  IsStepRunMode: boolean; // True if running step-by-step
  FilePath?: string;      // File path if loaded from file
}
```

**Example:**
```json
{
  "MessageType": "JobStartedEvent",
  "Timestamp": "2025-10-16T17:57:05.000Z",
  "Data": {
    "Timestamp": "2025-10-16T17:57:05.000Z",
    "Message": "Job started",
    "JobId": "job_20251016_175705",
    "GCodeLines": ["G0 X0 Y0", "G1 Z-5 F100", "G0 Z5"],
    "TotalLines": 3,
    "IsStepRunMode": false,
    "FilePath": "C:\\CNC\\Programs\\test.cnc"
  }
}
```

---

### 5. Job Completed Event

**MessageType:** `"JobCompletedEvent"`

Sent when a job finishes.

```typescript
interface JobCompletedEvent {
  Timestamp: string;
  Message: string;
  JobId: string;
  Success: boolean;           // True if completed successfully
  ErrorMessage?: string;      // Error message if failed
  Duration: string;           // ISO 8601 duration (e.g., "PT5M30S")
  LinesExecuted: number;      // Number of lines executed
}
```

**Example:**
```json
{
  "MessageType": "JobCompletedEvent",
  "Timestamp": "2025-10-16T17:57:35.000Z",
  "Data": {
    "Timestamp": "2025-10-16T17:57:35.000Z",
    "Message": "Job completed successfully",
    "JobId": "job_20251016_175705",
    "Success": true,
    "ErrorMessage": null,
    "Duration": "PT30S",
    "LinesExecuted": 3
  }
}
```

---

### 6. Step Execution Event

**MessageType:** `"StepExecutionEvent"`

Sent during step-by-step job execution.

```typescript
enum StepExecutionStatus {
  AboutToExecute = 0,
  Executing = 1,
  Completed = 2,
  Failed = 3,
  Skipped = 4
}

interface StepExecutionEvent {
  Timestamp: string;
  Message: string;
  JobId: string;
  LineNumber: number;           // Current line (1-based)
  CurrentLine: string;          // G-code line text
  TotalLines: number;           // Total lines in job
  IsLastStep: boolean;          // True if this is the last step
  Status: StepExecutionStatus;  // Execution status
}
```

**Example:**
```json
{
  "MessageType": "StepExecutionEvent",
  "Timestamp": "2025-10-16T17:57:20.000Z",
  "Data": {
    "Timestamp": "2025-10-16T17:57:20.000Z",
    "Message": "Executing step 2 of 3",
    "JobId": "job_20251016_175705",
    "LineNumber": 2,
    "CurrentLine": "G1 Z-5 F100",
    "TotalLines": 3,
    "IsLastStep": false,
    "Status": 1
  }
}
```

---

## React Hook Example

Create a custom hook to manage the SignalR connection:

### `useCNCHub.ts`

```typescript
import { useEffect, useRef, useState } from 'react';
import * as signalR from '@microsoft/signalr';

interface CNCMessage {
  MessageType: string;
  Timestamp: string;
  Data: any;
}

export function useCNCHub(hubUrl: string = 'http://localhost:5000/cncHub') {
  const [isConnected, setIsConnected] = useState(false);
  const [messages, setMessages] = useState<CNCMessage[]>([]);
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    // Create connection
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    connectionRef.current = connection;

    // Handle connection state
    connection.onreconnecting(() => setIsConnected(false));
    connection.onreconnected(() => setIsConnected(true));
    connection.onclose(() => setIsConnected(false));

    // Listen for CNC messages
    connection.on('ReceiveCNCMessage', (message: CNCMessage) => {
      setMessages(prev => [message, ...prev].slice(0, 100)); // Keep last 100
    });

    // Start connection
    connection.start()
      .then(() => {
        console.log('Connected to CNC Hub');
        setIsConnected(true);
      })
      .catch(err => {
        console.error('Failed to connect to CNC Hub:', err);
        setIsConnected(false);
      });

    // Cleanup
    return () => {
      connection.stop();
    };
  }, [hubUrl]);

  // Subscribe to specific message type
  const subscribeToMessageType = async (messageType: string) => {
    if (connectionRef.current && isConnected) {
      await connectionRef.current.invoke('SubscribeToMessageType', messageType);
    }
  };

  // Unsubscribe from specific message type
  const unsubscribeFromMessageType = async (messageType: string) => {
    if (connectionRef.current && isConnected) {
      await connectionRef.current.invoke('UnsubscribeFromMessageType', messageType);
    }
  };

  return {
    isConnected,
    messages,
    subscribeToMessageType,
    unsubscribeFromMessageType
  };
}
```

---

## Component Examples

### 1. Real-time Position Display (DRO)

```typescript
import React from 'react';
import { useCNCHub } from './hooks/useCNCHub';

interface DROEvent {
  Axis1: number;
  Axis2: number;
  Axis3: number;
}

export const DRODisplay: React.FC = () => {
  const { isConnected, messages } = useCNCHub();
  const [position, setPosition] = React.useState<DROEvent | null>(null);

  React.useEffect(() => {
    // Find the most recent DRO event
    const droMessage = messages.find(m => m.MessageType === 'DROEvent');
    if (droMessage) {
      setPosition(droMessage.Data as DROEvent);
    }
  }, [messages]);

  return (
    <div className="dro-display">
      <h3>Machine Position {!isConnected && '(Disconnected)'}</h3>
      {position && (
        <div className="axes">
          <div className="axis">
            <span className="label">X:</span>
            <span className="value">{position.Axis1.toFixed(4)}</span>
          </div>
          <div className="axis">
            <span className="label">Y:</span>
            <span className="value">{position.Axis2.toFixed(4)}</span>
          </div>
          <div className="axis">
            <span className="label">Z:</span>
            <span className="value">{position.Axis3.toFixed(4)}</span>
          </div>
        </div>
      )}
    </div>
  );
};
```

### 2. Message Log Display

```typescript
import React from 'react';
import { useCNCHub } from './hooks/useCNCHub';

interface MessageEvent {
  EventCode: number;
  Message: string;
  EventType: number;
}

const getMessageClassName = (eventType: number): string => {
  // 0-3 are status messages
  if (eventType <= 3) return 'status';
  // 4-17 are errors
  if (eventType <= 17) return 'error';
  return 'info';
};

export const MessageLog: React.FC = () => {
  const { isConnected, messages } = useCNCHub();

  const messageEvents = messages.filter(m => m.MessageType === 'MessageEvent');

  return (
    <div className="message-log">
      <h3>CNC Messages {!isConnected && '(Disconnected)'}</h3>
      <div className="messages">
        {messageEvents.map((msg, idx) => {
          const data = msg.Data as MessageEvent;
          return (
            <div 
              key={idx} 
              className={`message ${getMessageClassName(data.EventType)}`}
            >
              <span className="timestamp">
                {new Date(msg.Timestamp).toLocaleTimeString()}
              </span>
              <span className="code">[{data.EventCode}]</span>
              <span className="text">{data.Message}</span>
            </div>
          );
        })}
      </div>
    </div>
  );
};
```

### 3. Job Progress Monitor

```typescript
import React from 'react';
import { useCNCHub } from './hooks/useCNCHub';

interface JobInfoEvent {
  LineNumber: number;
  JobName: string;
}

interface JobStartedEvent {
  JobId: string;
  TotalLines: number;
  IsStepRunMode: boolean;
}

export const JobProgress: React.FC = () => {
  const { isConnected, messages } = useCNCHub();
  const [jobInfo, setJobInfo] = React.useState<JobInfoEvent | null>(null);
  const [totalLines, setTotalLines] = React.useState(0);

  React.useEffect(() => {
    // Check for job started
    const jobStarted = messages.find(m => m.MessageType === 'JobStartedEvent');
    if (jobStarted) {
      const data = jobStarted.Data as JobStartedEvent;
      setTotalLines(data.TotalLines);
    }

    // Get latest job info
    const jobInfoMsg = messages.find(m => m.MessageType === 'JobInfoEvent');
    if (jobInfoMsg) {
      setJobInfo(jobInfoMsg.Data as JobInfoEvent);
    }
  }, [messages]);

  const progress = totalLines > 0 
    ? ((jobInfo?.LineNumber || 0) / totalLines) * 100 
    : 0;

  return (
    <div className="job-progress">
      <h3>Job Progress {!isConnected && '(Disconnected)'}</h3>
      {jobInfo && (
        <>
          <div className="job-name">{jobInfo.JobName}</div>
          <div className="progress-bar">
            <div 
              className="progress-fill" 
              style={{ width: `${progress}%` }}
            />
          </div>
          <div className="progress-text">
            Line {jobInfo.LineNumber} of {totalLines} 
            ({progress.toFixed(1)}%)
          </div>
        </>
      )}
    </div>
  );
};
```

### 4. Combined Dashboard Component

```typescript
import React from 'react';
import { DRODisplay } from './DRODisplay';
import { MessageLog } from './MessageLog';
import { JobProgress } from './JobProgress';
import './Dashboard.css';

export const CNCDashboard: React.FC = () => {
  return (
    <div className="cnc-dashboard">
      <header>
        <h1>HavenCNC Server Dashboard</h1>
      </header>
      <div className="dashboard-grid">
        <div className="panel">
          <DRODisplay />
        </div>
        <div className="panel">
          <JobProgress />
        </div>
        <div className="panel full-width">
          <MessageLog />
        </div>
      </div>
    </div>
  );
};
```

---

## Best Practices

### 1. Message Filtering

Filter messages on the client side to show only relevant events:

```typescript
const errorMessages = messages.filter(m => 
  m.MessageType === 'MessageEvent' && 
  m.Data.EventType >= 4 && 
  m.Data.EventType <= 17
);
```

### 2. Message Type Subscriptions

Subscribe only to specific message types to reduce bandwidth:

```typescript
useEffect(() => {
  if (isConnected) {
    // Only receive DRO updates
    subscribeToMessageType('DROEvent');
    
    return () => {
      unsubscribeFromMessageType('DROEvent');
    };
  }
}, [isConnected]);
```

### 3. Message Buffering

Limit stored messages to prevent memory issues:

```typescript
setMessages(prev => [message, ...prev].slice(0, 100)); // Keep last 100
```

### 4. Connection State Handling

Always check connection state before displaying data:

```typescript
{!isConnected && <div className="warning">Disconnected from CNC</div>}
```

### 5. Debouncing High-Frequency Events

For DRO updates (which can be very frequent), consider debouncing:

```typescript
const [debouncedPosition, setDebouncedPosition] = useState<DROEvent | null>(null);

useEffect(() => {
  const timer = setTimeout(() => {
    const droMessage = messages.find(m => m.MessageType === 'DROEvent');
    if (droMessage) {
      setDebouncedPosition(droMessage.Data);
    }
  }, 100); // Update every 100ms instead of every message

  return () => clearTimeout(timer);
}, [messages]);
```

---

## Troubleshooting

### Connection Issues

**Problem:** Can't connect to the hub

**Solution:**
1. Verify the server is running: `http://localhost:5000`
2. Check CORS settings in the server's `ApiStartup.cs`
3. Ensure the hub URL is correct: `http://localhost:5000/cncHub`
4. Check browser console for CORS errors

### No Messages Received

**Problem:** Connected but not receiving messages

**Solution:**
1. Verify you're listening for `ReceiveCNCMessage`:
   ```typescript
   connection.on('ReceiveCNCMessage', callback);
   ```
2. Check if the CNC is actually generating events
3. Look at server logs to see if events are being sent

### TypeScript Type Errors

**Problem:** TypeScript complains about message data types

**Solution:**
Create type guards:

```typescript
function isDROEvent(data: any): data is DROEvent {
  return 'Axis1' in data && 'Axis2' in data && 'Axis3' in data;
}

// Usage
if (isDROEvent(message.Data)) {
  // TypeScript knows Data is DROEvent
  console.log(message.Data.Axis1);
}
```

### Performance Issues

**Problem:** UI becomes sluggish with many messages

**Solutions:**
1. Limit message buffer size
2. Debounce high-frequency events (DRO updates)
3. Use React.memo for child components
4. Virtualize long message lists

```typescript
import { FixedSizeList } from 'react-window';

const MessageList = ({ messages }) => (
  <FixedSizeList
    height={400}
    itemCount={messages.length}
    itemSize={35}
  >
    {({ index, style }) => (
      <div style={style}>{messages[index].Data.Message}</div>
    )}
  </FixedSizeList>
);
```

---

## Summary

This integration allows your React application to receive real-time CNC events including:

- **Machine positions** (DRO updates)
- **Status messages** and **errors**
- **Job execution progress**
- **Step-by-step execution** events

The SignalR connection automatically reconnects if dropped, and you can filter/subscribe to specific event types as needed.

For more information, see:
- [SignalR JavaScript Client Documentation](https://docs.microsoft.com/en-us/aspnet/core/signalr/javascript-client)
- [HavenCNC Server API Documentation](./CentriodSetupAPI.md)
