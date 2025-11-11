# ServerStatus Event - API Response Structure

## Overview
The server sends a `ServerStatus` event every **2 seconds** via SignalR to all connected clients. This provides **periodic status updates** including server alive status, CNC connection status, and current machine position.

## Event Details

**Event Name:** `ServerStatus`

**Frequency:** Every 2 seconds (hardcoded for reliability)

**SignalR Method:** `connection.on("ReceiveCNCMessage", callback)` - Filter by `EventType === "ServerStatus"`

**Purpose:** Periodic status updates with connection state and current position

## Response Structure

### TypeScript Definition
```typescript
interface ServerStatusMessage {
  EventType: "ServerStatus";
  Timestamp: string;        // ISO 8601 UTC - when sent
  Data: ServerStatusData;
}

interface ServerStatusData {
  Timestamp: string;        // ISO 8601 UTC timestamp
  ServerTime: string;       // Local server time
  IsConnected: boolean;     // CNC connection status
  Status: string;           // "Connected" or "Disconnected"
  MessageType: string;      // "ServerStatus"
  IsApiRestricted: boolean; // Whether API is in restricted mode
  Position: Position | null; // Machine position or null
}

interface Position {
  X: number;  // X-axis coordinate
  Y: number;  // Y-axis coordinate
  Z: number;  // Z-axis coordinate
  A: number;  // A-axis (rotary) coordinate in degrees
}
```

### Example Responses

**Connected with Position:**
```json
{
  "EventType": "ServerStatus",
  "Timestamp": "2025-11-11T16:45:23.123Z",
  "Data": {
    "Timestamp": "2025-11-11T16:45:23.123Z",
    "ServerTime": "2025-11-11T12:45:23.123",
    "IsConnected": true,
    "Status": "Connected",
    "MessageType": "ServerStatus",
    "IsApiRestricted": false,
    "Position": {
      "X": 5.2500,
      "Y": 3.1250,
      "Z": -0.5000,
      "A": 90.0000
    }
  }
}
```

**Disconnected:**
```json
{
  "EventType": "ServerStatus",
  "Timestamp": "2025-11-11T16:45:23.123Z",
  "Data": {
    "Timestamp": "2025-11-11T16:45:23.123Z",
    "ServerTime": "2025-11-11T12:45:23.123",
    "IsConnected": false,
    "Status": "Disconnected",
    "MessageType": "ServerStatus",
    "IsApiRestricted": false,
    "Position": null
  }
}
```

## Field Descriptions

| Field | Type | Description |
|-------|------|-------------|
| `Timestamp` | string | UTC timestamp when status was generated (ISO 8601 format) |
| `ServerTime` | string | Server's local time when status was generated |
| `IsConnected` | boolean | `true` = CNC machine is connected and operational<br>`false` = CNC machine is disconnected or unavailable |
| `Status` | string | Human-readable status: `"Connected"` or `"Disconnected"` |
| `MessageType` | string | Always `"ServerStatus"` - used to identify message type |
| `IsApiRestricted` | boolean | `true` if API is in restricted mode (cannot run commands), `false` if full access |
| `Position` | object or null | Current machine position if connected, `null` otherwise |
| `Position.X` | number | X-axis position in machine units (inches or millimeters) |
| `Position.Y` | number | Y-axis position in machine units |
| `Position.Z` | number | Z-axis position in machine units |
| `Position.A` | number | A-axis (rotary) position in degrees |

## Important Notes

### Position Data
- **Position IS included** in server status updates
- Provides a position snapshot every 2 seconds
- `Position` is `null` when `IsConnected` is `false`
- `Position` may be `null` even when connected if position retrieval fails
- **DRO events** provide higher-frequency position updates during movement (~10Hz)
- Server status ensures you always have recent position data, even when machine is idle

### Timing
- Status updates are sent every **2 seconds** (hardcoded for reliability)
- Use `Timestamp` for precise timing calculations
- Monitor for missed updates to detect server/network issues quickly
- Consider disconnected if no update received within 6 seconds (3x interval)

