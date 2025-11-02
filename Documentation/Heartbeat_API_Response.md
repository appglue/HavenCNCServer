# Heartbeat Event - API Response Structure

## Overview
The server sends a `Heartbeat` event every 30 seconds via SignalR to all connected clients. This event provides server status, CNC connection status, and current machine position.

## Event Details

**Event Name:** `Heartbeat`

**Frequency:** Every 30 seconds (configurable via `appsettings.json`)

**SignalR Method:** `connection.on("Heartbeat", callback)`

## Response Structure

### TypeScript Definition
```typescript
interface HeartbeatResponse {
  Timestamp: string;        // ISO 8601 UTC timestamp
  ServerTime: string;       // Local server time
  IsConnected: boolean;     // CNC connection status
  Status: string;           // "Connected" or "Disconnected"
  MessageType: string;      // Always "Heartbeat"
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
  "Timestamp": "2025-10-31T16:45:23.123Z",
  "ServerTime": "2025-10-31T12:45:23.123",
  "IsConnected": true,
  "Status": "Connected",
  "MessageType": "Heartbeat",
  "Position": {
    "X": 5.2500,
    "Y": 3.1250,
    "Z": -0.5000,
    "A": 90.0000
  }
}
```

**Disconnected:**
```json
{
  "Timestamp": "2025-10-31T16:45:23.123Z",
  "ServerTime": "2025-10-31T12:45:23.123",
  "IsConnected": false,
  "Status": "Disconnected",
  "MessageType": "Heartbeat",
  "Position": null
}
```

## Field Descriptions

| Field | Type | Description |
|-------|------|-------------|
| `Timestamp` | string | UTC timestamp when heartbeat was generated (ISO 8601 format) |
| `ServerTime` | string | Server's local time when heartbeat was generated |
| `IsConnected` | boolean | `true` = CNC machine is connected and operational<br>`false` = CNC machine is disconnected or unavailable |
| `Status` | string | Human-readable status: `"Connected"` or `"Disconnected"` |
| `MessageType` | string | Always `"Heartbeat"` - used to identify message type |
| `Position` | object or null | Current machine position if connected, `null` otherwise |
| `Position.X` | number | X-axis position in machine units (inches or millimeters) |
| `Position.Y` | number | Y-axis position in machine units |
| `Position.Z` | number | Z-axis position in machine units |
| `Position.A` | number | A-axis (rotary) position in degrees |

## Important Notes

### Position Availability
- `Position` is `null` when `IsConnected` is `false`
- `Position` may be `null` even when `IsConnected` is `true` if position data retrieval fails
- Always check for null before accessing position properties

### Coordinate System
- Position values are in **machine coordinates**
- Units depend on machine configuration (typically inches or millimeters)
- A-axis is always in degrees

### Timing
- Heartbeats are sent every 30 seconds by default
- Use `Timestamp` for precise timing calculations
- Monitor for missed heartbeats to detect server/network issues

## Client Implementation Example

### JavaScript/TypeScript
```javascript
connection.on("Heartbeat", (heartbeat) => {
  // Update connection indicators
  const isServerAlive = true;
  const isCNCConnected = heartbeat.IsConnected;
  
  // Update UI
  updateServerIndicator(isServerAlive);
  updateCNCIndicator(isCNCConnected);
  
  // Update position display if available
  if (heartbeat.Position !== null) {
    displayPosition({
      x: heartbeat.Position.X.toFixed(4),
      y: heartbeat.Position.Y.toFixed(4),
      z: heartbeat.Position.Z.toFixed(4),
      a: heartbeat.Position.A.toFixed(4)
    });
  } else {
    displayPosition(null); // Clear or disable position display
  }
  
  // Update timestamp
  updateLastHeartbeat(new Date(heartbeat.Timestamp));
});
```

### React Example
```typescript
import { useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';

interface HeartbeatResponse {
  Timestamp: string;
  ServerTime: string;
  IsConnected: boolean;
  Status: string;
  MessageType: string;
  Position: { X: number; Y: number; Z: number; A: number } | null;
}

function CNCStatusComponent() {
  const [heartbeat, setHeartbeat] = useState<HeartbeatResponse | null>(null);
  
  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl("/cncHub")
      .build();
    
    connection.on("Heartbeat", (data: HeartbeatResponse) => {
      setHeartbeat(data);
    });
    
    connection.start();
    
    return () => {
      connection.stop();
    };
  }, []);
  
  return (
    <div>
      <div>Server: {heartbeat ? 'Connected' : 'Disconnected'}</div>
      <div>CNC: {heartbeat?.Status || 'Unknown'}</div>
      {heartbeat?.Position && (
        <div>
          Position: 
          X: {heartbeat.Position.X.toFixed(4)}, 
          Y: {heartbeat.Position.Y.toFixed(4)}, 
          Z: {heartbeat.Position.Z.toFixed(4)}, 
          A: {heartbeat.Position.A.toFixed(4)}
        </div>
      )}
    </div>
  );
}
```

## Error Handling

### Detecting Disconnection
If no heartbeat is received within 60 seconds (2x the interval), consider the server disconnected:

```javascript
let lastHeartbeatTime = null;

connection.on("Heartbeat", (heartbeat) => {
  lastHeartbeatTime = Date.now();
  // ... handle heartbeat
});

setInterval(() => {
  if (lastHeartbeatTime && (Date.now() - lastHeartbeatTime) > 60000) {
    // Server appears to be disconnected
    showServerDisconnectedWarning();
  }
}, 10000); // Check every 10 seconds
```

### Handling Position Null
Always check for null before using position data:

```javascript
if (heartbeat.Position !== null && heartbeat.Position !== undefined) {
  // Safe to use position
  const x = heartbeat.Position.X;
} else {
  // Position unavailable - show placeholder or disable display
}
```

## Related Events

- **ConnectionStatusChanged**: Fired when CNC connection status changes (not periodic)
- **DROUpdate**: Real-time position updates during movement (high frequency)

The Heartbeat event provides a baseline status update, while DROUpdate provides real-time position during active operations.
