# REMOVED: Get Travel Limits API Endpoint

## Date Removed
December 18, 2025

## Reason for Removal
The GET endpoint for retrieving travel limits has been removed. Travel limits can still be CONFIGURED via axis configuration, but cannot be queried via API. The frontend should no longer request or display travel limits.

---

## What Was Removed

### API Endpoint
**Endpoint:** `GET /api/CNCConfiguration/GetTravelLimits`

**Method Name:** `GetTravelLimits()`

**Location:** `Controllers/CNCConfigurationController.cs` (lines ~407-448)

### Data Models Removed
**Location:** `Models/MachineConfigurationDTOs.cs`

1. **AxisTravelLimits** class - only used for GET response
2. **TravelLimitsResponse** class - only used for GET response

**Note:** Travel limits are still configurable via the `AxisConfiguration` object when setting up axes.

### What Was NOT Removed

- Ability to configure travel limits when setting up axes via `AxisConfiguration` object
- Backend logic for managing travel limits

---

## Response Structure (For Reference)

### TypeScript Interface (NO LONGER AVAILABLE)
```typescript
// DEPRECATED - DO NOT USE
interface TravelLimitsResponse {
  Axes: AxisTravelLimits[];
  Message: string;
}

interface AxisTravelLimits {
  AxisNumber: number;    // Axis number (1-8)
  AxisLabel: string;     // Axis label (X, Y, Z, A, B, C, U, V, W)
  PlusLimit: number;     // Maximum position (plus direction)
  MinusLimit: number;    // Minimum position (minus direction)
}
```

### Example Response (HISTORICAL)
```json
{
  "Axes": [
    {
      "AxisNumber": 1,
      "AxisLabel": "X",
      "PlusLimit": 24.0000,
      "MinusLimit": -1.0000
    },
    {
      "AxisNumber": 2,
      "AxisLabel": "Y",
      "PlusLimit": 16.0000,
      "MinusLimit": -1.0000
    },
    {
      "AxisNumber": 3,
      "AxisLabel": "Z",
      "PlusLimit": 1.0000,
      "MinusLimit": -8.0000
    }
  ],
  "Message": "Retrieved travel limits for 3 configured axes"
}
```

---

## Frontend Changes Required

### 1. Remove API Calls
Remove any code that calls this endpoint:
```typescript
// REMOVE THIS CODE
fetch('/api/CNCConfiguration/GetTravelLimits')
  .then(res => res.json())
  .then(data => { /* ... */ });
```

### 2. Remove Type Definitions (Frontend Only)
Remove these TypeScript interfaces if they exist in your frontend code:
```typescript
// REMOVE THESE TYPES FROM FRONTEND
interface TravelLimitsResponse { /* ... */ }
interface AxisTravelLimits { /* ... */ }
```

**Note:** The backend DTOs have also been removed since they were only used for the GET response.

### 3. Remove UI Components
Remove any UI components that display travel limits:
- Travel limits tables/displays
- Travel range calculations
- Limit-based validation UI
- Any visualizations showing work envelope based on limits

### 4. Remove State Management
Remove travel limits from state stores (Redux/Context/etc.):
```typescript
// REMOVE FROM STATE
interface MachineState {
  // travelLimits?: TravelLimitsResponse;  // REMOVE THIS
}
```

### 5. Update Validation Logic
If you were using travel limits for position validation, remove or update that logic:
```typescript
// REMOVE OR UPDATE THIS PATTERN
function isPositionValid(axis: string, position: number): boolean {
  // const limit = travelLimits.Axes.find(a => a.AxisLabel === axis);  // REMOVE
  // return position >= limit.MinusLimit && position <= limit.PlusLimit;  // REMOVE
  return true; // Or implement alternative validation
}
```

---

## SignalR Events

**Status:** No SignalR events were broadcasting travel limits. No SignalR changes needed.

---

## Alternative Approaches

If travel limit information is still needed in the frontend, consider:

1. **Hard-coded Configuration:** If limits are static per machine type
2. **Configuration File:** Store in frontend config
3. **Different API:** If this functionality is reimplemented differently in the future
4. **Machine Parameters:** Get limits as part of broader machine configuration

---

## Search Patterns for Frontend Cleanup

Use these patterns to find code that needs updating:

1. **API Calls:**
   ```regex
   GetTravelLimits|/api/CNCConfiguration/GetTravelLimits
   ```

2. **Type Definitions:**
   ```regex
   TravelLimitsResponse|AxisTravelLimits
   ```

3. **State/Props:**
   ```regex
   travelLimits|travel[_-]?limits
   ```

4. **Variables:**
   ```regex
   (plus|minus)Limit|PlusLimit|MinusLimit
   ```

---

## Migration Checklist

Frontend teams should complete the following:

- [ ] Remove API endpoint calls to `GetTravelLimits`
- [ ] Remove TypeScript type definitions for `TravelLimitsResponse` and `AxisTravelLimits`
- [ ] Remove UI components displaying travel limits
- [ ] Remove travel limits from state management (Redux/Context/Zustand/etc.)
- [ ] Remove or update position validation logic that relied on travel limits
- [ ] Remove travel limits from mock data/test fixtures
- [ ] Update any documentation referencing travel limits
- [ ] Test that application works without travel limits data
- [ ] Remove any cached travel limits data

---

## Contact

If you have questions about this removal or need alternative solutions, please reach out to the backend team.
