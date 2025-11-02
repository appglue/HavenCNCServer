# SignalR Connection Status Broadcasting

## Overview

The HavenCNCServer automatically broadcasts CNC connection status changes to all connected SignalR clients. This allows front-end applications to be notified in real-time when the server gains or loses connection to the Centroid CNC system.

Additionally, the server sends periodic heartbeat messages containing system status information to ensure clients know the server is alive and operational.

## Heartbeat Event

### Event Name
`Heartbeat`

### When Triggered
This event is broadcast periodically (default: every 30 seconds, configurable in `appsettings.json` via `HeartbeatIntervalMs`) to inform clients that the server is alive and provide current system status.

### Message Format

**When Connected:**
```json
{
  "Timestamp": "2025-10-31T12:34:56.789Z",
  "ServerTime": "2025-10-31T08:34:56.789",
  "IsConnected": true,
  "Status": "Connected",
  "MessageType": "Heartbeat",
  "Position": {
    "X": 1.2345,
    "Y": 2.3456,
    "Z": 3.4567,
    "A": 0.0
  }
}
```

**When Disconnected:**
```json
{
  "Timestamp": "2025-10-31T12:34:56.789Z",
  "ServerTime": "2025-10-31T08:34:56.789",
  "IsConnected": false,
  "Status": "Disconnected",
  "MessageType": "Heartbeat",
  "Position": null
}
```

### Message Properties

| Property | Type | Description |
|----------|------|-------------|
| `Timestamp` | `string` | UTC timestamp when heartbeat was sent (ISO 8601 format) |
| `ServerTime` | `string` | Local server time when heartbeat was sent |
| `IsConnected` | `boolean` | `true` if server is connected to CNC, `false` otherwise |
| `Status` | `string` | Either `"Connected"` or `"Disconnected"` |
| `MessageType` | `string` | Always `"Heartbeat"` |
| `Position` | `object` or `null` | Current machine position if connected, `null` if disconnected or position unavailable |
| `Position.X` | `number` | X-axis position in machine units (inches or mm) |
| `Position.Y` | `number` | Y-axis position in machine units |
| `Position.Z` | `number` | Z-axis position in machine units |
| `Position.A` | `number` | A-axis (rotary) position in degrees |

### Purpose
- Allows clients to detect if the SignalR connection is still active
- Provides regular updates on CNC connection status without polling
- Helps clients distinguish between server unavailability and CNC disconnection
- Includes current machine position for real-time display updates
- Can be used to update "last seen" timestamps in the UI

### Data Structure for Frontend/AI

**Event Name:** `Heartbeat`

**Broadcast Frequency:** Every 30 seconds (configurable)

**TypeScript Interface:**
```typescript
interface Heartbeat {
  Timestamp: string;        // ISO 8601 UTC timestamp: "2025-10-31T12:34:56.789Z"
  ServerTime: string;       // Local server time: "2025-10-31T08:34:56.789"
  IsConnected: boolean;     // true = CNC connected, false = CNC disconnected
  Status: string;           // "Connected" or "Disconnected"
  MessageType: string;      // Always "Heartbeat"
  Position: {               // null if disconnected or unavailable
    X: number;              // X-axis position (machine units)
    Y: number;              // Y-axis position (machine units)
    Z: number;              // Z-axis position (machine units)
    A: number;              // A-axis rotary position (degrees)
  } | null;
}
```

**JSON Schema:**
```json
{
  "type": "object",
  "properties": {
    "Timestamp": { "type": "string", "format": "date-time" },
    "ServerTime": { "type": "string", "format": "date-time" },
    "IsConnected": { "type": "boolean" },
    "Status": { "type": "string", "enum": ["Connected", "Disconnected"] },
    "MessageType": { "type": "string", "const": "Heartbeat" },
    "Position": {
      "type": ["object", "null"],
      "properties": {
        "X": { "type": "number" },
        "Y": { "type": "number" },
        "Z": { "type": "number" },
        "A": { "type": "number" }
      },
      "required": ["X", "Y", "Z", "A"]
    }
  },
  "required": ["Timestamp", "ServerTime", "IsConnected", "Status", "MessageType", "Position"]
}
```

**Usage Notes:**
- Always check `IsConnected` before using `Position` data
- `Position` will be `null` when `IsConnected` is `false`
- `Position` may also be `null` even when connected if position retrieval fails
- All position values are in machine coordinates
- Units (inches/mm) depend on machine configuration

