# CNC UI Controller and Jog Settings API

## Overview

This document describes the new CNC UI Controller, renewal pattern for skin events, and jog settings tracking system added to the HavenCNCServer API.

## New CNCUIController

A new controller has been added at `/api/CNCUI` to handle common UI functions via CNC12 skin events.

### Key Features

1. **Generic Skin Event Methods** - Trigger, start, and stop any skin event by number
2. **Jog Control Methods** - Specific methods for jogging axes in different modes
3. **Renewal Pattern** - Safety mechanism to prevent events from sticking "on"
4. **Cycle Control Methods** - Start/stop/cancel cycle operations
5. **Jog Settings Tracking** - Track and broadcast jog mode state changes

---

## Renewal Pattern for Skin Events

### Problem
Skin events that are "held" (started but not stopped) could stick in the "on" state if the client disconnects or stops calling the stop method.

### Solution
A renewal tracking system with automatic timeout:

- Client calls `StartSkinEvent` with `willRenew=true`
- Server tracks the event and expects renewal every 100ms
- Client must call `RenewSkinEvent` every 100ms to keep the event active
- If no renewal is received within 250ms, the server automatically stops the event
- A background timer runs every 100ms checking for expired events

### API Endpoints

#### Start with Renewal
```http
POST /api/CNCUI/StartSkinEvent/39?willRenew=true
```
Starts holding event 39 (Axis 1 jog plus) with renewal tracking enabled.

#### Renew Event
```http
POST /api/CNCUI/RenewSkinEvent/39
```
Updates the timestamp for event 39, preventing auto-stop. Must be called every 100ms.

#### Stop Event
```http
POST /api/CNCUI/StopSkinEvent/39
```
Explicitly stops event 39 and removes it from renewal tracking.

### Implementation Details

**Backend (CNCUtils.cs):**
```csharp
private static readonly ConcurrentDictionary<int, DateTime> _activeRenewableEvents;
private static readonly System.Threading.Timer _renewalMonitorTimer;

// Timer checks every 100ms
_renewalMonitorTimer = new System.Threading.Timer(MonitorRenewableEvents, null, 100, 100);

// Auto-stops events with no renewal in 250ms
if ((DateTime.UtcNow - lastRenewal).TotalMilliseconds > 250)
{
    StopSkinEvent(eventNumber);
}
```

---

## Jog Settings Tracking System

### Overview
The system now tracks three jog settings and broadcasts them via SignalR whenever they change:

1. **Incremental/Continuous Mode** - Whether jogging in incremental steps or continuous motion
2. **Slow/Fast Speed** - Whether using slow or fast jog speed
3. **Increment Speed Multiplier** - X1, X10, or X100 multiplier for incremental jogging

### State Management

**Location:** `CNCMovementController.cs`

```csharp
private static bool _isIncrementalMode = true;  // Default: incremental
private static bool _isSlowJogMode = true;      // Default: slow
private static JogIncrementSpeed _jogIncrementSpeed = JogIncrementSpeed.X1;  // Default: X1
```

### JogIncrementSpeed Enum

```csharp
public enum JogIncrementSpeed
{
    X1 = 1,
    X10 = 10,
    X100 = 100
}
```

### SignalR Status Broadcast

Every 2 seconds, the server broadcasts a `ServerStatus` event that includes:

```json
{
  "EventType": "ServerStatus",
  "Timestamp": "2025-12-27T10:30:00Z",
  "Data": {
    "IsConnected": true,
    "Position": { "X": 0.0, "Y": 0.0, "Z": 0.0, "A": 0.0 },
    "JogSettings": {
      "IsIncremental": true,
      "IsSlowMode": true,
      "IncrementSpeed": "X1"
    }
  }
}
```

**When jog settings change**, a full `ServerStatus` event is **immediately broadcast** to all connected clients, ensuring the UI stays synchronized.

---

## Jog Settings API Endpoints

### 1. Set Increment Speed (X1/X10/X100)

Changes the jog increment multiplier.

