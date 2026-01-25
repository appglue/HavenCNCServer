# Home and Limit Switch Assignment Storage

## Overview
This document details how home and limit switch input assignments are stored in the Centroid CNC Wizard system. Unlike axis motion parameters (steps/rev, max rate, etc.), home and limit switch assignments are configured from two sources:

1. **PLC Definitions** - Which physical inputs are assigned to Home/Limit/HomeLimit functions
2. **Axis Homing Direction** - Which direction each axis homes in (Plus, Minus, or InPlace)

These are combined and sent to CNC12 via the `SetLimit()` and `SetHomeLimit()` API calls.

## Data Sources

### PLC Definitions (Input Assignments)
Users assign physical inputs to functions in the PLC editor:
- `Input 3 → X- HomeLimit` (serves as both home and limit)
- `Input 4 → X+ Limit`
- `Input 5 → Y- Limit`
- `Input 6 → Y+ HomeLimit`

Stored in: `Plc.Definitions.Selected` collection

### Axis Homing Direction
Users configure which direction each axis homes on the Homing page:
- X-axis homes in MINUS direction
- Y-axis homes in PLUS direction
- Z-axis homes in MINUS direction

Stored in: `Axes[n].HomingDirection` property

## Processing Input Assignments (UpdateHomeLimitTable)

The `UpdateHomeLimitTable()` method combines PLC definitions with axis homing directions to prepare data for the API calls.

