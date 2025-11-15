# SignalR OutputStateChanged Event

## Overview
The `OutputStateChanged` event is broadcast to all connected SignalR clients whenever an output's state is changed via the API. This allows real-time monitoring of output control operations.

## Event Details

### Event Type
`OutputStateChanged`

### When It's Triggered
- When `POST /api/CNCIO/SetOutputState` is called to force an output on or off
- The event is broadcast immediately after the output state is successfully changed

### Event Data Structure

```typescript
{
  EventType: "OutputStateChanged",
  Timestamp: string,  // UTC ISO 8601 format
  Data: {
    OutputNumber: number,      // The output number (e.g., 1-16)
    State: boolean,            // true = on, false = off
    ForceState: string,        // "ForcedOn", "ForcedOff", or "NotForced"
    IsForced: boolean         // true if output is forced (not controlled by PLC)
  }
}
```

### Example Event Payload

```json
{
  "EventType": "OutputStateChanged",
  "Timestamp": "2025-11-14T10:30:45.123Z",
  "Data": {
    "OutputNumber": 5,
    "State": true,
    "ForceState": "ForcedOn",
    "IsForced": true
  }
}
```

## Related API Endpoints

### Get Forced Outputs
**GET** `/api/CNCIO/GetForcedOutputs`

Returns a dictionary of all currently forced outputs with their force states. The server maintains state tracking of forced outputs, starting with an empty state (all outputs unforced) and updating as outputs are forced or reset.

**Response:**
```json
{
  "1": "ForcedOn",
  "5": "ForcedOn",
  "8": "ForcedOff"
}
```

**Note:** The tracking state is maintained in server memory. If the server restarts, all outputs are assumed to be unforced (which matches the behavior of the `ResetAllOutputs` call on CNC connection startup).

### Set Output State
**POST** `/api/CNCIO/SetOutputState`

Forces an output to a specific state and broadcasts the change.

**Request Body:**
```json
{
  "Number": 5,
  "Value": true
}
```

## Client-Side Integration

### JavaScript/TypeScript Example

```javascript
// Connect to SignalR hub
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/cncmessage")
    .build();

// Listen for output state changes
connection.on("ReceiveCNCMessage", (message) => {
    if (message.EventType === "OutputStateChanged") {
        const { OutputNumber, State, ForceState, IsForced } = message.Data;
        
        console.log(`Output ${OutputNumber} changed to ${State ? "ON" : "OFF"}`);
        console.log(`Force State: ${ForceState}`);
        console.log(`Is Forced: ${IsForced}`);
        
        // Update UI accordingly
        updateOutputIndicator(OutputNumber, State, IsForced);
    }
});

// Start connection
await connection.start();

// Join the CNCClients group to receive events
await connection.invoke("JoinGroup", "CNCClients");
```

### React Example

```typescript
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { useEffect, useState } from 'react';

interface OutputStateChangedData {
  OutputNumber: number;
  State: boolean;
  ForceState: string;
  IsForced: boolean;
}

function OutputMonitor() {
  const [forcedOutputs, setForcedOutputs] = useState<Map<number, boolean>>(new Map());

  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/cncmessage')
      .build();

    connection.on('ReceiveCNCMessage', (message) => {
      if (message.EventType === 'OutputStateChanged') {
        const data: OutputStateChangedData = message.Data;
        
        setForcedOutputs(prev => {
          const updated = new Map(prev);
          if (data.IsForced) {
            updated.set(data.OutputNumber, data.State);
          } else {
            updated.delete(data.OutputNumber);
          }
          return updated;
        });
      }
    });

    connection.start()
      .then(() => connection.invoke('JoinGroup', 'CNCClients'))
      .catch(err => console.error('SignalR connection error:', err));

    return () => {
      connection.stop();
    };
  }, []);

  return (
    <div>
      <h3>Forced Outputs</h3>
      {Array.from(forcedOutputs.entries()).map(([num, state]) => (
        <div key={num}>
          Output {num}: {state ? 'ON' : 'OFF'}
        </div>
      ))}
    </div>
  );
}
```

## Use Cases

1. **Real-time Dashboard Updates**: Display which outputs are currently forced and their states
2. **Multi-user Coordination**: Alert all users when someone changes an output state
3. **Audit Logging**: Track when and how outputs are manually controlled
4. **Safety Monitoring**: Notify operators when critical outputs are forced
5. **Debugging**: Monitor output state changes during troubleshooting

## Force States Explained

- **`NotForced`**: Output is controlled by the PLC program logic (normal operation)
- **`ForcedOn`**: Output is forced to ON state, overriding PLC control
- **`ForcedOff`**: Output is forced to OFF state, overriding PLC control

## Notes

- The event is only broadcast when the API successfully changes the output state
- If the force operation fails, no event is broadcast
- The `IsForced` field is a convenience boolean derived from `ForceState != "NotForced"`
- Clients should handle connection drops gracefully and can re-query forced outputs on reconnect using the `GetForcedOutputs` endpoint
- The server maintains state tracking of forced outputs in memory, starting with all outputs in an unforced state
- On server restart, the forced outputs state is reset (empty), which is consistent with the `ResetAllOutputs` call that happens on CNC connection startup
- Use `GetForcedOutputs` API to get the current state of all forced outputs on connection/reconnection