**Endpoint:**
```http
POST /api/CNCUI/TriggerJogIncrementSpeed?speed=X1
POST /api/CNCUI/TriggerJogIncrementSpeed?speed=X10
POST /api/CNCUI/TriggerJogIncrementSpeed?speed=X100
```

**Query Parameter:**
- `speed` (JogIncrementSpeed enum): `X1`, `X10`, or `X100`

**Response:**
```json
{
  "success": true,
  "message": "Jog X10 mode activated"
}
```

**What it does:**
1. Sends skin event 27 (X1), 28 (X10), or 29 (X100) to CNC12
2. Updates `_jogIncrementSpeed` in `CNCMovementController`
3. Broadcasts full `ServerStatus` immediately via SignalR

**Frontend Usage:**
```typescript
// Change to X10 multiplier
await api.apiCNCUITriggerJogIncrementSpeedPost({ speed: JogIncrementSpeed.X10 });

// Listen for status update
hubConnection.on('ReceiveCNCMessage', (message) => {
  if (message.EventType === 'ServerStatus') {
    const jogSettings = message.Data.JogSettings;
    console.log(`Current speed: ${jogSettings.IncrementSpeed}`); // "X10"
  }
});
```

---

### 2. Set Continuous/Incremental Jog Mode

Sets whether jogging is continuous (hold to move) or incremental (fixed steps).

**Endpoint:**
```http
POST /api/CNCUI/SetContinuousJog?isContinuous=true
POST /api/CNCUI/SetContinuousJog?isContinuous=false
```

**Query Parameter:**
- `isContinuous` (bool): `true` for continuous mode, `false` for incremental mode

**Response:**
```json
{
  "success": true,
  "message": "Jog mode set to continuous"
}
```

**What it does:**
1. If `isContinuous=true`: Starts (holds) skin event 26, enabling continuous mode
2. If `isContinuous=false`: Stops skin event 26, returning to incremental mode
3. Updates `_isIncrementalMode` in `CNCMovementController` (inverted: continuous = !incremental)
4. Broadcasts full `ServerStatus` immediately via SignalR

**Frontend Usage:**
```typescript
// Enable continuous jogging
await api.apiCNCUISetContinuousJogPost({ isContinuous: true });

// Return to incremental jogging
await api.apiCNCUISetContinuousJogPost({ isContinuous: false });

// Listen for status update
hubConnection.on('ReceiveCNCMessage', (message) => {
  if (message.EventType === 'ServerStatus') {
    const jogSettings = message.Data.JogSettings;
    console.log(`Mode: ${jogSettings.IsIncremental ? 'Incremental' : 'Continuous'}`);
  }
});
```

**UI Recommendation:**
Create a toggle button that shows "Incremental" or "Continuous" based on `JogSettings.IsIncremental`.

---

### 3. Set Fast/Slow Jog Speed

Sets whether jogging uses slow or fast speed.

**Endpoint:**
```http
POST /api/CNCUI/SetFastJog?isFast=true
POST /api/CNCUI/SetFastJog?isFast=false
```

**Query Parameter:**
- `isFast` (bool): `true` for fast mode, `false` for slow mode

**Response:**
```json
{
  "success": true,
  "message": "Jog speed set to fast"
}
```

**What it does:**
1. If `isFast=true`: Starts (holds) skin event 38, enabling fast mode
2. If `isFast=false`: Stops skin event 38, returning to slow mode
3. Updates `_isSlowJogMode` in `CNCMovementController` (inverted: fast = !slow)
4. Broadcasts full `ServerStatus` immediately via SignalR

**Frontend Usage:**
```typescript
// Enable fast jogging
await api.apiCNCUISetFastJogPost({ isFast: true });

// Return to slow jogging
await api.apiCNCUISetFastJogPost({ isFast: false });

// Listen for status update
hubConnection.on('ReceiveCNCMessage', (message) => {
  if (message.EventType === 'ServerStatus') {
    const jogSettings = message.Data.JogSettings;
    console.log(`Speed: ${jogSettings.IsSlowMode ? 'Slow' : 'Fast'}`);
  }
});
```