From [Data.cs lines 426-520](c:\centriodwizard\Data.cs#L426-L520):

```csharp
public void UpdateHomeLimitTable()
{
    ClearHomeLimitTable();
    bool[] homeSet = new bool[] { false, false, false, false, false, false };
    
    // Get all boundary inputs (Home, Limit, HomeLimit functions) FROM PLC DEFINITIONS
    var boundaries = Plc.Definitions.Selected
        .Where(definition => definition.Function is Plc.Function.Boundary)
        .ToList();
    
    foreach (var boundary in boundaries)
    {
        var currentFunction = (Plc.Function.Boundary)boundary.Function;
        int directionOffset = (int)currentFunction.Direction;  // 0=Minus, 1=Plus
        int axisNum = currentFunction.Axis - 1;  // Convert to 0-based index
        int inputNumber = boundary.IONumber;  // The physical input number
        
        bool isHomeFunction = currentFunction.GetType() == typeof(Plc.Function.Home);
        bool isHomeLimitFunction = currentFunction.GetType() == typeof(Plc.Function.HomeLimit);
        bool isLimitFunction = currentFunction.GetType() == typeof(Plc.Function.Limit);
        
        if (isHomeFunction || isHomeLimitFunction)
        {
            // IMPORTANT: Use the axis's homing direction FROM AXIS PROPERTIES,
            // not the direction from the PLC definition
            directionOffset = (int)Axes[axisNum].HomingDirection;
            
            if (directionOffset == (int)Direction.InPlace)
            {
                // Axis doesn't home - clear home inputs
                HomeLimitTable[axisNum, 2] = 0;
                HomeLimitTable[axisNum, 3] = 0;
            }
            else
            {
                // Store home input in internal table
                HomeLimitTable[axisNum, directionOffset + 2] = inputNumber;
                homeSet[axisNum] = true;
            }
        }
        
        if (isLimitFunction || isHomeLimitFunction)
        {
            // Store limit input in internal table
            HomeLimitTable[axisNum, directionOffset] = inputNumber;
        }
    }
}
```

**Key Points:**
1. **Input assignments come from PLC definitions** - user assigns inputs in the PLC editor
2. **Homing direction comes from Axis properties** - set on the Homing page
3. The function type (Home, Limit, HomeLimit) determines how it's categorized
4. For home switches, the axis's homing direction overrides the PLC definition's direction
5. The method builds an internal lookup table to simplify the API calls

## Writing to CNC12 (SaveAxisData)

The save process combines PLC input assignments with axis homing directions, then writes them to CNC12.

From [Data.cs lines 1050-1058](c:\centriodwizard\Data.cs#L1050-L1058):

```csharp
private void SaveAxisData()
{
    // First, process PLC definitions and axis homing directions
    if (!MainWindow.mainWindow.IsServo)
    {
        UpdateHomeLimitTable();
    }
    
    foreach (var axis in Axes)
    {
        // Map axis number to CNCPipe enum
        CNCPipe.Axes skinningAxis = CNCPipe.Axes.AXIS_1; // (set in switch statement)
        
        // Get the input numbers for this axis's limit and home switches
        int minusLimit = /* from processed data */;
        int plusLimit = /* from processed data */;
        int minusHomeLimit = /* from processed data */;
        int plusHomeLimit = /* from processed data */;
        
        // Write to CNC12 via Skinning API
        MainWindow.skin.axis.SetLimit(skinningAxis, CNCPipe.Axis.Direction.MINUS, minusLimit);
        MainWindow.skin.axis.SetLimit(skinningAxis, CNCPipe.Axis.Direction.PLUS, plusLimit);
        MainWindow.skin.axis.SetHomeLimit(skinningAxis, CNCPipe.Axis.Direction.MINUS, minusHomeLimit);
        MainWindow.skin.axis.SetHomeLimit(skinningAxis, CNCPipe.Axis.Direction.PLUS, plusHomeLimit);
        
        // Continue with other axis properties...
        axis.Save(skinningAxis, axisPropertyParamNum);
    }
}
```

## Storage Flow

```
┌─────────────────────────────────────────────────────────────────────┐
│ 1. USER CONFIGURES TWO THINGS:                                       │
│    A) Assigns inputs in PLC Editor:                                  │
│       Input 3 → X- HomeLimit, Input 4 → X+ Limit                     │
│    B) Sets homing direction on Homing page:                          │
│       X-axis homes in MINUS direction                                │
└──────────────────────────┬──────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────────┐
│ 2. DATA STORED IN TWO LOCATIONS:                                     │
│    A) Plc.Definitions.Selected - Input assignments                   │
│       Each definition: IONumber, Function, Direction, Axis           │
│    B) Axes[n].HomingDirection - Direction each axis homes            │
└──────────────────────────┬──────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────────┐
│ 3. WIZARD SAVE INITIATED                                             │
│    (Data.cs - SaveToCncSoftware() → SaveAxisData())                  │
└──────────────────────────┬──────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────────┐
│ 4. PROCESS COMBINED DATA                                             │
│    (Data.cs - UpdateHomeLimitTable())                                │
│    - Reads input assignments from Plc.Definitions.Selected           │
│    - Reads homing direction from Axes[n].HomingDirection             │
│    - Combines into internal lookup structure                         │
└──────────────────────────┬──────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────────┐
│ 5. WRITE TO CNC12 VIA SKINNING API                                   │
│    For each axis:                                                    │
│    - MainWindow.skin.axis.SetLimit(axis, MINUS, inputNum)            │
│    - MainWindow.skin.axis.SetLimit(axis, PLUS, inputNum)             │
│    - MainWindow.skin.axis.SetHomeLimit(axis, MINUS, inputNum)        │
│    - MainWindow.skin.axis.SetHomeLimit(axis, PLUS, inputNum)         │
└──────────────────────────┬──────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────────┐
│ 6. STORED IN CNC12 INTERNAL TABLES                                   │
│    (NOT in numbered parameters - internal CNC12 storage)             │
└─────────────────────────────────────────────────────────────────────┘
```

## Complete Example Scenario

**User Configuration:**
- Input 3: X- Home/Limit (HomeLimit function)
- Input 4: X+ Limit
- Input 5: Y- Limit  
- Input 6: Y+ Home/Limit (HomeLimit function)
- Input 7: Z- Home/Limit (HomeLimit function)
- Input 8: Z+ Limit
- X-axis homes in MINUS direction
- Y-axis homes in PLUS direction
- Z-axis homes in MINUS direction

**Resulting API Calls:**
```csharp
// X-Axis (AXIS_1)
MainWindow.skin.axis.SetLimit(CNCPipe.Axes.AXIS_1, CNCPipe.Axis.Direction.MINUS, 3);
MainWindow.skin.axis.SetLimit(CNCPipe.Axes.AXIS_1, CNCPipe.Axis.Direction.PLUS, 4);
MainWindow.skin.axis.SetHomeLimit(CNCPipe.Axes.AXIS_1, CNCPipe.Axis.Direction.MINUS, 3);
MainWindow.skin.axis.SetHomeLimit(CNCPipe.Axes.AXIS_1, CNCPipe.Axis.Direction.PLUS, 0);

// Y-Axis (AXIS_2)
MainWindow.skin.axis.SetLimit(CNCPipe.Axes.AXIS_2, CNCPipe.Axis.Direction.MINUS, 5);
MainWindow.skin.axis.SetLimit(CNCPipe.Axes.AXIS_2, CNCPipe.Axis.Direction.PLUS, 6);
MainWindow.skin.axis.SetHomeLimit(CNCPipe.Axes.AXIS_2, CNCPipe.Axis.Direction.MINUS, 0);
MainWindow.skin.axis.SetHomeLimit(CNCPipe.Axes.AXIS_2, CNCPipe.Axis.Direction.PLUS, 6);

// Z-Axis (AXIS_3)
MainWindow.skin.axis.SetLimit(CNCPipe.Axes.AXIS_3, CNCPipe.Axis.Direction.MINUS, 7);
MainWindow.skin.axis.SetLimit(CNCPipe.Axes.AXIS_3, CNCPipe.Axis.Direction.PLUS, 8);
MainWindow.skin.axis.SetHomeLimit(CNCPipe.Axes.AXIS_3, CNCPipe.Axis.Direction.MINUS, 7);
MainWindow.skin.axis.SetHomeLimit(CNCPipe.Axes.AXIS_3, CNCPipe.Axis.Direction.PLUS, 0);
```

## Special Cases

### 1. HomeLimit Function (Dual Purpose Input)
When an input is assigned as "HomeLimit", it serves both as a limit switch AND a home switch:
```csharp
if (isHomeLimitFunction)
{
    // Input serves BOTH as limit switch AND home switch
    // Results in both SetLimit() and SetHomeLimit() calls with same input number
}
```

### 2. HomeAll Input
A single input can be used to home all axes:
```csharp
if (currentFunction.Name == "HomeAll")
{
    // Single input assigned as home switch for all configured axes
    for (int j = 0; j < Axes.Count; j++)
    {
        if (Axes[j].Label != 'N' && Axes[j].HomingDirection != Direction.InPlace)
        {
            // This axis will use the same input for homing
        }
    }
}
```

### 3. Memory-Based Limits (Ignore Limits During Homing)
When "IgnoreLimitSwitchesDuringHoming" is enabled:
```csharp
if (MainWindow.wizardSettings.IgnoreLimitSwitchesDuringHoming)
{
    // Use memory bit instead of physical input for limits
    // Allows axis to move past limit switches during homing sequence
    int memNum = 71000 + (2 * axisNum + 1) + directionOffset;
}
```

## Important Notes

1. **Input Numbers are 1-based**: Physical inputs are numbered 1-96 (1-16 on Acorn, 17-32 on first Ether1616, etc.)
2. **Zero Means "Not Assigned"**: Passing 0 to SetLimit/SetHomeLimit means no input is assigned
3. **Processing Order**: PLC definitions and axis homing directions must be processed before making API calls
4. **Not Stored in Parameters**: Unlike other axis config, home/limit assignments are NOT stored in numbered parameters - they're stored in internal CNC12 tables
5. **Separate from Limit Override**: The SetLimit/SetHomeLimit calls are different from the limit override parameters (P963, P969-973)
6. **Rebuilt Every Save**: Input assignments are re-processed from PLC definitions every time the wizard saves
7. **Input Active/Inactive State**: The wizard configures which inputs are assigned and their polarity (NO/NC), but it does NOT read the real-time active/inactive state - that's handled by CNC12's runtime control logic
8. **Polarity vs State**: `SetInputInversionState()` sets the expected wiring type (NO/NC), not the current triggered state

## API Methods

### SetLimit
```csharp
MainWindow.skin.axis.SetLimit(
    CNCPipe.Axes axis,           // AXIS_1 through AXIS_6
    CNCPipe.Axis.Direction dir,  // PLUS or MINUS
    int inputNumber              // Physical input number (1-96) or 0 for none
);
```
Assigns a physical input to act as a limit switch for the specified axis and direction.

### SetHomeLimit
```csharp
MainWindow.skin.axis.SetHomeLimit(
    CNCPipe.Axes axis,           // AXIS_1 through AXIS_6
    CNCPipe.Axis.Direction dir,  // PLUS or MINUS
    int inputNumber              // Physical input number (1-96) or 0 for none
);
```
Assigns a physical input to act as a home switch for the specified axis and direction.

### SetInputInversionState
```csharp
MainWindow.skin.plc.SetInputInversionState(
    int inputNumber,                        // Physical input number (1-96)
    CNCPipe.Plc.InversionState state        // Inverted or NotInverted
);
```
Sets whether an input's logic is inverted (Normally Open vs Normally Closed).

**Input States:**
- `CNCPipe.Plc.InversionState.NotInverted` - Normally Closed (reads 1 when physically closed, 0 when open)
- `CNCPipe.Plc.InversionState.Inverted` - Normally Open (reads 1 when physically open, 0 when closed)

**Note:** This configures the **expected state** of the input (NO vs NC), not the current real-time active/inactive state. The real-time state is read by the CNC12 control based on physical voltage levels at the input terminals.

## Debugging Home/Limit Switch Issues

1. **Verify PLC Definitions**: Check that inputs are correctly assigned to Home/Limit/HomeLimit functions in the PLC editor
2. **Check Processing Logic**: Add logging in `UpdateHomeLimitTable()` to see how PLC definitions are being processed
3. **Verify Homing Direction**: Ensure `Axes[n].HomingDirection` matches the expected direction (Plus, Minus, or InPlace)
4. **Check API Calls**: Log the values passed to `SetLimit()` and `SetHomeLimit()` to verify correct input numbers
5. **Test Input Numbers**: Verify physical inputs are numbered correctly (count from Acorn input 1, then Ether1616 boards)
6. **Check Input Polarity**: Verify `SetInputInversionState()` matches your switch wiring (NO vs NC)
7. **Monitor Real-Time State**: Use CNC12's diagnostics screen to watch input states change when switches are triggered

## Comparison to Axis Motion Parameters

| Aspect | Motion Parameters | Home/Limit Switches |
|--------|------------------|---------------------|
| **Storage Method** | Individual properties | Combined from PLC + Axis settings |
| **Source** | UI text inputs | PLC definitions + Homing direction |
| **API Calls** | SetCountsPerTurn, SetRate, etc. | SetLimit, SetHomeLimit |
| **CNC12 Storage** | Numbered parameters (P91, P968, etc.) | Internal tables (not parameters) |
| **Per-Axis** | Yes | Yes |
| **Rebuild Each Save** | No (properties persist) | Yes (re-processed from PLC definitions) |
