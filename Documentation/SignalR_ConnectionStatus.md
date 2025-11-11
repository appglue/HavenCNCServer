# SignalR Connection Status Broadcasting

## Overview

The HavenCNCServer automatically broadcasts CNC connection status changes to all connected SignalR clients. This allows front-end applications to be notified in real-time when the server gains or loses connection to the Centroid CNC system.

Additionally, the server sends periodic heartbeat messages containing system status information to ensure clients know the server is alive and operational.

## ServerStatus Event (Heartbeat)

### Event Name
`ServerStatus`

### When Triggered
This event is broadcast periodically every **2 seconds** to inform clients that the server is alive and provide current CNC connection status and machine position.

### Purpose
- **Keepalive**: Detect server disconnections quickly (2-second interval)
- **Status**: Connected/disconnected status for CNC
- **Position**: Current machine position snapshot
- **Baseline updates**: Ensures position is updated even when machine is idle

**Note:** DRO events provide higher-frequency position updates during movement (~10Hz).

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
let lastStatusTime = null;

// Subscribe to all CNC messages (including server status)
connection.on("ReceiveCNCMessage", (message) => {
    if (message.EventType === "ServerStatus") {
        const status = message.Data;
        console.log(`Server alive at: ${status.ServerTime}`);
        console.log(`CNC Status: ${status.Status}`);
        
        lastStatusTime = new Date(message.Timestamp);
        
        // Update UI with connection status
        updateServerStatus("Server Connected");
        updateCNCStatus(status.IsConnected);
        
        // Update position display if available
        if (status.Position) {
            console.log(`Position: X=${status.Position.X}, Y=${status.Position.Y}, Z=${status.Position.Z}, A=${status.Position.A}`);
            updatePositionDisplay(status.Position);
        } else {
            clearPositionDisplay();
        }
        
        // Clear any "server disconnected" warnings
        clearServerWarning();
    }
    
    if (message.EventType === "DROEvent") {
        // High-frequency position updates during movement
        const position = message.Data;
        console.log(`Position: X=${position.Axis1}, Y=${position.Axis2}, Z=${position.Axis3}, A=${position.Axis4}`);
        updatePositionDisplay(position);
    }
});

// Subscribe to connection status changes
connection.on("ConnectionStatusChanged", (status) => {
    console.log(`CNC Status: ${status.Status}`);
    console.log(`Message: ${status.Message}`);
    console.log(`Connected: ${status.IsConnected}`);
    
// Monitor for missed status updates (optional)
setInterval(() => {
    if (lastStatusTime) {
        const secondsSinceStatus = (Date.now() - lastStatusTime.getTime()) / 1000;
        // Warn if no status update for 6 seconds (3x the 2-second interval)
        if (secondsSinceStatus > 6) {
            showServerWarning("No status update received - server may be disconnected");
        }
    }
}, 1000); // Check every second
```Interval(() => {
    if (lastHeartbeatTime) {
        const secondsSinceHeartbeat = (Date.now() - lastHeartbeatTime.getTime()) / 1000;
        // Warn if no heartbeat for 6 seconds (3x the 2-second interval)
        if (secondsSinceHeartbeat > 6) {
// Subscribe to all CNC messages (including server status)
hubConnection.On<CNCMessage>("ReceiveCNCMessage", message =>
{
    if (message.EventType == "ServerStatus")
    {
        var status = JsonSerializer.Deserialize<ServerStatus>(message.Data.ToString());
        Console.WriteLine($"Server Time: {status.ServerTime}");
        Console.WriteLine($"CNC Status: {status.Status}");
        
        // Update last status timestamp
        LastStatusReceived = status.Timestamp;
        
        // Update UI
        UpdateServerStatus("Server Connected");
        UpdateCNCStatus(status.IsConnected);
        
        // Update position display if available
        if (status.Position != null)
        {
            Console.WriteLine($"Position: X={status.Position.X}, Y={status.Position.Y}, Z={status.Position.Z}, A={status.Position.A}");
            UpdatePositionDisplay(status.Position);
        }
        else
        {
            ClearPositionDisplay();
        }
    }
});mestamp;
        
        // Update UI
        UpdateServerStatus("Server Connected");
        UpdateCNCStatus(heartbeat.IsConnected);
    }
});

// Position updates come through DRO events
// (Handle separately via ReceiveCNCMessage) }
});

// Subscribe to connection status changes
hubConnection.On<ConnectionStatus>("ConnectionStatusChanged", status =>
{
    Console.WriteLine($"CNC Status: {status.Status}");
    Console.WriteLine($"Message: {status.Message}");
    
public class ServerStatus
{
    public DateTime Timestamp { get; set; }
    public DateTime ServerTime { get; set; }
    public bool IsConnected { get; set; }
    public string Status { get; set; }
    public string MessageType { get; set; }
    public bool IsApiRestricted { get; set; }
    public MachinePosition? Position { get; set; }
}

public class MachinePosition
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public double A { get; set; }
}       // Disable CNC operations
    }
});

public class Heartbeat
{
    public DateTime Timestamp { get; set; }
    public DateTime ServerTime { get; set; }
    public bool IsConnected { get; set; }
    public string Status { get; set; }
    public string MessageType { get; set; }
public class Heartbeat
{
    public DateTime Timestamp { get; set; }
    public DateTime ServerTime { get; set; }
    public bool IsConnected { get; set; }
    public string Status { get; set; }
}   public string Status { get; set; }
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

### ServerStatus Flow
1. `SignalRManager.StartHeartbeatTimer()` is called during setup
2. Timer fires every 2 seconds (hardcoded for reliability)
3. `SendHeartbeat()` retrieves current CNC connection status and position
4. ServerStatus message is formatted and broadcast to all SignalR clients in the `CNCClients` group using `ReceiveCNCMessage` with `EventType = "ServerStatus"`
5. All connected clients receive the status update with position data
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

### Heartbeat Flow
1. `SignalRManager.StartHeartbeatTimer()` is called during setup
2. Timer fires every 2 seconds (hardcoded for reliability)
3. `SendHeartbeat()` retrieves current CNC connection status
4. Heartbeat message is formatted and broadcast to all SignalR clients in the `CNCClients` group using the dedicated `"Heartbeat"` method
5. All connected clients receive the `Heartbeat` event as a simple ping