**UI Recommendation:**
Create a toggle button that shows "Slow" or "Fast" based on `JogSettings.IsSlowMode`.

---

### 4. Get Current Jog Settings

Retrieves the current jog settings without waiting for the next heartbeat.

**Endpoint:**
```http
GET /api/CNCMovement/GetJogSettings
```

**Response:**
```json
{
  "IsIncremental": true,
  "IsSlowMode": true,
  "IncrementSpeed": "X1"
}
```

**Frontend Usage:**
```typescript
// Get current settings on page load
const settings = await api.apiCNCMovementGetJogSettingsGet();
console.log(`Current settings: ${JSON.stringify(settings)}`);
```

---

## Axis Jog Control Endpoints

The CNCUIController provides methods for jogging each axis in both directions.

### Available Endpoints

**Axis 1 (X):**
- `POST /api/CNCUI/StartJogAxis1Plus?willRenew=true` - Jog X+ (with renewal)
- `POST /api/CNCUI/StartJogAxis1Minus?willRenew=true` - Jog X- (with renewal)
- `POST /api/CNCUI/StopJogAxis1Plus` - Stop X+ jog
- `POST /api/CNCUI/StopJogAxis1Minus` - Stop X- jog

**Axis 2 (Y):**
- `POST /api/CNCUI/StartJogAxis2Plus?willRenew=true` - Jog Y+
- `POST /api/CNCUI/StartJogAxis2Minus?willRenew=true` - Jog Y-
- `POST /api/CNCUI/StopJogAxis2Plus` - Stop Y+ jog
- `POST /api/CNCUI/StopJogAxis2Minus` - Stop Y- jog

**Axis 3 (Z):**
- `POST /api/CNCUI/StartJogAxis3Plus?willRenew=true` - Jog Z+
- `POST /api/CNCUI/StartJogAxis3Minus?willRenew=true` - Jog Z-
- `POST /api/CNCUI/StopJogAxis3Plus` - Stop Z+ jog
- `POST /api/CNCUI/StopJogAxis3Minus` - Stop Z- jog

**Axis 4 (A - Rotary):**
- `POST /api/CNCUI/StartJogAxis4Plus?willRenew=true` - Jog A+
- `POST /api/CNCUI/StartJogAxis4Minus?willRenew=true` - Jog A-
- `POST /api/CNCUI/StopJogAxis4Plus` - Stop A+ jog
- `POST /api/CNCUI/StopJogAxis4Minus` - Stop A- jog

### Frontend Usage Example

**Continuous Jogging with Renewal:**
```typescript
let jogRenewalInterval: NodeJS.Timeout | null = null;

// Start jogging X+ when button pressed
async function onJogXPlusDown() {
  await api.apiCNCUIStartJogAxis1PlusPost({ willRenew: true });
  
  // Renew every 100ms to keep it active
  jogRenewalInterval = setInterval(async () => {
    await api.apiCNCUIRenewSkinEventPost({ eventNumber: 39 });
  }, 100);
}

// Stop jogging X+ when button released
async function onJogXPlusUp() {
  if (jogRenewalInterval) {
    clearInterval(jogRenewalInterval);
    jogRenewalInterval = null;
  }
  await api.apiCNCUIStopJogAxis1PlusPost();
}
```

**Incremental Jogging (No Renewal):**
```typescript
// In incremental mode, just trigger the jog
async function onJogXPlusClick() {
  // Will move one increment and stop automatically
  await api.apiCNCUIStartJogAxis1PlusPost({ willRenew: false });
}
```

---

## Complete Frontend Integration Example