## Connection Status Event

### Event Name
`ConnectionStatusChanged`

### When Triggered
This event is broadcast whenever:
- The server successfully connects to the Centroid CNC system
- The server loses connection to the Centroid CNC system
- Connection attempts fail
- Connection is manually disconnected

### Message Format

```json
{
  "IsConnected": true,
  "Message": "Connected to CNC successfully",
  "Timestamp": "2025-10-31T12:34:56.789Z",
  "Status": "Connected"
}
```

### Message Properties

| Property | Type | Description |
|----------|------|-------------|
| `IsConnected` | `boolean` | `true` if connected to CNC, `false` if disconnected |
| `Message` | `string` | Human-readable description of the connection status change |
| `Timestamp` | `string` | UTC timestamp when the status change occurred (ISO 8601 format) |
| `Status` | `string` | Either `"Connected"` or `"Disconnected"` |

## Example Messages

### Successful Connection
```json
{
  "IsConnected": true,
  "Message": "Connected to CNC successfully",
  "Timestamp": "2025-10-31T12:34:56.789Z",
  "Status": "Connected"
}
```

### Connection Verified
```json
{
  "IsConnected": true,
  "Message": "CNC connection verified and ready",
  "Timestamp": "2025-10-31T12:34:57.123Z",
  "Status": "Connected"
}
```

### Connection Attempt
```json
{
  "IsConnected": false,
  "Message": "Connection attempt 1/3...",
  "Timestamp": "2025-10-31T12:34:50.456Z",
  "Status": "Disconnected"
}
```

### Connection Failed
```json
{
  "IsConnected": false,
  "Message": "All connection attempts failed",
  "Timestamp": "2025-10-31T12:35:00.789Z",
  "Status": "Disconnected"
}
```

### Manual Disconnect
```json
{
  "IsConnected": false,
  "Message": "Disconnected from CNC",
  "Timestamp": "2025-10-31T12:45:00.123Z",
  "Status": "Disconnected"
}
```

## Client Implementation

### JavaScript/TypeScript Example

```javascript
let lastHeartbeatTime = null;

// Subscribe to heartbeat messages
connection.on("Heartbeat", (heartbeat) => {
    console.log(`Server alive at: ${heartbeat.ServerTime}`);
    console.log(`CNC Status: ${heartbeat.Status}`);
    
    lastHeartbeatTime = new Date(heartbeat.Timestamp);
    
    // Update UI with connection status
    updateServerStatus("Server Connected");
    updateCNCStatus(heartbeat.IsConnected);
    
    // Update position display if available
    if (heartbeat.Position) {
        console.log(`Position: X=${heartbeat.Position.X}, Y=${heartbeat.Position.Y}, Z=${heartbeat.Position.Z}, A=${heartbeat.Position.A}`);
        updatePositionDisplay(heartbeat.Position);
    } else {
        clearPositionDisplay();
    }
    
    // Clear any "server disconnected" warnings
    clearServerWarning();
});

// Subscribe to connection status changes
connection.on("ConnectionStatusChanged", (status) => {
    console.log(`CNC Status: ${status.Status}`);
    console.log(`Message: ${status.Message}`);
    console.log(`Connected: ${status.IsConnected}`);
    
    // Update UI based on connection status
    if (status.IsConnected) {
        showConnectedIndicator();
        enableCNCControls();
    } else {
        showDisconnectedIndicator();
        disableCNCControls();
    }
});

// Monitor for missed heartbeats (optional)
setInterval(() => {
    if (lastHeartbeatTime) {
        const secondsSinceHeartbeat = (Date.now() - lastHeartbeatTime.getTime()) / 1000;
        // Warn if no heartbeat for 60 seconds (2x the default interval)
        if (secondsSinceHeartbeat > 60) {
            showServerWarning("No heartbeat received - server may be disconnected");
        }
    }
}, 10000); // Check every 10 seconds
```

### C# Example