### Why This Design?
- ✅ **Complete** - Includes both connection status and position
- ✅ **Reliable** - Regular updates ensure fresh data
- ✅ **Complementary** - Works with DRO events for real-time updates
- ✅ **Fast detection** - 2-second interval detects disconnections quickly

## Client Implementation Example

### JavaScript/TypeScript
```javascript
// Listen for all messages including server status
connection.on("ReceiveCNCMessage", (message) => {
  if (message.EventType === "ServerStatus") {
    const status = message.Data;
    // Update connection indicators
    updateServerIndicator(true); // Server is alive
    updateCNCIndicator(status.IsConnected);
    
    // Update position display
    if (status.Position) {
      displayPosition({
        x: status.Position.X.toFixed(4),
        y: status.Position.Y.toFixed(4),
        z: status.Position.Z.toFixed(4),
        a: status.Position.A.toFixed(4)
      });
    } else {
      clearPositionDisplay();
    }
    
    // Track last status update for disconnect detection
    lastStatusTime = Date.now();
  }
  
  if (message.EventType === "DROEvent") {
    // High-frequency position updates during movement
    const position = message.Data;
    displayPosition({
      x: position.Axis1.toFixed(4),
      y: position.Axis2.toFixed(4),
      z: position.Axis3.toFixed(4),
      a: position.Axis4.toFixed(4)
    });
  }
});
```

### React Example
```typescript
import { useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';

interface CNCMessage {
  EventType: string;
  Timestamp: string;
  Data: any;
}

interface ServerStatusData {
  Timestamp: string;
  ServerTime: string;
  IsConnected: boolean;
  Status: string;
  MessageType: string;
  Position: { X: number; Y: number; Z: number; A: number } | null;
}

function CNCStatusComponent() {
  const [serverAlive, setServerAlive] = useState(false);
  const [cncConnected, setCncConnected] = useState(false);
  const [position, setPosition] = useState<any>(null);
  
  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl("/cncHub")
      .build();
    
    connection.on("ReceiveCNCMessage", (message: CNCMessage) => {
      if (message.EventType === "ServerStatus") {
        const status = message.Data as ServerStatusData;
        setServerAlive(true);
        setCncConnected(status.IsConnected);
        setPosition(status.Position);
      }
    });
    
    connection.start();
    
    // Check for missed status updates
    const interval = setInterval(() => {
      // If no update in 6 seconds, consider disconnected
      // (Set logic in your actual implementation)
    }, 1000);
    
    return () => {
      connection.stop();
      clearInterval(interval);
    };
  }, []);
  
  return (
    <div>
      <div>Server: {serverAlive ? 'Connected' : 'Disconnected'}</div>
      <div>CNC: {cncConnected ? 'Connected' : 'Disconnected'}</div>
      {position && (
        <div>
          Position: 
          X: {position.X.toFixed(4)}, 
          Y: {position.Y.toFixed(4)}, 
          Z: {position.Z.toFixed(4)}, 
          A: {position.A.toFixed(4)}
        </div>
      )}
    </div>
  );
}
```

## Error Handling

### Detecting Disconnection
If no heartbeat is received within 6 seconds (3x the 2-second interval), consider the server disconnected:

```javascript
let lastStatusTime = null;

connection.on("ReceiveCNCMessage", (message) => {
  if (message.EventType === "ServerStatus") {
    lastStatusTime = Date.now();
    // ... handle status update
  }
});

setInterval(() => {
  if (lastStatusTime && (Date.now() - lastStatusTime) > 6000) {
    // Server appears to be disconnected
    showServerDisconnectedWarning();
  }
}, 1000); // Check every second
```

## Related Events

- **ConnectionStatusChanged**: Fired when CNC connection status changes (immediate notification)
- **DROEvent**: Real-time position updates via `ReceiveCNCMessage` (~10Hz during movement)

ServerStatus provides periodic baseline updates every 2 seconds with both connection state and position, while DROEvent provides high-frequency real-time position updates during active machine movement.