```typescript
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';

class JogController {
  private hubConnection: HubConnection;
  private jogSettings = {
    isIncremental: true,
    isSlowMode: true,
    incrementSpeed: 'X1'
  };

  constructor() {
    // Setup SignalR connection
    this.hubConnection = new HubConnectionBuilder()
      .withUrl('/hubs/cncmessage')
      .build();

    // Listen for jog settings updates
    this.hubConnection.on('ReceiveCNCMessage', (message) => {
      if (message.EventType === 'ServerStatus') {
        this.jogSettings = message.Data.JogSettings;
        this.updateUI();
      }
    });

    this.hubConnection.start();
  }

  // Set increment speed
  async setIncrementSpeed(speed: 'X1' | 'X10' | 'X100') {
    await api.apiCNCUITriggerJogIncrementSpeedPost({ speed });
    // ServerStatus will be broadcast immediately with updated settings
  }

  // Toggle continuous mode
  async setContinuousMode(enabled: boolean) {
    await api.apiCNCUISetContinuousJogPost({ isContinuous: enabled });
    // ServerStatus will be broadcast immediately with updated settings
  }

  // Toggle fast mode
  async setFastMode(enabled: boolean) {
    await api.apiCNCUISetFastJogPost({ isFast: enabled });
    // ServerStatus will be broadcast immediately with updated settings
  }

  // Update UI elements based on current settings
  private updateUI() {
    // Update increment speed buttons
    document.getElementById('x1-btn')?.classList.toggle('active', 
      this.jogSettings.incrementSpeed === 'X1');
    document.getElementById('x10-btn')?.classList.toggle('active', 
      this.jogSettings.incrementSpeed === 'X10');
    document.getElementById('x100-btn')?.classList.toggle('active', 
      this.jogSettings.incrementSpeed === 'X100');

    // Update mode toggles
    document.getElementById('mode-label')?.textContent = 
      this.jogSettings.isIncremental ? 'Incremental' : 'Continuous';
    document.getElementById('speed-label')?.textContent = 
      this.jogSettings.isSlowMode ? 'Slow' : 'Fast';
  }
}
```

---

## Summary

### Key Changes

1. **New CNCUIController** - Centralized UI control via skin events
2. **Renewal Pattern** - Prevents stuck events with 100ms renewal requirement and 250ms timeout
3. **Jog Settings Tracking** - Three tracked settings broadcast via SignalR:
   - Incremental/Continuous mode
   - Slow/Fast speed
   - X1/X10/X100 multiplier
4. **Immediate Broadcast** - Any jog setting change triggers immediate ServerStatus event
5. **State Synchronization** - Frontend always has current jog settings via SignalR heartbeat

### Migration from Old API

**Old (if it existed):**
```typescript
// Hypothetical old toggle approach
await api.toggleIncrementalMode(); // State unknown after call
```

**New:**
```typescript
// Explicit state setting
await api.apiCNCUISetContinuousJogPost({ isContinuous: false }); // Incremental
await api.apiCNCUISetContinuousJogPost({ isContinuous: true });  // Continuous

// Listen for confirmation
hubConnection.on('ReceiveCNCMessage', (message) => {
  if (message.EventType === 'ServerStatus') {
    const { IsIncremental } = message.Data.JogSettings;
    console.log(`Confirmed: ${IsIncremental ? 'Incremental' : 'Continuous'}`);
  }
});
```

### Best Practices

1. **Always use willRenew=true for continuous jogging** - Prevents stuck jog if client crashes
2. **Clear renewal intervals on unmount** - Prevent memory leaks in React/Vue components
3. **Listen to ServerStatus events** - Don't assume state after API call, wait for confirmation
4. **Initialize from GetJogSettings** - Get current state on page load before listening to updates
5. **Use explicit set methods** - SetContinuousJog and SetFastJog provide clear intent

---

## Additional Configuration

### Base Increment Values

The base increment values (multiplied by X1/X10/X100) can be configured via the `GlobalSystemConfiguration`:

```json
{
  "globalSystem": {
    "linearJogIncrement": 0.001,
    "rotaryJogIncrement": 0.1
  }
}
```

This sets:
- **Parameter 40** (BASIC_JOG_INCREMENT_PARM): 0.001 inches for linear axes
- **Parameter 41** (ROTARY_JOG_INCREMENT_PARM): 0.1 degrees for rotary axis

These are typically set during machine configuration and rarely changed during normal operation.