```csharp
// Subscribe to heartbeat messages
hubConnection.On<Heartbeat>("Heartbeat", heartbeat =>
{
    Console.WriteLine($"Server Time: {heartbeat.ServerTime}");
    Console.WriteLine($"CNC Status: {heartbeat.Status}");
    
    // Update last heartbeat timestamp
    LastHeartbeatReceived = heartbeat.Timestamp;
    
    // Update UI
    UpdateServerStatus("Server Connected");
    UpdateCNCStatus(heartbeat.IsConnected);
    
    // Update position display if available
    if (heartbeat.Position != null)
    {
        Console.WriteLine($"Position: X={heartbeat.Position.X}, Y={heartbeat.Position.Y}, Z={heartbeat.Position.Z}, A={heartbeat.Position.A}");
        UpdatePositionDisplay(heartbeat.Position);
    }
    else
    {
        ClearPositionDisplay();
    }
});

// Subscribe to connection status changes
hubConnection.On<ConnectionStatus>("ConnectionStatusChanged", status =>
{
    Console.WriteLine($"CNC Status: {status.Status}");
    Console.WriteLine($"Message: {status.Message}");
    
    if (status.IsConnected)
    {
        // Enable CNC operations
    }
    else
    {
        // Disable CNC operations
    }
});

public class Heartbeat
{
    public DateTime Timestamp { get; set; }
    public DateTime ServerTime { get; set; }
    public bool IsConnected { get; set; }
    public string Status { get; set; }
    public string MessageType { get; set; }
    public MachinePosition? Position { get; set; }
}

public class MachinePosition
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public double A { get; set; }
}

public class ConnectionStatus
{
    public bool IsConnected { get; set; }
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
    public string Status { get; set; }
}
```

## Best Practices

1. **Monitor Heartbeats**: Subscribe to `Heartbeat` events to ensure the server is responsive and to track CNC connection status.

2. **Implement Heartbeat Timeout Detection**: If no heartbeat is received within 2x the configured interval (default: 60 seconds), warn users that the server may be disconnected.

3. **Always Monitor Connection Status**: Subscribe to `ConnectionStatusChanged` events to ensure your application knows when CNC operations are available.

4. **Disable UI Controls When Disconnected**: Prevent users from attempting CNC operations when the connection is lost.

5. **Show Visual Indicators**: Display clear visual indicators:
   - Server connection status (based on heartbeat reception)
   - CNC connection status (from heartbeat or ConnectionStatusChanged)

6. **Handle Reconnection**: When a disconnection occurs, your application should wait for the reconnection event before allowing CNC operations again.

7. **Log Connection Events**: Log connection status changes and missed heartbeats for debugging and monitoring purposes.

8. **Use Heartbeat for Status**: Instead of polling the server for status, rely on the periodic heartbeat messages which provide the same information automatically.

9. **Distinguish Between Server and CNC Issues**: Use heartbeats to differentiate between:
   - No heartbeat = Server/network issue
   - Heartbeat with `IsConnected: false` = Server OK, CNC disconnected

## Related API Endpoints

- `GET /api/CNCSystem/IsConnectedToCentroid` - Check if currently connected by attempting to retrieve machine position
- `GET /api/CNCSystem/Status` - Get overall system status including connection state
- `POST /api/CNCServer/Connect` - Manually trigger connection attempt (if available)
- `POST /api/CNCServer/Disconnect` - Manually disconnect from CNC (if available)

## Configuration

### Heartbeat Interval
The heartbeat interval can be configured in `appsettings.json`:

```json
{
  "Cnc": {
    "HeartbeatIntervalMs": 30000
  }
}
```

Default: 30000 milliseconds (30 seconds)

## Technical Details

### Implementation
The connection status broadcasting is implemented in:
- **Connection Manager**: `CNCConnectionManager` (raises `ConnectionStatusChanged` event)
- **SignalR Manager**: `SignalRManager` (subscribes to event, broadcasts to clients, and manages heartbeat timer)

### Heartbeat Flow
1. `SignalRManager.StartHeartbeatTimer()` is called during setup
2. Timer fires every `HeartbeatIntervalMs` (default: 30 seconds)
3. `SendHeartbeat()` retrieves current CNC connection status
4. Heartbeat message is formatted and broadcast to all SignalR clients in the `CNCClients` group
5. All connected clients receive the `Heartbeat` event

### Connection Status Change Flow
1. CNC connection status changes in `CNCConnectionManager`
2. `ConnectionStatusChanged` event is fired with `isConnected` flag and message
3. `SignalRManager.OnConnectionStatusChanged` receives the event
4. Message is formatted and broadcast to all SignalR clients in the `CNCClients` group
5. All connected clients receive the `ConnectionStatusChanged` event

### Groups
Connection status messages are sent to:
- **CNCClients**: All clients subscribed to CNC events

To receive these messages, clients must join the `CNCClients` group when connecting to the hub.
