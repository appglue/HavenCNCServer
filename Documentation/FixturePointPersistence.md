# Fixture Point Persistence Feature

## Overview
The system now automatically remembers and restores the last fixture point when the CNC connection is established or re-established.

## Implementation Details

### In-Memory Storage
- The last fixture point is stored as a static field in `CNCMovementController`
- Persists across API calls but resets when the application restarts
- Stored in machine coordinates (X, Y, Z, A)

### Key Components

#### 1. CNCMovementController
**Location**: `Controllers/CNCMovementController.cs`

**New Static Properties**:
- `_lastFixturePoint`: Private static field storing the last set fixture point
- `LastFixturePoint`: Public static property to read the last fixture point

**New Methods**:
- `RestoreLastFixturePointAsync()`: Public static method that restores the last fixture point if CNC is connected
- `GetLastFixturePoint()`: GET endpoint to retrieve the last fixture point

**Updated Methods**:
- `SetFixturePoint()`: Now saves the fixture point to `_lastFixturePoint` after successfully setting it

#### 2. CNCConnectionManager
**Location**: `Centriod/CNCConnectionManager.cs`

**Updated Logic**:
- After successful connection and test, automatically calls `RestoreLastFixturePointAsync()`
- Uses reflection to avoid circular dependency between layers
- Includes 500ms delay to allow Centroid to stabilize before restoration

**New Method**:
- `RestoreLastFixturePointAsync()`: Private static method that uses reflection to call the controller's restore method

#### 3. RunGCode Methods
**Location**: `Controllers/CNCProgramController.cs`

**New Parameters** (added to both `RunGCode` and `RunGCodeCommand`):
- `fixturePointX`: Optional X coordinate for fixture point
- `fixturePointY`: Optional Y coordinate for fixture point
- `fixturePointZ`: Optional Z coordinate for fixture point
- `fixturePointA`: Optional A coordinate for fixture point

**Behavior**:
- If any fixture point coordinates are provided, creates a `MachinePoint` and sets it before executing G-code
- Fixture point setting happens BEFORE the G-code job is created
- Logs fixture point setting operation for debugging

## API Endpoints

### Get Last Fixture Point
```http
GET /api/CNCMovement/GetLastFixturePoint
```

**Response**:
```json
{
  "x": 10.0,
  "y": 20.0,
  "z": 5.0,
  "a": 0.0
}
```
Returns `null` if no fixture point has been set.

### Run G-Code with Fixture Point
```http
POST /api/CNCProgram/RunGCode?fixturePointX=10.0&fixturePointY=20.0&fixturePointZ=5.0
Content-Type: application/json

["G0 X10 Y10", "G1 Z-5 F100"]
```

### Run Single Command with Fixture Point
```http
POST /api/CNCProgram/RunGCodeCommand?fixturePointX=10.0&fixturePointY=20.0
Content-Type: application/json

"G0 X10 Y10"
```

## Workflow

### Initial Setup
1. User sets fixture point via `SetFixturePoint` endpoint
2. System saves it to `_lastFixturePoint` static field
3. Fixture coordinates are stored in memory

### On Reconnection
1. CNC connection is established (manual connect or auto-connect)
2. `CNCConnectionManager` tests the connection
3. After successful test, waits 500ms for Centroid to stabilize
4. Calls `RestoreLastFixturePointAsync()` via reflection
5. Controller checks if `_lastFixturePoint` is not null
6. If fixture point exists and CNC is connected, automatically restores it
7. Logs restoration success/failure

### Running G-Code with Fixture Point
1. User calls `RunGCode` or `RunGCodeCommand` with optional fixture point parameters
2. If any coordinate is provided, system creates a `MachinePoint`
3. Calls `SetFixturePoint` before creating the G-code job
4. Fixture point is saved to memory (normal SetFixturePoint behavior)
5. G-code job is created and executed with the new fixture point active

## Benefits

1. **Automatic Recovery**: After connection loss and reconnection, fixture point is automatically restored
2. **No Manual Re-setup**: Operators don't need to manually re-zero after reconnection
3. **Integrated Workflow**: G-code can be run with automatic fixture point setup in a single API call
4. **Transparency**: All fixture operations are logged for debugging and audit trail
5. **Separation of Concerns**: Logic lives in the controller layer, not the UI

## Logging

All fixture point operations are logged with emoji prefixes for easy identification:
- 📍 Fixture point operations
- 🔄 Fixture point restoration
- ✓ Success messages
- ❌ Error messages

Example log output:
```
[Fixture] 📍 SetFixturePoint called: X=10.0000, Y=20.0000, Z=5.0000, A=0.0000
[Fixture] ✅ Fixture point set successfully: X=10.0000, Y=20.0000, Z=5.0000, A=0.0000
[Fixture] 🔄 Restoring last fixture point: X=10.0000, Y=20.0000, Z=5.0000, A=0.0000
[Fixture] ✓ Last fixture point restored successfully
```

## Architecture Benefits

### Proper Separation of Concerns
- **Controller Layer**: Handles business logic (fixture point restoration)
- **Connection Manager**: Triggers restoration at the right time
- **UI Layer**: Only displays status messages

### No Circular Dependencies
- Uses reflection to call controller methods from connection manager
- Maintains clean architecture boundaries
- Connection manager doesn't need to reference controller types

### Thread Safety
- Static fixture point field is accessed from both UI and background threads
- Controller instantiation is thread-safe
- Async operations prevent blocking

## Future Enhancements

Potential improvements for the future:
1. **Persistent Storage**: Save fixture point to disk/database for survival across application restarts
2. **Multiple Fixture Points**: Support named fixture presets (e.g., "Station A", "Station B")
3. **Fixture History**: Track last N fixture points for quick recall
4. **Fixture Validation**: Verify fixture point is within machine limits before setting
5. **Automatic Backup**: Periodically save fixture point to prevent data loss
