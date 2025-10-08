# PLC File Format and I/O Writing Guide

## Hardware Restart Requirements

Understanding when hardware restarts are required is critical for implementing configuration changes in a Centroid CNC wizard system. The restart requirement depends on whether changes modify the PLC file versus parameter-only changes.

### Changes Requiring Hardware Restart

**PLC File Modifications (Require Restart):**
- **Input/Output Assignments**: Adding, removing, or changing I/O pin assignments
- **PWM Output Assignments**: Assigning PWM functions to different output pins
- **Custom Function Assignments**: Adding or modifying custom PLC functions

**Examples of PLC-modifying changes:**
```plc
# Changes to wizard regions require restart:
; #wizardregion Inputs
EStopOk         IS INP8     # Changing pin assignment: INP8 → INP9
XHomeLS         IS INP1     # Adding new input assignment
; #endregion

; #wizardregion Outputs  
PWMOutput       IS OUT2     # Adding PWM function assignment
; #endregion
```

**Restart Process:**
1. PLC file is modified and compiled
2. Wizard displays warning: "PLC Configuration has changed"
3. User must shut down Wizard, CNC12, and hardware (Acorn/Hickory)
4. Power cycle the control board
5. Restart CNC12 software

### Changes NOT Requiring Hardware Restart

**Parameter-Only Modifications (No Restart):**
- **Axis Configuration**: Encoder counts, speed ranges, gear ratios
- **Spindle Settings**: Speed limits, encoder parameters, rigid tapping settings
- **PWM Parameters**: Frequency, velocity scaling, floor values (when output assignment unchanged)
- **Motion Parameters**: Acceleration, velocity, backlash compensation
- **Tool Changer Settings**: Tool positions, timing parameters
- **Probe Settings**: Touch plate dimensions, probing speeds
- **Input Inversion Changes**: Modifying normally open/closed states via parameters

**Examples of parameter-only changes:**
```pseudocode
# These API calls/parameter changes do not require restart:
SetCountsPerTurn(axis, counts)                    # Parameter 34, 39, 45, etc.
SetHighRangeSpindleSpeed(MAX/MIN, value)         # API call
SetParameterValue(SPINDLE_ENCODER_COUNTS, 400)   # Parameter 34
SetParameterValue(PWM_FREQUENCY, 1221)           # Parameter 814 (if output exists)
SetParameterValue(PWM_FLOOR, 15)                 # Parameter 817
```

### Determining Restart Requirements

**Use this decision tree:**

1. **Are you modifying wizard regions in the PLC file?** 
   - YES → Restart required
   - NO → Continue to step 2

2. **Are you changing I/O pin assignments?**
   - YES → Restart required  
   - NO → Continue to step 3

3. **Are you only setting parameter values or making API calls?**
   - YES → No restart required
   - NO → Restart likely required

### Implementation Guidelines

**For PLC Changes:**
```pseudocode
1. Modify PLC file wizard regions
2. Compile PLC program
3. Display restart warning to user
4. Provide restart instructions
5. Save configuration
```

**For Parameter Changes:**
```pseudocode
1. Call SetParameterValue() or API functions
2. Changes take effect immediately
3. No restart warning needed
4. Continue normal operation
```

**Mixed Changes:**
If your configuration includes both PLC modifications AND parameter changes, the restart requirement applies to the entire operation.

## Overview
This document explains the PLC file format used by Centroid CNC systems and how to programmatically write input/output definitions to these files. This guide focuses on the actual file structure and transformation process rather than specific implementation details.

## PLC File Structure

### Basic File Format
PLC files (typically with `.src` extension) are text files containing ladder logic definitions and I/O assignments. The wizard manages specific sections using special comment markers called "wizard regions."

### Example PLC File Structure
```plc
;==============================================================================
; CENTROID CNC12 PLC PROGRAM
;==============================================================================

; Program configuration and setup code...

;------------------------------------------------------------------------------
;                        INPUT DEFINITIONS  
;               Closed = 1 (green)  Open = 0 (red)
;------------------------------------------------------------------------------

; DO NOT MODIFY
; #wizardregion Inputs
EStopOk         IS INP8
XHomeLS         IS INP1
YHomeLS         IS INP2
ZHomeLS         IS INP3
XPlusLimit      IS INP4
XMinusLimit     IS INP5
YPlusLimit      IS INP6
YMinusLimit     IS INP7
; #endregion

;------------------------------------------------------------------------------
;                        OUTPUT DEFINITIONS
;------------------------------------------------------------------------------

; DO NOT MODIFY  
; #wizardregion Outputs
SpindleEnable   IS OUT1
CoolantFlood    IS OUT2
CoolantMist     IS OUT3
StepperEnable   IS OUT4
; #endregion

; Additional PLC ladder logic code continues...
```

## Wizard Region System

### Region Markers
The wizard uses special comment markers to identify sections it can modify:

- **Start Marker**: `; #wizardregion [RegionName]`
- **End Marker**: `; #endregion`

### Supported Region Types
| Region Name | Purpose | Definition Format |
|-------------|---------|-------------------|
| `Inputs` | Standard I/O board inputs | `FunctionName IS INP[1-64]` |
| `Outputs` | Standard I/O board outputs | `FunctionName IS OUT[1-64]` |
| `MemoryBits` | Internal memory bits | `FunctionName IS MEM[number]` |
| `Configuration` | System configuration | Various formats |
| `Drive` | Drive system info | Comment format |
| `Date` | Timestamp | Comment format |

## I/O Definition Format

### Standard Input/Output Format
```plc
FunctionName    IS INP1
LongerName      IS INP2  
VeryLongName    IS INP3
```

**Format Rules:**
- Function name (left-aligned)
- Minimum 4 spaces between name and definition
- Additional spaces added to align all definitions to the longest name
- `IS INP[number]` or `IS OUT[number]` format
- Numbers typically range 1-64 depending on hardware

### Memory Bit Format
```plc
ProbeTripped    IS MEM1050
ToolChanging    IS MEM1051
```

## Input Inversion System

### Concept
Input inversion allows reversing the logic state of physical inputs:
- **Normal (Non-inverted)**: Input reads 1 when physically closed, 0 when open
- **Inverted**: Input reads 1 when physically open, 0 when closed

### Visual Representation in UI
- **Green Circle**: NormallyClosed (not inverted) - Input reads 1 when closed, 0 when open
- **Red Circle**: NormallyOpen (inverted) - Input reads 1 when open, 0 when closed  
- **Black Circle**: Logic determined by other settings

### Parameter Storage
Inversion states are stored in CNC12 system parameters, not in the PLC file itself:

| Parameter | Inputs Covered | Description |
|-----------|----------------|-------------|
| 911 | Inputs 1-16 | Bit 0 = Input 1, Bit 15 = Input 16 |
| 912 | Inputs 17-32 | Bit 0 = Input 17, Bit 15 = Input 32 |
| 913 | Inputs 33-48 | Bit 0 = Input 33, Bit 15 = Input 48 |
| 914 | Inputs 49-64 | Bit 0 = Input 49, Bit 15 = Input 64 |
| 915 | Inputs 65-80 | Bit 0 = Input 65, Bit 15 = Input 80 |

**Bit Encoding:**
- Bit = 0: Normal (not inverted)
- Bit = 1: Inverted

**Example:**
- Parameter 911 = 5 (binary: 101)
- Input 1: Inverted (bit 0 = 1)
- Input 2: Normal (bit 1 = 0)  
- Input 3: Inverted (bit 2 = 1)
- Inputs 4-16: Normal (bits 3-15 = 0)

## File Modification Process

### 1. Reading Existing File
```
1. Load PLC file as list of text lines
2. Locate wizard regions using regex patterns:
   - Start: "; #wizardregion [RegionName]"  
   - End: "; #endregion"
3. Parse existing definitions within regions
4. Extract inversion states from system parameters
```

### 2. Building New Content
```
1. Collect selected I/O functions from configuration
2. Sort by I/O number for consistent ordering
3. Generate definition lines with proper alignment:
   - Find longest function name
   - Add 4+ spaces between name and definition
   - Align all definitions to same column
4. Create separate content for each region type
```

### 3. Writing Updated File
```
1. For each wizard region:
   a. Find region boundaries in file
   b. Remove old content between markers
   c. Insert new generated content
   d. Preserve region markers
2. Leave all non-wizard content unchanged
3. Write complete file back to disk
```

### Example Transformation

**Before (existing PLC file):**
```plc
; #wizardregion Inputs
EStopOk    IS INP8
HomeLS     IS INP1  
; #endregion
```

**After (updated by wizard):**
```plc
; #wizardregion Inputs
EStopOk         IS INP8
XHomeLS         IS INP1
YHomeLS         IS INP2
ZHomeLS         IS INP3
XPlusLimit      IS INP4
XMinusLimit     IS INP5
; #endregion
```

**Changes Made:**
- Added new input definitions
- Proper alignment applied to all entries
- Sorted by I/O number
- Original EStopOk definition preserved

## Implementation Guidelines

### For Reading PLC Files
1. **File Parsing**: Read as text lines, handle different line endings
2. **Region Detection**: Use regex to find wizard region boundaries
3. **Definition Extraction**: Parse lines matching `Name IS TYPE[Number]` pattern
4. **Error Handling**: Handle missing regions, malformed definitions

### For Writing PLC Files
1. **Preserve Structure**: Only modify content within wizard regions
2. **Maintain Alignment**: Calculate proper spacing for readability
3. **Sort Consistently**: Order by I/O number within each region
4. **Backup Original**: Always backup before modifications
5. **Validate Output**: Ensure generated file compiles correctly

### For Inversion Management
1. **Parameter Access**: Read/write CNC12 parameters 911-915 for standard inputs
2. **Bit Manipulation**: Use bitwise operations for parameter encoding
3. **State Persistence**: Inversion states persist independently of PLC file

### Region Auto-Creation
If wizard regions don't exist in an existing PLC file:
1. Search for existing I/O definitions using patterns like `IS INP[0-9]`
2. Find the range of existing definitions
3. Insert wizard region markers around the existing content
4. Preserve original definitions within the new regions

## Error Scenarios and Handling

### Missing Wizard Regions
- **Issue**: PLC file lacks required wizard region markers
- **Solution**: Auto-create regions around existing I/O definitions

### Conflicting Definitions  
- **Issue**: Manual definitions conflict with wizard-generated ones
- **Solution**: Wizard regions take precedence; manual changes outside regions preserved

### Parameter Access Errors
- **Issue**: Cannot read/write inversion parameters
- **Solution**: Default to non-inverted state, warn user

### File Write Errors
- **Issue**: Cannot write to PLC file (permissions, file lock, etc.)
- **Solution**: Show error message, allow user to retry or save elsewhere

## Best Practices

### File Management
- Always backup original PLC file before modifications
- Validate PLC file compiles after changes
- Use consistent naming conventions for I/O functions
- Document any manual PLC modifications outside wizard regions

### I/O Assignment
- Plan I/O layout before starting (inputs 1-8 for critical functions, etc.)
- Group related functions on consecutive I/O numbers
- Reserve specific ranges for different systems (limits, probes, etc.)
- Document I/O assignments in system documentation

### Inversion Configuration
- Test inversion settings with actual hardware
- Document which inputs require inversion and why
- Use consistent inversion patterns across similar machines
- Verify inversion settings after any hardware changes

This guide provides the practical information needed to implement a similar PLC file management system without relying on the specific codebase structure.

## Axis Configuration System

The wizard also manages comprehensive axis configuration through a combination of API calls and parameter storage. Unlike I/O definitions which are written to the PLC file, axis configuration is stored in the CNC12 system parameters and applied via API calls.

### Axis Configuration Parameters

#### Core Axis Properties
Each axis has fundamental configuration stored via API calls:

| Property | API Method | Description | Units/Range |
|----------|------------|-------------|-------------|
| Steps Per Revolution | `MainWindow.skin.axis.SetCountsPerTurn(axis, value)` | Motor/drive steps per revolution | 1600-8,388,608 |
| Turn Ratio | `MainWindow.skin.axis.SetScrewPitch(axis, value)` | Distance per revolution | Linear: inches/mm per rev<br/>Rotary: degrees per revolution |
| Travel Limits | `MainWindow.skin.axis.SetTravelLimit(axis, direction, value)` | Software travel limits | Plus/Minus directions |
| Backlash Compensation | `MainWindow.skin.axis.SetLashComp(axis, value)` | Backlash compensation amount | Linear units |
| Jog Rates | `MainWindow.skin.axis.SetRate(axis, rateType, value)` | Various jog speeds | Linear/angular units per minute |
| Acceleration | `MainWindow.skin.axis.SetAccelTime(axis, value)` | Acceleration time | Seconds |
| Axis Label | `MainWindow.skin.axis.SetLabel(axis, label)` | Axis identifier | X, Y, Z, A, B, C, U, V, W |
| Axis Reversal | `MainWindow.skin.axis.SetAxisReversal(axis, value)` | Reverse axis direction | Boolean |
| Axis Reversal | `SetAxisReversal()` | Direction reversal | Boolean |

#### Typical Steps Per Revolution Values
- **Standard Systems**: 1600, 2000, 2048, 3200, 4000, 4096, 5000, 6400, 8000, 8192, 10000, 12000, 16000, 16384, 32000, 32768, 1048576
- **Hickory Systems**: 51200, 524288, 4194304, 1048576, 8388608

### Axis Pairing System

#### Axis Pairing Parameters
Axis pairing allows multiple axes to move together with coordinated motion:

| Parameter | Purpose | Values |
|-----------|---------|---------|
| 554 | 4th Axis Master/Slave Pairing | 0=None, 1=X, 2=Y, 3=Z |
| 555 | 5th Axis Master/Slave Pairing | 0=None, 1=X, 2=Y, 3=Z, 4=4th |
| 500 | Acorn Axis Pairing Mode | 0=Disabled, 1=Enabled |
| 964-967 | Acorn Pairing Parameters | Hardware-specific pairing |

#### Pairing Behavior
When axes are paired:
1. **Slave axis inherits master axis properties**:
   - Steps per revolution
   - Turn ratio
   - Backlash compensation
   - Jog rates
   - Travel limits
   - Direction reversal (with optional inversion)

2. **UI controls for slave axis are disabled**

3. **Automatic synchronization** occurs when master axis values change

### Axis Property Parameters

#### Core Parameter Numbers
| Parameter | Axis | Purpose |
|-----------|------|---------|
| 91 | Axis 1 | Axis properties bit field |
| 92 | Axis 2 | Axis properties bit field |
| 93 | Axis 3 | Axis properties bit field |
| 94 | Axis 4 | Axis properties bit field |
| 166 | Axis 5 | Axis properties bit field |
| 167 | Axis 6 | Axis properties bit field |
| 168 | Axis 7 | Axis properties bit field |
| 169 | Axis 8 | Axis properties bit field |

#### Axis Property Bit Fields
The axis property parameters use bit encoding for various settings:

| Bit | Purpose |
|-----|---------|
| 0 | Linear/Rotary (0=Linear, 1=Rotary) |
| 1 | Rotary DRO Display (0=Show Rotations, 1=Wrap Around) |
| 4 | C-Axis Enable |
| 7 | Prevent Divide by 360 for C-Axis |
| 9 | Hide Axis from DRO (ATC Turret) |
| 11 | Parallel to X (Rotary) |
| 12 | Parallel to Y (Rotary) |
| 17 | Display as Rotary (Tangential Knife) |

**Note**: Axis signal inversions (Step, Direction, Enable, Quadrature) are stored in separate parameters (P961) using 4-bit nibbles per axis, not in the axis property parameters.

### Additional Axis Parameters

#### Drive and Control Parameters
| Parameter | Purpose | Range |
|-----------|---------|-------|
| 300-307 | Axis Drive Numbers | 1-8 for each axis |
| 308-315 | Encoder Index Numbers | Encoder assignments |
| 340-347 | Drive Position Mode Delays | Drive-specific delays |
| 357-364 | Maximum Drive RPM | Drive speed limits |
| 968 | Stepper Pulse Rate | Pulse frequency control |

#### Step Frequency Configuration
The system supports multiple step frequencies for different hardware:
- 100,000 steps/second
- 200,000 steps/second  
- 300,000 steps/second
- 400,000 steps/second
- 240,000 steps/second

Stored in parameter 968 as: `PulseStepFrequency / StepFrequency`

### Turn Ratio Calculation

#### Linear Axes
- **Imperial**: Distance in inches per revolution
- **Metric**: Distance in mm per revolution

#### Rotary Axes
- **Imperial**: Uses reciprocal (1/degrees per revolution) for display
- **Metric**: Direct degrees per revolution

#### Example Calculations
```
Linear Axis (Imperial):
- Lead screw: 0.2 inches per revolution
- Turn Ratio = 0.2

Rotary Axis (Imperial):
- Gear reduction: 90:1 (4 degrees per motor revolution)  
- Turn Ratio = 1/4 = 0.25 (stored value)
- Display Value = 4 degrees/rev

Rotary Axis (Metric):
- Direct: 4 degrees per revolution
- Turn Ratio = 4.0
```

### Travel Limits Configuration

#### Software Travel Limits
Travel limits are set via API calls and stored in the system:
- **Plus Direction Limit**: Maximum positive travel
- **Minus Direction Limit**: Maximum negative travel  
- **Units**: Match axis units (inches, mm, degrees)

#### Home Limits vs Travel Limits
- **Home Limits**: Physical switch positions for homing
- **Travel Limits**: Software-enforced motion boundaries
- **Both are configured separately** via different API calls

### Implementation Guidelines for Axis Configuration

#### Reading Current Configuration
```csharp
For each axis:
1. Get core properties via API calls:
   - MainWindow.skin.axis.GetCountsPerTurn(axis, out stepsPerRev)
   - MainWindow.skin.axis.GetScrewPitch(axis, out turnRatio)
   - MainWindow.skin.axis.GetLashComp(axis, out lashComp)
   - MainWindow.skin.axis.GetRate(axis, rateType, out value)
   - MainWindow.skin.axis.GetTravelLimit(axis, direction, out value)
   - MainWindow.skin.axis.GetAccelTime(axis, out accelTime)
   - MainWindow.skin.axis.GetLabel(axis, out label)

2. Get axis properties from parameters 91-94, 166-169
3. Get pairing status from parameters 554-555
4. Parse bit fields for property settings
```

#### Writing New Configuration  
```csharp
For each axis:
1. Set core properties via API calls:
   - MainWindow.skin.axis.SetCountsPerTurn(axis, stepsPerRev)
   - MainWindow.skin.axis.SetScrewPitch(axis, turnRatio)
   - MainWindow.skin.axis.SetLashComp(axis, lashComp)
   - MainWindow.skin.axis.SetTravelLimit(axis, CNCPipe.Axis.Direction.PLUS, plusLimit)
   - MainWindow.skin.axis.SetTravelLimit(axis, CNCPipe.Axis.Direction.MINUS, minusLimit)
   - MainWindow.skin.axis.SetRate(axis, CNCPipe.Axis.Rate.SLOW_JOG, slowJogRate)
   - MainWindow.skin.axis.SetRate(axis, CNCPipe.Axis.Rate.FAST_JOG_PLUS, fastJogPlusRate)
   - MainWindow.skin.axis.SetRate(axis, CNCPipe.Axis.Rate.FAST_JOG_MINUS, fastJogMinusRate)
   - MainWindow.skin.axis.SetRate(axis, CNCPipe.Axis.Rate.HOME_JOG, homingRate)
   - MainWindow.skin.axis.SetRate(axis, CNCPipe.Axis.Rate.MAX, maxRate)
   - MainWindow.skin.axis.SetAccelTime(axis, accelRate)
   - MainWindow.skin.axis.SetLabel(axis, label)
   - MainWindow.skin.axis.SetAxisReversal(axis, axisReversed)
   
2. Update axis property parameters:
   - Build bit field with property settings
   - MainWindow.skin.parameter.SetMachineParameter((int)axisPropertyParamNum, AxisParamValue)
   
3. Set axis pairing (if applicable):
   - MainWindow.skin.parameter.SetMachineParameter((int)pairingParamNum, pairingValue)
```
   - Write to appropriate parameter (91-94, 166-169)
   
3. Handle axis pairing:
   - Set pairing parameters if enabled
   - Copy master axis settings to slave
```

#### Validation Rules
1. **Steps Per Revolution**: 1600 minimum, hardware-dependent maximum
2. **Turn Ratio**: Must be positive, reasonable range for application
3. **Travel Limits**: Plus > Minus, within mechanical limits  
4. **Pairing**: Master axis must be lower-numbered than slave
5. **Units**: Consistent with machine configuration (Imperial/Metric)

#### Common Configuration Patterns

**Basic 3-Axis Mill:**
```
X-Axis: Linear, 4000 steps/rev, 0.2 in/rev, ±12 inch travel
Y-Axis: Linear, 4000 steps/rev, 0.2 in/rev, ±8 inch travel  
Z-Axis: Linear, 4000 steps/rev, 0.2 in/rev, -8 to +2 inch travel
```

**4th Axis Rotary:**
```
A-Axis: Rotary, 4000 steps/rev, 4 deg/rev, ±999999 degree travel
Pairing: None (independent)
```

**Dual X-Axis (Gantry):**
```
X-Axis: Linear, 4000 steps/rev, 0.2 in/rev, ±24 inch travel
U-Axis: Linear, 4000 steps/rev, 0.2 in/rev, ±24 inch travel
Pairing: U paired to X (parameter 554 = 1)
```

This axis configuration system works in parallel with the PLC I/O system to provide complete machine setup through the wizard interface.

## Spindle Configuration System

The wizard manages comprehensive spindle configuration through a combination of API calls and parameter storage. Spindle configuration includes encoder setup, speed ranges, gear ratios, and advanced features like rigid tapping.

### Primary Spindle Configuration

#### Core Spindle Parameters
| Parameter | Purpose | Description | Units/Range |
|-----------|---------|-------------|-------------|
| 34 | Encoder Counts | Encoder pulses per spindle revolution | Counts (e.g., 8000 for 2000 PPR) |
| 78 | Spindle Control | Bit field for encoder enable and options | Bit flags |
| 35 | Spindle Axis | Axis number for spindle encoder | 5 (standard) or 8 (AcornSix/Hickory) |
| 65 | Low Gear Ratio | Pulley ratio for low speed range | Ratio (e.g., 2.5) |
| 66 | Medium Gear Ratio | Pulley ratio for medium speed range | Ratio (e.g., 1.5) |
| 67 | High Gear Ratio | Pulley ratio for high speed range | Ratio (e.g., 1.0) |
| 420 | Analog Output Range | DAC voltage range for spindle control | 0-3 (see voltage ranges) |
| 430 | RTG Spindle Display | Realtime graphics spindle display mode | 0-2 |
| 996 | Spindle OK Delay | Delay before spindle ready signal | Seconds |
| 997 | Cooling Fan Delay | Spindle cooling fan delay timer | Seconds |

#### Spindle Speed Configuration
Spindle speeds are configured via API calls rather than parameters:
- **Maximum Speed**: `MainWindow.skin.state.SetHighRangeSpindleSpeed(CNCPipe.State.Value.MAX, value)`
- **Minimum Speed**: `MainWindow.skin.state.SetHighRangeSpindleSpeed(CNCPipe.State.Value.MIN, value)`
- **Reading Values**: `MainWindow.skin.state.GetHighRangeSpindleSpeed(CNCPipe.State.Value.MAX/MIN, out value)`
- **Storage**: Values stored in `cncmcfg.xml` configuration file

#### Spindle Parameter 78 Bit Field
| Bit | Purpose | Description |
|-----|---------|-------------|
| 0 | Primary Encoder Enable | Enable spindle encoder feedback |
| 1 | Reserved | - |
| 2 | Reserved | - |
| 3 | Second Spindle Encoder | Enable second spindle encoder |
| 4 | Scaling Enable | Enable spindle speed scaling |

#### Analog Output Voltage Ranges (Parameter 420)
| Value | Voltage Range | Description |
|-------|---------------|-------------|
| 0 | 0 to +10VDC | Most common, unipolar |
| 1 | 0 to +5VDC | Lower voltage systems |
| 2 | -5 to +5VDC | Bipolar, allows reverse |
| 3 | -10 to +10VDC | Full bipolar range |

### Second Spindle Configuration

#### Second Spindle Parameters
| Parameter | Purpose | Description |
|-----------|---------|-------------|
| 459 | Second Spindle Enable | Enable second spindle operation |
| 460 | Second Spindle Max Speed | Maximum speed for second spindle |
| 461 | Second Spindle Min Speed | Minimum speed for second spindle |
| 462 | Second Spindle Encoder Counts | Encoder counts per revolution |

### Gear Ratio Configuration

#### Purpose and Calculation
Gear ratios compensate for pulley/belt drive systems between the spindle motor and actual spindle:

```
Gear Ratio = Motor RPM / Spindle RPM

Examples:
- Direct drive: Ratio = 1.0
- 2:1 reduction: Ratio = 2.0 (motor turns twice for each spindle turn)
- 1:2 increase: Ratio = 0.5 (motor turns once for two spindle turns)
```

#### Multi-Range Configuration
Many systems use multiple pulley ratios for different speed ranges:
- **Low Range (P65)**: Heavy cutting, high torque, low speed
- **Medium Range (P66)**: General purpose machining  
- **High Range (P67)**: Light cutting, high speed

### Rigid Tapping Configuration

#### Rigid Tapping Parameters
| Parameter | Purpose | Description |
|-----------|---------|-------------|
| 36 | Rigid Tapping Enable | Enable/disable and configuration bits |
| 240 | Accel/Decel Distance | Linear distance for acceleration |
| 241 | Sync Distance | Rotational degrees for synchronization |
| 68 | Slow Spindle Speed | Minimum RPM for rigid tapping |
| 69 | Slow Spindle Time | Duration at minimum RPM |

#### Rigid Tapping Requirements
1. **Spindle encoder must be enabled** (Parameter 78, bit 0)
2. **Encoder counts properly configured** (Parameter 34)
3. **Spindle axis assignment** (Parameter 35)
4. **Acceleration parameters tuned** for machine dynamics

### Encoder Configuration

#### Encoder Setup Process
1. **Physical Connection**: Encoder connected to specified encoder port
2. **Counts Calculation**: 
   ```
   Total Counts = PPR × 4 (for quadrature)
   Example: 2000 PPR encoder = 8000 counts
   ```
3. **Port Assignment**: 
   - Standard systems: Encoder port 1-3
   - AcornSix/Hickory: Encoder port assignment via Parameter 315

#### Encoder Verification
The wizard provides encoder count verification through the CNC12 PID menu to ensure proper signal reception and counting.

### Advanced Spindle Features

#### Spindle Speed Variation (SSV)
| Parameter | Purpose | Range |
|-----------|---------|-------|
| 982 | SSV Cycle Time | Variation cycle duration |
| 983 | SSV Amount | Speed variation percentage |

#### Real-Time Graphics (RTG)
| RTG Display Value | Description |
|-------------------|-------------|
| 0 | Encoder feedback speed |
| 1 | Programmed G-code speed |
| 2 | Mixed display mode |

#### Gang Tool Configuration
| Parameter | Purpose | Bit Field |
|-----------|---------|-----------|
| 163 | Gang Tool Enable | Bit 0: Enable gang tooling |

### Implementation Guidelines for Spindle Configuration

#### Reading Current Configuration
```pseudocode
1. Read encoder configuration:
   - Get spindle parameter 78 for enable bits
   - Get encoder counts from parameter 34
   - Get spindle axis from parameter 35

2. Read speed configuration:
   - Use MainWindow.skin.state.GetHighRangeSpindleSpeed(MAX/MIN, out value) API calls
   - Get gear ratios from parameters 65-67
   - Get analog range from parameter 420

3. Read advanced features:
   - Get rigid tapping from parameter 36
   - Get RTG display from parameter 430
   - Get second spindle from parameters 459-462
```

#### Writing New Spindle Configuration
```csharp
// Core parameters via CNCUtils.SetParameterValue()
CNCUtils.SetParameterValue(CNC12Parameters.SPINDLE_COUNTS_REV_PARM, encoderCounts);
CNCUtils.SetParameterValue(CNC12Parameters.LOW_GEAR_RATIO_PARM, lowRatio);
CNCUtils.SetParameterValue(CNC12Parameters.MED_LOW_GEAR_RATIO_PARM, mediumRatio);
CNCUtils.SetParameterValue(CNC12Parameters.PLC_ANALOG_PARM, analogRange);
CNCUtils.SetParameterValue(CNC12Parameters.SPINDLE_OK_DELAY_PARM, okDelay);
CNCUtils.SetParameterValue(CNC12Parameters.SPINDLE_COOLING_FAN_DELAY_TIMER, fanDelay);

// Speed configuration via API calls
MainWindow.skin.state.SetHighRangeSpindleSpeed(CNCPipe.State.Value.MAX, maxSpeed);
MainWindow.skin.state.SetHighRangeSpindleSpeed(CNCPipe.State.Value.MIN, minSpeed);

// Spindle parameter 78 bit field management
SpindleParmValue = CNCUtils.ModifyBit((int)SpindleParmValue, 0, encoderEnabled);
SpindleParmValue = CNCUtils.ModifyBit((int)SpindleParmValue, 3, secondSpindleEnabled);
SpindleParmValue = CNCUtils.ModifyBit((int)SpindleParmValue, 4, scalingEnabled);
CNCUtils.SetParameterValue(CNC12Parameters.SPINDLE_PARM, SpindleParmValue);

// Spindle axis assignment (hardware-dependent)
if (IsAcornSix || IsHickory) {
    CNCUtils.SetParameterValue(CNC12Parameters.SPINDLE_AXIS_PARM, 8);
} else {
    CNCUtils.SetParameterValue(CNC12Parameters.SPINDLE_AXIS_PARM, 5);
}

// Rigid tapping configuration (if enabled)
CNCUtils.SetParameterValue(CNC12Parameters.RIGID_TAPPING_PARM, rigidTappingBits);
CNCUtils.SetParameterValue(CNC12Parameters.SPINDLE_DECEL_TIME_PARM, decelTime);
CNCUtils.SetParameterValue(CNC12Parameters.RT_SLOW_SPINDLE_SPEED_PARM, minRpm);
CNCUtils.SetParameterValue(CNC12Parameters.RT_SLOW_SPINDLE_TIME_PARM, minRpmTime);
CNCUtils.SetParameterValue(CNC12Parameters.RT_SPINDLE_CUTOFF_DRIFT_PARM, drift);
CNCUtils.SetParameterValue(CNC12Parameters.THREADING_AND_TAPPING_ACCEL_DECEL_DISTANCE_PARM, accelDistance);
CNCUtils.SetParameterValue(CNC12Parameters.THREADING_AND_TAPPING_ACCEL_DECEL_ROT_DEG_STEP_AMT_PARM, syncDistance);

// Spindle Speed/Feed Rate Variation (SSV/FRV)
CNCUtils.SetParameterValue(CNC12Parameters.SSV_CYCLE_TIME, ssvCycleTime);
CNCUtils.SetParameterValue(CNC12Parameters.SSV_AMOUNT, ssvAmount);
CNCUtils.SetParameterValue(CNC12Parameters.FRV_CYCLE_TIME, frvCycleTime);

// Lathe-specific configuration
CNCUtils.SetParameterValue(CNC12LatheParameters.G98_OR_G99_DEFAULT_PARM, g98Default);
LatheConfigParam = CNCUtils.ModifyBit(LatheConfigParam, 0, toolOrientation);
LatheConfigParam = CNCUtils.ModifyBit(LatheConfigParam, 1, reverseDirection);
LatheConfigParam = CNCUtils.ModifyBit(LatheConfigParam, 2, machineOrientation == 1);
LatheConfigParam = CNCUtils.ModifyBit(LatheConfigParam, 3, machineOrientation);
CNCUtils.SetParameterValue(CNC12Parameters.X_ORIENTATION_PARM, LatheConfigParam);
```

## PWM Configuration System

The wizard configures PWM (Pulse Width Modulation) output for spindle control, laser applications, and VFD (Variable Frequency Drive) control. PWM configuration includes frequency settings, velocity scaling, minimum floor values, and specialized modes for different hardware types.

### PWM Parameters

| Parameter | Purpose | Bit Field/Values |
|-----------|---------|------------------|
| 814 | PWM Frequency | Hz value (76, 610, 1221, 4883 for Acorn) |
| 815 | PWM Options | Bit 0: Inverse Enable, Bit 1: Velocity 100%, Bit 2: Minimum Floor Enable |
| 816 | PWM Velocity | Reserved for velocity scaling |
| 817 | PWM Floor | Minimum PWM value (percentage) |
| 969 | G37 Laser Velocity | Laser cutting velocity parameter |
| 998 | Laser Cooling Fan Delay Timer | Delay before fan shutdown (seconds) |

### Detailed PWM Parameter Configuration

#### PWM Enable/Disable
**Parameter**: 814 (`ACORN_PWM_FREQUENCY_PARM`)
**Control Method**: Frequency value (0 = disabled, non-zero = enabled)

```csharp
// PWM Enable: Set frequency to enable PWM (e.g., 1221 Hz)
cncPipe.parameter.SetMachineParameter(814, 1221);  // Enable PWM at 1221 Hz

// PWM Disable: Set frequency to 0 to disable PWM
cncPipe.parameter.SetMachineParameter(814, 0);     // Disable PWM

// Check if PWM is enabled
cncPipe.parameter.GetMachineParameterValue(814, out double pwmFrequency);
bool pwmEnabled = pwmFrequency != 0;
```

#### PWM S Command Range (0-1000 or 0-100)
**Parameter**: 815 (`ACORN_PWM_OPTIONS_PARM`) - Bit 1
**Values**: 
- Bit 1 = 0: S command range 0-10 (scaled to 0-100%)
- Bit 1 = 1: S command range 0-100 (velocity 100% mode)

```csharp
// Read current PWM options
cncPipe.parameter.GetMachineParameterValue(815, out double pwmOptions);
int pwmParam = (int)pwmOptions;

// Check current range setting
bool velocity100Mode = GeneralUtils.IsBitSet(pwmParam, 1);
string rangeDescription = velocity100Mode ? "0-100 range" : "0-10 range (scaled to 0-100%)";

// Set S command range to 0-100 (velocity 100% mode)
pwmParam = GeneralUtils.ModifyBit(pwmParam, 1, true);
cncPipe.parameter.SetMachineParameter(815, pwmParam);

// Set S command range to 0-10 (scaled to 0-100%)
pwmParam = GeneralUtils.ModifyBit(pwmParam, 1, false);
cncPipe.parameter.SetMachineParameter(815, pwmParam);
```

#### Only Apply Floor During PWM Velocity Modulation Moves
**Parameter**: 815 (`ACORN_PWM_OPTIONS_PARM`) - Bit 2
**Control**: Automatically set based on floor value (enabled when floor > 0)

```csharp
// Read current PWM options
cncPipe.parameter.GetMachineParameterValue(815, out double pwmOptions);
int pwmParam = (int)pwmOptions;

// Check if floor is enabled during velocity modulation
bool floorEnabled = GeneralUtils.IsBitSet(pwmParam, 2);

// The floor enable bit is automatically managed by the system:
// - Set to 1 when PWM floor value (Parameter 817) > 0
// - Set to 0 when PWM floor value (Parameter 817) = 0

// To enable floor during velocity modulation, set a floor value:
cncPipe.parameter.SetMachineParameter(817, 15.0);  // 15% floor value
// System automatically sets bit 2 of parameter 815 to 1

// To disable floor during velocity modulation, set floor to 0:
cncPipe.parameter.SetMachineParameter(817, 0.0);   // No floor
// System automatically sets bit 2 of parameter 815 to 0
```

#### Laser Cooling Fan Delay Timer
**Parameter**: 998 (`LASER_COOLING_FAN_DELAY_TIMER`)
**Units**: Seconds
**Range**: 0+ seconds (0 = immediate shutdown)

```csharp
// Set laser cooling fan delay to 30 seconds
cncPipe.parameter.SetMachineParameter(998, 30.0);

// Set immediate fan shutdown (no delay)
cncPipe.parameter.SetMachineParameter(998, 0.0);

// Read current fan delay setting
cncPipe.parameter.GetMachineParameterValue(998, out double fanDelay);
Console.WriteLine($"Laser cooling fan delay: {fanDelay} seconds");
```

### PWM Options Parameter (815) Bit Field Summary

| Bit | Purpose | Description |
|-----|---------|-------------|
| 0 | Inverse Enable | Invert PWM signal (1 = inverted, 0 = normal) |
| 1 | Velocity 100% Mode | S command range (1 = 0-100, 0 = 0-10 scaled) |
| 2 | Floor Enable | Apply floor during velocity moves (auto-managed) |

### Complete PWM Configuration Example

```csharp
public void ConfigurePWMSettings(CNCPipe cncPipe)
{
    // 1. Enable PWM by setting frequency
    cncPipe.parameter.SetMachineParameter(814, 1221);  // Enable at 1221 Hz
    
    // 2. Configure PWM options
    cncPipe.parameter.GetMachineParameterValue(815, out double pwmOptions);
    int pwmParam = (int)pwmOptions;
    
    // Set S command range to 0-100 (velocity 100% mode)
    pwmParam = GeneralUtils.ModifyBit(pwmParam, 1, true);
    
    // Set inverse enable if needed
    bool inverseEnable = false;  // Set based on requirements
    pwmParam = GeneralUtils.ModifyBit(pwmParam, 0, inverseEnable);
    
    // Write PWM options
    cncPipe.parameter.SetMachineParameter(815, pwmParam);
    
    // 3. Set PWM floor value (automatically enables bit 2)
    cncPipe.parameter.SetMachineParameter(817, 10.0);  // 10% minimum floor
    
    // 4. Set laser cooling fan delay
    cncPipe.parameter.SetMachineParameter(998, 15.0);  // 15 second delay
}
```

### PWM Configuration Types

#### Standard PWM Mode (Default)
- General purpose PWM output
- Configurable frequency
- Velocity range: 0-100% or 0-10%
- Minimum floor setting available

#### J-Tech Laser Mode  
- Specialized for J-Tech laser systems
- PWM Output assigned to output 2
- LaserEnable assigned to output 4  
- LaserReset assigned to output 7
- Cooling fan delay timer configured

#### BLDC Spindle Mode
- Brushless DC motor control
- NoFaultOut assigned to output 1
- Default frequency: 1221 Hz
- Enhanced fault monitoring

#### VFD Mode  
- Variable Frequency Drive control
- PWM frequency matches spindle requirements
- Integrated with spindle speed control
- Compatible with second spindle systems

### PWM Output Assignment

#### PLC File Format
```plc
# wizardregion Outputs
PWMOutput       IS OUT2     # J-Tech/BLDC mode
LaserEnable     IS OUT4     # J-Tech mode only
LaserReset      IS OUT7     # J-Tech mode only  
NoFaultOut      IS OUT1     # BLDC mode only
# endregion
```

#### Hardware-Specific Behavior

**Acorn Systems:**
- Fixed frequencies: 76Hz, 610Hz, 1221Hz, 4883Hz
- PWM Output typically assigned to output 2
- Automatic frequency selection based on mode

**Standard Systems:**  
- Custom frequency input (0-24000 Hz)
- Manual PWM output assignment
- More flexible configuration options

### PWM Configuration Implementation

#### Data Structure
```csharp
public class PWMSetupData : IPageData
{
    public bool isPWMEnabled { set; get; }
    public bool PWMVelocity100 { set; get; }      // true = 0-100%, false = 0-10%
    public bool isinverseEnabled { set; get; }     // Invert PWM signal
    public double PWMFrequency { set; get; }       // Hz value
    public double minimums { get; set; }           // Floor percentage
    public double LaserCoolingFanDelayTimer { get; set; }
}
```

#### Parameter Bit Field Management
```csharp
// PWM Options Parameter (815) bit assignments:
PWMParam = CNCUtils.ModifyBit((int)PWMParam, 0, isinverseEnabled);
PWMParam = CNCUtils.ModifyBit((int)PWMParam, 1, PWMVelocity100);
PWMParam = CNCUtils.ModifyBit((int)PWMParam, 2, minimums > 0);
```

#### Frequency Assignment Logic
```csharp
// Acorn frequency mapping
switch (frequencyComboBoxIndex) {
    case 0: PWMFrequency = 76; break;
    case 1: PWMFrequency = 610; break;  
    case 2: PWMFrequency = 1221; break;
    case 3: PWMFrequency = 4883; break;
}

// Second spindle frequency override
if (SecondSpindleEnable) {
    switch (SpindleSetup.LimitSpindleRange) {
        case 3: PWMFrequency = 76; break;
        case 2: PWMFrequency = 610; break;
        case 1: PWMFrequency = 1221; break;
        case 0: PWMFrequency = 4883; break;
    }
}
```

### PWM Output Management

#### Output Selection Process
1. **Clear Previous Assignments**: Unselect outputs 1-8 when changing modes
2. **Assign Mode-Specific Outputs**: Set required outputs based on PWM type
3. **Configure Parameters**: Update frequency, options, and floor values
4. **Write PLC Definitions**: Generate appropriate output assignments

#### Mode-Specific Output Assignments

**J-Tech Laser Configuration:**
```csharp
// Assign PWM output
foreach (var def in data.Plc.Definitions.All) {
    if (def.Function.Name == "PWMOutput") {
        def.IONumber = 2;
        def.IsSelected = true;
    }
    if (def.Function.DisplayName == "LaserEnable") {
        def.IONumber = 4;
        def.IsSelected = true;
    }
    if (def.Function.DisplayName == "LaserReset") {
        def.IONumber = 7;
        def.IsSelected = true;
    }
}
```

**BLDC Configuration:**
```csharp
// Assign fault monitoring
if (def.Function.DisplayName == "NoFaultOut") {
    def.IONumber = 1;
    def.IsSelected = true;
}
```

### Implementation Guidelines for PWM Configuration

#### Reading Existing Configuration
```csharp
1. Load parameter values via CNCUtils.GetParameterValue():
   - PWM frequency: CNCUtils.GetParameterValue(CNC12Parameters.ACORN_PWM_FREQUENCY_PARM)
   - PWM options: CNCUtils.GetParameterValue(CNC12Parameters.ACORN_PWM_OPTIONS_PARM)
   - PWM floor: CNCUtils.GetParameterValue(CNC12Parameters.ACORN_PWM_FLOOR_PARM)
   - Cooling delay: CNCUtils.GetParameterValue(CNC12Parameters.LASER_COOLING_FAN_DELAY_TIMER)

2. Decode option bits via CNCUtils.IsBitSet():
   - Bit 0: inverseEnabled = CNCUtils.IsBitSet((int)PWMParam, 0)
   - Bit 1: PWMVelocity100 = CNCUtils.IsBitSet((int)PWMParam, 1)
   - Bit 2: floorEnabled = CNCUtils.IsBitSet((int)PWMParam, 2)

3. Determine PWM mode:
   - Check frequency value (0 = disabled)
   - Check assigned outputs in PLC definitions
   - Check wizard settings
```

#### Writing New PWM Configuration  
```csharp
1. Set PWM parameters via CNCUtils.SetParameterValue():
   - CNCUtils.SetParameterValue(CNC12Parameters.ACORN_PWM_FREQUENCY_PARM, PWMFrequency)
   - CNCUtils.SetParameterValue(CNC12Parameters.ACORN_PWM_FLOOR_PARM, minimums)
   - CNCUtils.SetParameterValue(CNC12Parameters.LASER_COOLING_FAN_DELAY_TIMER, delayTimer)

2. Build options bit field via CNCUtils.ModifyBit():
   - PWMParam = CNCUtils.ModifyBit((int)PWMParam, 0, isinverseEnabled)
   - PWMParam = CNCUtils.ModifyBit((int)PWMParam, 1, PWMVelocity100)
   - PWMParam = CNCUtils.ModifyBit((int)PWMParam, 2, minimums > 0)
   - CNCUtils.SetParameterValue(CNC12Parameters.ACORN_PWM_OPTIONS_PARM, PWMParam)

3. Configure mode-specific outputs in PLC definitions:
   - Unselect conflicting outputs
   - Assign mode-specific outputs
   - Set wizard mode flags
```

#### Validation Rules
- **Frequency Range**: 0-24000 Hz for standard systems
- **Floor Range**: 0-100% (scaled to 0-10% if PWMVelocity100 is false)
- **Output Conflicts**: Only one PWM mode active at a time
- **Hardware Compatibility**: Respect Acorn vs standard system limitations

#### Writing New Spindle Configuration
```pseudocode
1. Configure encoder:
   - Set encoder counts (parameter 34)
   - Set spindle axis (parameter 35)
   - Enable encoder in parameter 78 bit field
   
2. Configure speeds:
   - Call SetHighRangeSpindleSpeed(MAX, value)
   - Call SetHighRangeSpindleSpeed(MIN, value)
   - Set gear ratios (parameters 65-67)
   
3. Configure output:
   - Set analog voltage range (parameter 420)
   - Set spindle OK delay (parameter 996)
   
4. Enable advanced features:
   - Set rigid tapping parameters if needed
   - Configure second spindle if present
   - Set RTG display preferences
```

#### Validation Rules
1. **Encoder Counts**: Must be positive, typically 400-20000 range
2. **Speed Ranges**: Max > Min, within motor capabilities
3. **Gear Ratios**: Must be positive, reasonable for mechanical setup
4. **Voltage Range**: Must match VFD/drive input requirements
5. **Rigid Tapping**: Requires encoder feedback enabled

#### Common Configuration Examples

**Basic VFD Spindle:**
```
Encoder: Disabled
Speed Range: 100-6000 RPM
Analog Output: 0-10VDC
Gear Ratio: 1.0 (direct drive)
```

**Servo Spindle with Encoder:**
```
Encoder: 2000 PPR (8000 counts)
Speed Range: 50-8000 RPM  
Analog Output: ±10VDC
Gear Ratio: 1.5 (belt reduction)
Rigid Tapping: Enabled
```

**Multi-Range Belt Drive:**
```
Encoder: 1000 PPR (4000 counts)
Low Range: 3.0 ratio (100-1000 RPM)
Medium Range: 1.5 ratio (500-3000 RPM)  
High Range: 1.0 ratio (1000-6000 RPM)
```

### Spindle vs. Other Systems Integration

#### Relationship to Axis Configuration
- Spindle encoder uses axis encoder inputs (typically axis 5 or 8)
- Shares encoder port assignments with axis encoders
- Rigid tapping requires coordinated axis and spindle motion

#### Relationship to I/O Configuration  
- Spindle enable/direction outputs defined in PLC I/O sections
- Spindle speed analog output separate from digital I/O
- Spindle OK input can be configured as standard input

This spindle configuration system integrates with both the PLC I/O management and axis configuration to provide complete machine setup capabilities.

## Probe Configuration System

The wizard manages probe configuration for workpiece measurement and part setup operations. Probe configuration includes input assignments, probe types, and operational parameters.

### Probe Parameters

| Parameter | Purpose | Description | Values |
|-----------|---------|-------------|--------|
| 12 | Probe Tool Number | Tool number assigned to probe | Tool number (1-999) |
| 13 | Probe Recovery Distance | Retract distance after contact | Distance in machine units |
| 14 | Fast Probe Rate | Fast probing speed | Units per minute |
| 15 | Slow Probe Rate | Slow probing speed | Units per minute |
| 16 | Maximum Probing Distance | Maximum search distance | Distance in machine units |
| 153 | Probe Protection | Enable probe protection | 0=Disabled, 1=Enabled |
| 406 | Probe Input Type | Normal open/closed configuration | 0=NO, 1=NC |
| 409 | Probe Type | Physical probe type | 0=Conductive, 1=Non-conductive |
| 410 | Display Probe Warning | Show probe warnings | 0=No, 1=Yes |
| 416 | Probe Inhibit | Inhibit spindle when detect is on | Bit field (see below) |
| 155 | DSP Probe Parameter | Enhanced probe type setting | 0=Standard, 1=DSP, 2=DP7 |
| 3 (bit 4) | Probe Protection Based on Tool Number | Use tool number for protection | Bit 4 in modal tool parameter |

### Detailed Probe Configuration

#### Basic Probe Settings

**Probe Tool Number** - Parameter 12
```csharp
// Set probe tool number to 10
cncPipe.parameter.SetMachineParameter(12, 10);

// Read current probe tool number
cncPipe.parameter.GetMachineParameterValue(12, out double probeToolNumber);
```

**Fast Probe Rate** - Parameter 14
```csharp
// Set fast probe rate to 12 IPM
cncPipe.parameter.SetMachineParameter(14, 12);

// Read current fast probe rate
cncPipe.parameter.GetMachineParameterValue(14, out double fastProbeRate);
```

**Slow Probe Rate** - Parameter 15
```csharp
// Set slow probe rate to 5 IPM
cncPipe.parameter.SetMachineParameter(15, 5);

// Read current slow probe rate
cncPipe.parameter.GetMachineParameterValue(15, out double slowProbeRate);
```

**Recovery Distance** - Parameter 13
```csharp
// Set recovery distance to 0.05 inches
cncPipe.parameter.SetMachineParameter(13, 0.05);

// Read current recovery distance
cncPipe.parameter.GetMachineParameterValue(13, out double recoveryDistance);
```

**Maximum Probing Distance** - Parameter 16
```csharp
// Set maximum probing distance to 10 inches
cncPipe.parameter.SetMachineParameter(16, 10);

// Read current maximum probing distance
cncPipe.parameter.GetMachineParameterValue(16, out double maxProbingDistance);
```

#### Probe Protection Settings

**Probe Protection Enabled** - Parameter 153
```csharp
// Enable probe protection
cncPipe.parameter.SetMachineParameter(153, 1);

// Disable probe protection
cncPipe.parameter.SetMachineParameter(153, 0);

// Read probe protection status
cncPipe.parameter.GetMachineParameterValue(153, out double probeProtection);
bool protectionEnabled = probeProtection != 0;
```

**Probe Protection Based on Tool Number** - Parameter 3, Bit 4
```csharp
// Read current modal tool parameter
cncPipe.parameter.GetMachineParameterValue(3, out double modalToolParam);
int toolParam = (int)modalToolParam;

// Check if probe protection is based on tool number
bool protectionBasedOnTool = GeneralUtils.IsBitSet(toolParam, 4);

// Enable probe protection based on tool number
toolParam = GeneralUtils.ModifyBit(toolParam, 4, true);
cncPipe.parameter.SetMachineParameter(3, toolParam);

// Disable probe protection based on tool number
toolParam = GeneralUtils.ModifyBit(toolParam, 4, false);
cncPipe.parameter.SetMachineParameter(3, toolParam);
```

**Display Warning to Verify Probe is Functioning** - Parameter 410
```csharp
// Enable probe warning display
cncPipe.parameter.SetMachineParameter(410, 1);

// Disable probe warning display
cncPipe.parameter.SetMachineParameter(410, 0);

// Read probe warning setting
cncPipe.parameter.GetMachineParameterValue(410, out double probeWarning);
bool warningEnabled = probeWarning != 0;
```

**Inhibit Spindle when Detect is on (Green)** - Parameter 416
```csharp
// Enable spindle inhibit when probe detect is active
cncPipe.parameter.SetMachineParameter(416, 1);  // or 3 for enhanced mode

// Disable spindle inhibit
cncPipe.parameter.SetMachineParameter(416, 0);

// Read probe inhibit setting
cncPipe.parameter.GetMachineParameterValue(416, out double probeInhibit);
bool spindleInhibitEnabled = (probeInhibit == 1 || probeInhibit == 3);
```

#### Per-Axis Probe Jog Rates

Probe jog rates are configured per-axis using the CentroidAPI axis interface:

**Probe Slow Jog Rates**
```csharp
// Set probe slow jog rates for axes 1-4 (values: 10, 10, 10, 10)
cncPipe.axis.SetRate(CNCPipe.Axes.AXIS_1, CNCPipe.Axis.Rate.SLOW_JOG_PROBE, 10);
cncPipe.axis.SetRate(CNCPipe.Axes.AXIS_2, CNCPipe.Axis.Rate.SLOW_JOG_PROBE, 10);
cncPipe.axis.SetRate(CNCPipe.Axes.AXIS_3, CNCPipe.Axis.Rate.SLOW_JOG_PROBE, 10);
cncPipe.axis.SetRate(CNCPipe.Axes.AXIS_4, CNCPipe.Axis.Rate.SLOW_JOG_PROBE, 10);

// Read probe slow jog rates
cncPipe.axis.GetRate(CNCPipe.Axes.AXIS_1, CNCPipe.Axis.Rate.SLOW_JOG_PROBE, out double axis1SlowProbe);
```

**Probe Fast Jog (-) Rates**
```csharp
// Set probe fast jog minus rates for axes 1-4 (values: 50, 50, 10, 50)
cncPipe.axis.SetRate(CNCPipe.Axes.AXIS_1, CNCPipe.Axis.Rate.FAST_JOG_MINUS_PROBE, 50);
cncPipe.axis.SetRate(CNCPipe.Axes.AXIS_2, CNCPipe.Axis.Rate.FAST_JOG_MINUS_PROBE, 50);
cncPipe.axis.SetRate(CNCPipe.Axes.AXIS_3, CNCPipe.Axis.Rate.FAST_JOG_MINUS_PROBE, 10);
cncPipe.axis.SetRate(CNCPipe.Axes.AXIS_4, CNCPipe.Axis.Rate.FAST_JOG_MINUS_PROBE, 50);

// Read probe fast jog minus rates
cncPipe.axis.GetRate(CNCPipe.Axes.AXIS_1, CNCPipe.Axis.Rate.FAST_JOG_MINUS_PROBE, out double axis1FastMinus);
```

**Probe Fast Jog (+) Rates**
```csharp
// Set probe fast jog plus rates for axes 1-4 (values: 50, 50, 50, 50)
cncPipe.axis.SetRate(CNCPipe.Axes.AXIS_1, CNCPipe.Axis.Rate.FAST_JOG_PLUS_PROBE, 50);
cncPipe.axis.SetRate(CNCPipe.Axes.AXIS_2, CNCPipe.Axis.Rate.FAST_JOG_PLUS_PROBE, 50);
cncPipe.axis.SetRate(CNCPipe.Axes.AXIS_3, CNCPipe.Axis.Rate.FAST_JOG_PLUS_PROBE, 50);
cncPipe.axis.SetRate(CNCPipe.Axes.AXIS_4, CNCPipe.Axis.Rate.FAST_JOG_PLUS_PROBE, 50);

// Read probe fast jog plus rates
cncPipe.axis.GetRate(CNCPipe.Axes.AXIS_1, CNCPipe.Axis.Rate.FAST_JOG_PLUS_PROBE, out double axis1FastPlus);
```

#### Complete Probe Configuration Example

```csharp
public void ConfigureProbeSettings(CNCPipe cncPipe)
{
    // 1. Basic Probe Settings
    cncPipe.parameter.SetMachineParameter(12, 10);    // Probe tool number = 10
    cncPipe.parameter.SetMachineParameter(14, 12);    // Fast probe rate = 12 IPM
    cncPipe.parameter.SetMachineParameter(15, 5);     // Slow probe rate = 5 IPM
    cncPipe.parameter.SetMachineParameter(13, 0.05);  // Recovery distance = 0.05"
    cncPipe.parameter.SetMachineParameter(16, 10);    // Maximum distance = 10"
    
    // 2. Probe Protection Settings
    cncPipe.parameter.SetMachineParameter(153, 1);    // Enable probe protection
    cncPipe.parameter.SetMachineParameter(410, 1);    // Enable warning display
    cncPipe.parameter.SetMachineParameter(416, 1);    // Enable spindle inhibit
    
    // Configure probe protection based on tool number (disable)
    cncPipe.parameter.GetMachineParameterValue(3, out double modalToolParam);
    int toolParam = GeneralUtils.ModifyBit((int)modalToolParam, 4, false);
    cncPipe.parameter.SetMachineParameter(3, toolParam);
    
    // 3. Per-Axis Probe Jog Rates
    // Slow jog rates (10, 10, 10, 10)
    for (int axis = 0; axis < 4; axis++)
    {
        cncPipe.axis.SetRate((CNCPipe.Axes)axis, CNCPipe.Axis.Rate.SLOW_JOG_PROBE, 10);
    }
    
    // Fast jog minus rates (50, 50, 10, 50)
    double[] fastMinusRates = { 50, 50, 10, 50 };
    for (int axis = 0; axis < 4; axis++)
    {
        cncPipe.axis.SetRate((CNCPipe.Axes)axis, CNCPipe.Axis.Rate.FAST_JOG_MINUS_PROBE, fastMinusRates[axis]);
    }
    
    // Fast jog plus rates (50, 50, 50, 50)
    for (int axis = 0; axis < 4; axis++)
    {
        cncPipe.axis.SetRate((CNCPipe.Axes)axis, CNCPipe.Axis.Rate.FAST_JOG_PLUS_PROBE, 50);
    }
}
```

### Probe Input Configuration

#### PLC Input Assignments
Probe configuration requires specific input assignments in the PLC file:

```plc
# wizardregion Inputs
ProbeTripped    IS INP15    # Probe contact signal
ProbeDetected   IS INP16    # Probe protection/detection (optional)
# endregion
```

#### Input Type Behavior
- **Normally Open (0)**: Probe reads 0 when not touching, 1 when in contact
- **Normally Closed (1)**: Probe reads 1 when not touching, 0 when in contact

### Probe Types and Settings

#### Probe Type Configuration
| Probe Type | Value | Description |
|------------|-------|-------------|
| Conductive | 0 | Electrical contact probe (most common) |
| Non-conductive | 1 | Optical or mechanical switch probe |

#### DSP Probe Settings (Parameter 143)
| Setting | Value | Description |
|---------|-------|-------------|
| Standard Probe | 0 | Basic probe functionality |
| DSP Probe | 1 | Digital Signal Processing enhanced |
| DP7 Probe | 2 | Renishaw DP7 or similar |

### Probe Protection System

#### Probe Inhibit Parameter (416)
Bit field controlling probe protection features:

| Bit | Purpose | Description |
|-----|---------|-------------|
| 0 | Probe Protection Enable | Enable probe protection logic |
| 1 | Tool Number Based | Use tool number for probe protection |
| 2 | Reserved | - |
| 3 | Reserved | - |

## Touch Plate Configuration System

Touch plate configuration manages workpiece setup and coordinate system establishment using touch plates or edge finders.

### Touch Plate Parameters

| Parameter | Purpose | Description | Units |
|-----------|---------|-------------|-------|
| 540 | Touch Plate Input | Input assignment for touch plate | Input number |
| 541 | Touch Plate Detect | Detection input (optional) | Input number |
| 542 | Touch Plate Input Type | Normal open/closed setting | 0=NO, 1=NC |
| 543 | Touch Plate Wall Height | Height of touch plate walls | Linear units |
| 544 | Touch Plate Wall Thickness | Thickness of touch plate walls | Linear units |
| 545 | Touch Plate Internal Diameter | Internal diameter for bore touch | Linear units |
| 546 | Touch Plate Max Distance | Maximum travel distance | Linear units |
| 547 | Touch Plate Retract Distance | Retract distance after contact | Linear units |
| 548 | Touch Plate Fast Rate | Fast approach speed | Units per minute |
| 549 | Touch Plate Slow Rate | Slow touch speed | Units per minute |
| 550 | Touch Plate Attributes | Configuration bit field | Bit flags |

### Touch Plate Types

#### Physical Touch Plate Types
- **Standard Touch Plate**: Rectangular touch plate for surface and edge finding
- **3D Touch Plate**: Multi-surface touch plate with walls and bores
- **Custom Touch Plate**: User-defined dimensions

#### Touch Plate Attributes (Parameter 550)
| Bit | Purpose | Description |
|-----|---------|-------------|
| 0 | Inside Touch Enable | Enable internal bore touching |
| 1 | Surface Plate Mode | Use moveable surface plate |
| 2 | Bore Touch Enable | Enable bore measurement |

### Touch Plate Input Configuration

#### PLC Input Assignment
```plc
# wizardregion Inputs
TouchPlateTripped   IS INP17    # Touch plate contact signal
TouchPlateDetected  IS INP18    # Touch plate detection (optional)
# endregion
```

#### Input Inversion Logic
Touch plate inputs require special inversion handling:
- **Normally Closed Touch Plate**: Input inverted in CNC12 system
- **Normally Open Touch Plate**: Input used as-is

## ATC (Automatic Tool Changer) Configuration System

The wizard supports multiple ATC types with comprehensive configuration for each system type.

### ATC Core Parameters

| Parameter | Purpose | Description | Values |
|-----------|---------|-------------|--------|
| 6 | Tool Changer Installed | Enable tool changer | 0=None, 1=Enabled |
| 830 | ATC Type | Specific ATC type | See ATC Types table |
| 161 | ATC Max Bins | Number of tool positions | 1-99 positions |

### ATC Type Configuration

| ATC Type | Parameter 830 | Description |
|----------|---------------|-------------|
| None | 0 | No automatic tool changer |
| Carousel | 1 | Rotating carousel ATC |
| Counter Turret | 2 | Lathe counter-rotating turret |
| GreyCode1 | 3 | Gray code position sensing (type 1) |
| GreyCode2 | 4 | Gray code position sensing (type 2) |
| Time Turret | 5 | Time-based turret positioning |
| Axis Driven Turret | 6 | Servo axis driven turret |
| Rack Mount | 7 | Fixed position rack system |
| Electric Turret | 8 | Electric motor driven turret |

### ATC Type-Specific Parameters

#### Carousel ATC (Type 1)
| Parameter | Purpose | Description |
|-----------|---------|-------------|
| 852 | Skip First Count on Reversal | Skip first position count | 0=No, 1=Yes |

Position stored in G30 reference points:
- X Position: G30 X value
- Y Position: G30 Y value  
- Z Position: G30 Z value

#### Rack Mount ATC (Type 7)
| Parameter | Purpose | Description |
|-----------|---------|-------------|
| 431 | Holding Configuration | Tool holding method | 0=Hole, 1=Fork |
| 432 | Tool Length Method | Measurement method | 0=Fixed position, 1=Surface plate |

Tool position bits stored in parameters 831-882 (one per tool position).

#### Turret Systems (Types 2, 5, 8)
| Parameter | Purpose | Description |
|-----------|---------|-------------|
| 850 | Time Delay to Start | Initial delay before movement | Seconds |
| 848 | Time to Reverse | Reverse direction time | Seconds |
| 849 | Time to Fault | Fault detection time | Seconds |
| 851 | Time Delay Before Reverse | Delay before reverse | Seconds |
| 975 | Time Per Tool Position | Time for each position (Type 5) | Seconds |

#### Axis Driven Turret (Type 6)
| Parameter | Purpose | Description |
|-----------|---------|-------------|
| 853 | Travel Past Distance | Overtravel distance | Linear units |
| 854 | Travel Behind Distance | Backoff distance | Linear units |

### ATC Tool Position Configuration

#### Tool Position Sensing
Different ATC types use various methods for position feedback:

**Gray Code Systems (Types 3, 4):**
- Binary sensors for absolute position
- Multiple inputs for position encoding
- Automatic position verification

**Time-Based Systems (Type 5):**
- Calculated positioning based on timing
- No position feedback required
- Parameter 975 sets time per position

**Axis-Driven Systems (Type 6):**
- Servo axis provides position feedback
- Encoder-based positioning
- Integrated with axis configuration

#### Tool Change Macro Integration
ATC configuration generates or selects appropriate macro files:

**Mill/Router Systems:**
- `mfunc6.mac` - Standard M6 tool change macro
- Location varies by ATC type:
  - Universal: `\resources\ATC\Universal\mfunc6.mac`
  - Fixed Carousel: `\resources\ATC\FixedCarousel\mfunc6.mac`

**Lathe Systems:**
- `cnctch.mac` - Tool change macro for lathes
- Handles turret positioning and tool selection

### Implementation Guidelines for Probe/Touch Plate/ATC

#### Reading Current Configuration
```pseudocode
Probe Configuration:
1. Get probe input assignments from PLC definitions
2. Read probe parameters (11, 406, 409, 12-15, 416, 410)
3. Parse probe type and protection settings
4. Check input inversion states

Touch Plate Configuration:
1. Get touch plate input assignments from PLC definitions  
2. Read touch plate parameters (540-550)
3. Parse touch plate type and attributes
4. Handle input inversion logic

ATC Configuration:
1. Read ATC type from parameter 830
2. Get tool count from parameter 161
3. Load type-specific parameters based on ATC type
4. Read tool position data (parameters 831-882)
5. Check G30 reference points for position data
```

#### Writing New Configuration
```pseudocode
Probe Configuration:
1. Assign probe inputs in PLC wizard regions
2. Set probe parameters based on probe type
3. Configure protection settings
4. Update input inversion parameters

Touch Plate Configuration:
1. Assign touch plate inputs in PLC wizard regions
2. Set dimensional parameters (543-547)
3. Configure touch plate attributes (550)
4. Handle input type and inversion

ATC Configuration:
1. Set ATC type (parameter 830) and enable (parameter 6)
2. Configure tool count (parameter 161)
3. Set type-specific timing/position parameters
4. Generate or copy appropriate macro files
5. Configure tool position sensing inputs
```

#### Validation Rules
**Probe System:**
- Probe input must be assigned for probe operations
- Detection input recommended for NC probes
- Tool number must not conflict with cutting tools
- Speed parameters must be positive and reasonable

**Touch Plate System:**
- Touch plate input required for operations
- Dimensional parameters must be positive
- Fast rate must be greater than slow rate
- Maximum distance must be reasonable for machine

**ATC System:**
- Tool count must match physical ATC capacity
- Timing parameters must allow proper mechanical movement
- Position sensing inputs must match ATC type requirements
- Macro files must exist for selected ATC type

This probe, touch plate, and ATC configuration system works alongside the PLC I/O management, axis configuration, spindle configuration, and PWM systems to provide complete machine setup through the wizard interface.

## Probe, Touch Plate, and ATC Configuration Systems

The wizard manages probe, touch plate, and automatic tool changer (ATC) configuration through parameter storage and specialized API calls for reference point management.

### Probe Configuration

#### Core Probe Parameters
| Parameter | Purpose | Description |
|-----------|---------|-------------|
| 406 | Probe Input Type | Input type (NC/NO) |
| 409 | Probe Type | Probe configuration type |
| 410 | Display Probe Warning | Show probe warnings |
| 416 | Probe Inhibit | Probe protection settings |

#### Probe Configuration APIs
```csharp
// Reading probe configuration
double probeInputType = CNCUtils.GetParameterValue(CNC12Parameters.PROBE_INPUT_TYPE);
double probeType = CNCUtils.GetParameterValue(CNC12Parameters.PROBE_TYPE);
double probeWarning = CNCUtils.GetParameterValue(CNC12Parameters.DISPLAY_PROBE_WARNING_PARAM);
double probeInhibit = CNCUtils.GetParameterValue(CNC12Parameters.PROBE_INHIBIT_PARM);

// Writing probe configuration
CNCUtils.SetParameterValue(CNC12Parameters.PROBE_INPUT_TYPE, inputType);
CNCUtils.SetParameterValue(CNC12Parameters.PROBE_TYPE, probeType);
CNCUtils.SetParameterValue(CNC12Parameters.DISPLAY_PROBE_WARNING_PARAM, showWarning);
CNCUtils.SetParameterValue(CNC12Parameters.PROBE_INHIBIT_PARM, inhibitSettings);

// Input assignment validation
if (plc.Definitions.IsSelected(DefinitionTypes.ProbeTripped)) {
    int inputNumber = plc.Definitions.FindIONumber(DefinitionTypes.ProbeTripped);
    // Configure probe input parameters
}
```

### Touch Plate Configuration

#### Touch Plate Parameters
| Parameter | Purpose | Description |
|-----------|---------|-------------|
| 540 | Touch Plate Input | Input assignment |
| 541 | Touch Plate Detect | Detection input |
| 542 | Touch Plate Input Type | Input type (NC/NO) |
| 543 | Touch Plate Wall Height | Wall height dimension |
| 544 | Touch Plate Wall Thickness | Wall thickness dimension |
| 545 | Touch Plate Internal Diameter | Internal diameter |
| 546 | Touch Plate Max Distance | Maximum search distance |
| 547 | Touch Plate Retract Distance | Retract distance |
| 548 | Touch Plate Fast Rate | Fast probing rate |
| 549 | Touch Plate Slow Rate | Slow probing rate |
| 550 | Touch Plate Attributes | Bit field for options |

#### Touch Plate Configuration APIs
```csharp
// Reading touch plate configuration
double fastRate = CNCUtils.GetParameterValue(CNC12Parameters.TOUCH_PLATE_FAST_RATE_PARM);
double slowRate = CNCUtils.GetParameterValue(CNC12Parameters.TOUCH_PLATE_SLOW_RATE_PARM);
double maxDistance = CNCUtils.GetParameterValue(CNC12Parameters.TOUCH_PLATE_MAX_DISTANCE_PARM);
double retractDistance = CNCUtils.GetParameterValue(CNC12Parameters.TOUCH_PLATE_RETRACT_DISTANCE_PARM);
double wallHeight = CNCUtils.GetParameterValue(CNC12Parameters.TOUCH_PLATE_WALL_HEIGHT_PARM);
double wallThickness = CNCUtils.GetParameterValue(CNC12Parameters.TOUCH_PLATE_WALL_THICKNESS_PARM);
double diameter = CNCUtils.GetParameterValue(CNC12Parameters.TOUCH_PLATE_INTERNAL_DIAMETER_PARM);
double inputType = CNCUtils.GetParameterValue(CNC12Parameters.TOUCH_PLATE_INPUT_TYPE_PARM);

// Touch plate attributes bit field
double touchPlateAttributes = CNCUtils.GetParameterValue(CNC12Parameters.TOUCH_PLATE_ATTRIBUTES_PARM);
bool insideTouch = CNCUtils.IsBitSet((int)touchPlateAttributes, 0);
bool boreEnabled = CNCUtils.IsBitSet((int)touchPlateAttributes, 1);
bool surfacePlate = CNCUtils.IsBitSet((int)touchPlateAttributes, 2);

// Writing touch plate configuration
CNCUtils.SetParameterValue(CNC12Parameters.TOUCH_PLATE_FAST_RATE_PARM, fastRate);
CNCUtils.SetParameterValue(CNC12Parameters.TOUCH_PLATE_SLOW_RATE_PARM, slowRate);
CNCUtils.SetParameterValue(CNC12Parameters.TOUCH_PLATE_MAX_DISTANCE_PARM, maxDistance);
CNCUtils.SetParameterValue(CNC12Parameters.TOUCH_PLATE_RETRACT_DISTANCE_PARM, retractDistance);
CNCUtils.SetParameterValue(CNC12Parameters.TOUCH_PLATE_WALL_HEIGHT_PARM, wallHeight);
CNCUtils.SetParameterValue(CNC12Parameters.TOUCH_PLATE_WALL_THICKNESS_PARM, wallThickness);
CNCUtils.SetParameterValue(CNC12Parameters.TOUCH_PLATE_INTERNAL_DIAMETER_PARM, diameter);
CNCUtils.SetParameterValue(CNC12Parameters.TOUCH_PLATE_INPUT_TYPE_PARM, inputType);

// Build and save attributes bit field
int touchPlateAttributes = 0;
touchPlateAttributes = CNCUtils.ModifyBit(touchPlateAttributes, 0, insideTouch);
touchPlateAttributes = CNCUtils.ModifyBit(touchPlateAttributes, 1, boreEnabled);
touchPlateAttributes = CNCUtils.ModifyBit(touchPlateAttributes, 2, surfacePlate);
CNCUtils.SetParameterValue(CNC12Parameters.TOUCH_PLATE_ATTRIBUTES_PARM, touchPlateAttributes);

// Input assignment and state management
if (plc.Definitions.IsSelected(DefinitionTypes.TouchPlateTripped)) {
    int inputNum = plc.Definitions.FindIONumber(DefinitionTypes.TouchPlateTripped);
    // Reverse state for NC probes: NC probes need inversion in CNC12
    touchPlateInput.State = inputType == InputType.NormallyOpen ? 
        InputType.NormallyClosed : InputType.NormallyOpen;
}
```

### ATC Configuration

#### Core ATC Parameters
| Parameter | Purpose | Description |
|-----------|---------|-------------|
| 6 | Tool Changer Installed | Enable ATC operation |
| 161 | ATC Max Bins | Number of tool positions |
| 830 | ATC Type | ATC type identifier |
| 831-882 | Tool Position Data | Tool position information |
| 847-854 | Timing Parameters | ATC operation timing |

#### ATC Configuration APIs
```csharp
// Reading ATC configuration
double atcType = CNCUtils.GetParameterValue(CNC12Parameters.ATC_TYPE);
double maxBins = CNCUtils.GetParameterValue(CNC12Parameters.ATC_MAX_BINS_PARM);
double toolChangerInstalled = CNCUtils.GetParameterValue(CNC12Parameters.TOOL_CHANGER_INSTALLED_PARM);
double enhancedATC = CNCUtils.GetParameterValue(CNC12Parameters.ENHANCED_ATC_PARM);

// RackMount specific parameters
double holdingConfig = CNCUtils.GetParameterValue(CNC12Parameters.RTC_RACK_MOUNT_HOLDING_CONFIG);
double toolLengthMethod = CNCUtils.GetParameterValue(CNC12Parameters.RTC_RACK_MOUNT_TOOL_LENGTH_METHOD);

// Reading reference points for ATC positions
double carouselX = CNCUtils.GetWorkpieceReferencePoint(ReferencePoints.G30, 1);
double carouselY = CNCUtils.GetWorkpieceReferencePoint(ReferencePoints.G30, 2);
double carouselZ = CNCUtils.GetWorkpieceReferencePoint(ReferencePoints.G30, 3);

// Writing ATC configuration
CNCUtils.SetParameterValue(CNC12Parameters.ATC_MAX_BINS_PARM, numberOfBins);
CNCUtils.SetParameterValue(CNC12Parameters.TOOL_CHANGER_INSTALLED_PARM, enabled ? 1 : 0);

// ATC type-specific configuration
switch (atcType) {
    case ATCTypes.RackMount:
        CNCUtils.SetParameterValue(CNC12Parameters.ATC_TYPE, 7);
        CNCUtils.SetParameterValue(CNC12Parameters.RTC_RACK_MOUNT_HOLDING_CONFIG, holdingConfig);
        CNCUtils.SetParameterValue(CNC12Parameters.RTC_RACK_MOUNT_TOOL_LENGTH_METHOD, toolLengthMethod);
        CNCUtils.SetWorkpieceReferencePoint(ReferencePoints.G30, 3, zHeightFlyover);
        break;
        
    case ATCTypes.Carousel:
        CNCUtils.SetParameterValue(CNC12Parameters.ATC_TYPE, 1);
        CNCUtils.SetParameterValue(CNC12Parameters.ENHANCED_ATC_PARM, 1);
        CNCUtils.SetWorkpieceReferencePoint(ReferencePoints.G30, 1, carouselXPos);
        CNCUtils.SetWorkpieceReferencePoint(ReferencePoints.G30, 2, carouselYPos);
        CNCUtils.SetWorkpieceReferencePoint(ReferencePoints.G30, 3, carouselZPos);
        break;
        
    case ATCTypes.AxisDrivenTurret:
        CNCUtils.SetParameterValue(CNC12Parameters.ATC_TYPE, 6);
        CNCUtils.SetParameterValue(CNC12Parameters.AXIS_DRIVEN_TURRET_TRAVEL_PAST_DISTANCE, travelPast);
        CNCUtils.SetParameterValue(CNC12Parameters.AXIS_DRIVEN_TURRET_TRAVEL_BEHIND_DISTANCE, travelBehind);
        break;
}

// Timing parameter configuration
CNCUtils.SetParameterValue(CNC12Parameters.TURRET_SETTLE_TIME, settleTime);
CNCUtils.SetParameterValue(CNC12Parameters.TIME_TO_REVERSE, timeToReverse);
CNCUtils.SetParameterValue(CNC12Parameters.TIME_TO_FAULT, timeToFault);
CNCUtils.SetParameterValue(CNC12Parameters.TIME_DELAY_TO_START, delayToStart);
CNCUtils.SetParameterValue(CNC12Parameters.TIME_DELAY_BEFORE_REVERSE, delayBeforeReverse);
```

### Workpiece Reference Point Management

#### Reference Point APIs
```csharp
// Reading reference points (G28, G30, etc.)
double xPosition = CNCUtils.GetWorkpieceReferencePoint(ReferencePoints.G30, 1);
double yPosition = CNCUtils.GetWorkpieceReferencePoint(ReferencePoints.G30, 2);
double zPosition = CNCUtils.GetWorkpieceReferencePoint(ReferencePoints.G30, 3);

// Alternative API using skin.wcs directly
MainWindow.skin.wcs.GetWorkpieceReference(3, 1, out double xPos);   // G30 P3 X
MainWindow.skin.wcs.GetWorkpieceReference(3, 2, out double yPos);   // G30 P3 Y  
MainWindow.skin.wcs.GetWorkpieceReference(3, 3, out double zPos);   // G30 P3 Z

// Writing reference points
CNCUtils.SetWorkpieceReferencePoint(ReferencePoints.G30, 1, xPosition);
CNCUtils.SetWorkpieceReferencePoint(ReferencePoints.G30, 2, yPosition);
CNCUtils.SetWorkpieceReferencePoint(ReferencePoints.G30, 3, zPosition);

// Alternative API using skin.wcs directly
MainWindow.skin.wcs.SetWorkpieceReference(3, 1, xPosition);   // G30 P3 X
MainWindow.skin.wcs.SetWorkpieceReference(3, 2, yPosition);   // G30 P3 Y
MainWindow.skin.wcs.SetWorkpieceReference(3, 3, zPosition);   // G30 P3 Z
```

#### Reference Point Usage
- **G28**: Home position reference
- **G30**: Return point #2 (commonly used for ATC positions)
- **G30 P3**: Tool touch-off reference position
- **G30 P4**: Additional reference position

This probe, touch plate, and ATC configuration system works alongside the PLC I/O management, axis configuration, spindle configuration, and PWM systems to provide complete machine setup through the wizard interface.

## I/O Definition Management APIs

The wizard system provides comprehensive APIs for managing input/output assignments and PLC definitions. These APIs handle the assignment, verification, and management of physical I/O pins to logical functions.

### Core Definition Management APIs

#### Finding I/O Assignments
```csharp
// Find I/O number by function name
int ioNumber = Plc.Definitions.FindIONumber("EStopOK");
int probeInput = Plc.Definitions.FindIONumber("ProbeTripped");

// Find I/O number by definition type (specialized lookup)
int touchPlateTripped = Plc.Definitions.FindIONumber(DefinitionTypes.TouchPlateTripped);
int touchPlateDetected = Plc.Definitions.FindIONumber(DefinitionTypes.TouchPlateDetected);
int toolTouchDetected = Plc.Definitions.FindIONumber(DefinitionTypes.TTDetected);
int probeTripped = Plc.Definitions.FindIONumber(DefinitionTypes.ProbeTripped);
int eStopOK = Plc.Definitions.FindIONumber(DefinitionTypes.EStopOK);
int homeAll = Plc.Definitions.FindIONumber(DefinitionTypes.HomeAll);
int limitAll = Plc.Definitions.FindIONumber(DefinitionTypes.LimitAll);
```

#### Checking Definition Status
```csharp
// Check if definition is selected/assigned
bool isSelected = Plc.Definitions.IsSelected("EStopOK");
bool isTouchPlateSelected = Plc.Definitions.IsSelected(DefinitionTypes.TouchPlateTripped);

// Check if definition is available for assignment
bool isAvailable = Plc.Definitions.IsAvailable("PWMOutput");

// Check if definition is required by the system
bool isRequired = Plc.Definitions.IsRequired("EStopOK");
```

#### Managing Definition Assignments
```csharp
// Unselect specific inputs/outputs
Plc.Definitions.UnselectInput(5);        // Unselect input at I/O number 5
Plc.Definitions.UnselectOutput(2);       // Unselect output at I/O number 2

// Bulk unselect operations
Plc.Definitions.UnselectAll();           // Remove all assignments
Plc.Definitions.UnselectAllInputs();     // Remove all input assignments
Plc.Definitions.UnselectAllOutputs();    // Remove all output assignments  

// Unselect by function name
Plc.Definitions.Unselect("PWMOutput");
```

#### Definition Properties Access
```csharp
// Access definition collections
IEnumerable<Definition> allDefs = Plc.Definitions.All;        // All definitions
IEnumerable<Definition> available = Plc.Definitions.Available; // Unassigned definitions
IEnumerable<Definition> selected = Plc.Definitions.Selected;   // Assigned definitions
IEnumerable<Definition> required = Plc.Definitions.Required;   // Required definitions

// Individual definition properties
foreach (var def in Plc.Definitions.All) {
    string functionName = def.Function.Name;
    string displayName = def.Function.DisplayName;
    int ioNumber = def.IONumber;                    // Physical I/O pin number
    bool isSelected = def.IsSelected;               // Assignment status
    bool isRequired = def.Function.IsRequired;     // System requirement
}
```

### Specialized Input/Output Assignment APIs

#### Input Assignment with Parameter Updates
```csharp
// Touch plate input assignment
private void SetTouchPlateInputs() {
    int inputNumber;
    
    // Touch plate tripped input
    if (Plc.Definitions.IsSelected(DefinitionTypes.TouchPlateTripped)) {
        inputNumber = Plc.Definitions.FindIONumber(DefinitionTypes.TouchPlateTripped) + InputOffset;
        CNCUtils.SetParameterValue(CNC12Parameters.TOUCH_PLATE_INPUT_PARM, inputNumber);
    } else {
        CNCUtils.SetParameterValue(CNC12Parameters.TOUCH_PLATE_INPUT_PARM, 0);
    }
    
    // Touch plate detected input  
    if (Plc.Definitions.IsSelected(DefinitionTypes.TouchPlateDetected)) {
        inputNumber = Plc.Definitions.FindIONumber(DefinitionTypes.TouchPlateDetected) + InputOffset;
        CNCUtils.SetParameterValue(CNC12Parameters.TOUCH_PLATE_DETECT_PARM, inputNumber);
    } else {
        CNCUtils.SetParameterValue(CNC12Parameters.TOUCH_PLATE_DETECT_PARM, 0);
    }
}

// Tool touch-off input assignment
private void SetToolTouchOffDetect() {
    int toolTouchOffDetectInput = Plc.Definitions.FindIONumber(DefinitionTypes.TTDetected) + InputOffset;
    CNCUtils.SetParameterValue(CNC12Parameters.TT1_DETECT_INPUT, 
        Plc.Definitions.IsSelected(DefinitionTypes.TTDetected) ? toolTouchOffDetectInput : 0);
}
```

#### Output Assignment for PWM and Laser Systems
```csharp
// J-Tech laser output assignment
foreach (var def in Plc.Definitions.All) {
    // PWM output assignment
    if (def.Function.Name == "PWMOutput") {
        def.IONumber = 2;
        def.IsSelected = true;
    }
    
    // Laser enable output
    if (def.Function.DisplayName == "LaserEnable") {
        def.IONumber = 4;
        def.IsSelected = true;
    }
    
    // Laser reset output
    if (def.Function.DisplayName == "LaserReset") {
        def.IONumber = 7;
        def.IsSelected = true;
    }
}

// BLDC fault monitoring output
foreach (var def in Plc.Definitions.All) {
    if (def.Function.DisplayName == "NoFaultOut") {
        def.IONumber = 1;
        def.IsSelected = true;
    }
}
```

### Definition Type Resolution APIs

#### Display Name Resolution
```csharp
// Get display name for a function
string displayName = Plc.Definitions.GetDisplayName("EStopOK");

// Get input display name without BOB I/O
string inputName = Plc.Definitions.GetInputDisplayName_NoBob(5);
```

#### Definition Type Properties
```csharp
// Specialized definition type checking within FindIONumber logic
switch (definition) {
    case DefinitionTypes.ProbeTripped:
        definitions = All.FindAll(x => x.IsProbeTripped && x.IsSelected);
        break;
    case DefinitionTypes.ProbeDetected:  
        definitions = All.FindAll(x => x.IsProbeDetected && x.IsSelected);
        break;
    case DefinitionTypes.TTTripped:
        definitions = All.FindAll(x => x.IsTTTripped && x.IsSelected);
        break;
    case DefinitionTypes.TTDetected:
        definitions = All.FindAll(x => x.IsTTDetected && x.IsSelected);
        break;
    case DefinitionTypes.TouchPlateTripped:
        definitions = All.FindAll(x => x.IsTouchPlateTripped && x.IsSelected);
        break;
    case DefinitionTypes.TouchPlateDetected:
        definitions = All.FindAll(x => x.IsTouchPlateDetected && x.IsSelected);
        break;
    case DefinitionTypes.EStopOK:
        definitions = All.FindAll(x => x.IsEStopOk && x.IsSelected);
        break;
    case DefinitionTypes.HomeAll:
        definitions = All.FindAll(x => x.IsHomeAll && x.IsSelected);
        break;
    case DefinitionTypes.LimitAll:
        definitions = All.FindAll(x => x.IsLimitAll && x.IsSelected);
        break;
}
```

### Implementation Guidelines for I/O Management

#### Reading Current I/O Configuration  
```csharp
1. Load PLC definitions from functions.xml:
   - FunctionsFile.Load(definitions) automatically loads available functions
   - Plc.Definitions.All contains all possible I/O functions
   - Plc.Definitions.Selected contains currently assigned functions

2. Check assignment status:
   - Use IsSelected() to verify if specific functions are assigned
   - Use FindIONumber() to get physical pin assignments
   - Use Available collection to find unassigned functions

3. Validate assignments:
   - Check Required collection for missing mandatory assignments
   - Verify no conflicts between definitions
   - Ensure hardware compatibility
```

#### Writing New I/O Configuration
```csharp
1. Clear conflicting assignments:
   - Use UnselectInput()/UnselectOutput() for specific pins
   - Use UnselectAll*() methods for bulk clearing
   - Verify no double-assignments

2. Assign new functions:
   - Set definition.IONumber to physical pin
   - Set definition.IsSelected = true
   - Update associated CNC12 parameters via CNCUtils.SetParameterValue()

3. Validate and save:
   - Check all required functions are assigned
   - Verify parameter consistency
   - Write PLC file with new assignments
```

#### Best Practices for I/O Management
- **Always check IsSelected()** before using FindIONumber() results
- **Use specialized FindIONumber(DefinitionTypes)** for type-safe lookups
- **Clear conflicting assignments** before setting new ones
- **Update CNC12 parameters** when changing I/O assignments
- **Validate required functions** are assigned before saving
- **Respect hardware limitations** (pin counts, capabilities)

This I/O definition management system provides the foundation for all wizard I/O configuration, working with the axis, spindle, PWM, probe, touch plate, and ATC systems to create complete machine configurations.

---

# CentroidAPI - CNC12 Programming Interface

The CentroidAPI provides a comprehensive interface for communicating with Centroid CNC12 control systems. This section covers the primary components and usage patterns for direct API integration.

## CentroidAPI Overview

The CentroidAPI is the official programming interface for Centroid CNC12 systems, providing access to:
- Machine parameters and configuration
- Real-time system state
- Axis control and feedback
- Spindle control and monitoring
- I/O board management
- Workpiece coordinate systems
- System diagnostics and status

## API Structure and Method Access Patterns

### CNCPipe Object Structure
The CentroidAPI uses a hierarchical structure to organize different functional areas:

```csharp
CNCPipe cncPipe = new CNCPipe();

// Parameter access
cncPipe.parameter.GetMachineParameterValue(paramNum, out double value);
cncPipe.parameter.SetMachineParameter(paramNum, value);

// System information and hardware detection  
cncPipe.system.GetUnlockVersion(out CNCPipe.Sys.UnlockVersions version);
cncPipe.system.GetEther1616DeviceInfo(out List<CNCPipe.Sys.Ether1616Device> devices);

// Axis control
cncPipe.axis.SetTravelLimit(axis, direction, limit);
cncPipe.axis.SetRate(axis, rate, value);

// System state
cncPipe.state.SetHighRangeSpindleSpeed(valueType, speed);
cncPipe.state.GetHighRangeSpindleSpeed(valueType, out double speed);

// Workpiece references (direct on CNCPipe)
cncPipe.GetWorkpieceReference(reference, axis, out double value);
cncPipe.SetWorkpieceReference(reference, axis, value);
```

### Return Code Patterns
Different CentroidAPI method categories use different return patterns:

#### Parameter Methods - Return CNCPipe.ReturnCode
```csharp
CNCPipe.ReturnCode result = cncPipe.parameter.GetMachineParameterValue(paramNum, out double value);
if (result != CNCPipe.ReturnCode.SUCCESS)
{
    // Handle error
}
```

#### System Detection Methods - Void with Out Parameters  
```csharp
// These methods do not return error codes
cncPipe.system.GetUnlockVersion(out CNCPipe.Sys.UnlockVersions version);
cncPipe.system.GetEther1616DeviceInfo(out List<CNCPipe.Sys.Ether1616Device> devices);
cncPipe.system.GetPLCEXP1616NumberofDevices(out int count);
```

#### Workpiece Reference Methods - Void with Out Parameters
```csharp
// These methods also do not return error codes
cncPipe.GetWorkpieceReference(reference, axis, out double value);
cncPipe.SetWorkpieceReference(reference, axis, value);  // void return
```

## Core CentroidAPI Components

### CNCPipe Class
The main interface class that provides access to all CNC12 functionality.

```csharp
using CentroidAPI;

// Typical initialization pattern
CNCPipe cncConnection = new CNCPipe();
// Connection logic would go here
```

### CNCPipe Namespaces and Subcomponents

#### Parameter Management (`CNCPipe.parameter`)
Provides access to machine parameters (the numbered configuration values in CNC12).

**Key Methods:**
- `GetMachineParameterValue(int parameter, out double value)` - Read parameter values
- `SetMachineParameter(int parameter, double value)` - Write parameter values

#### System State (`CNCPipe.state`)
Manages real-time system state and operational values.

**Key Methods:**
- `SetHighRangeSpindleSpeed(CNCPipe.State.Value.MAX/MIN, value)` - Set spindle speed limits
- `GetHighRangeSpindleSpeed(CNCPipe.State.Value.MAX/MIN, out value)` - Read spindle speed limits

#### System Information (`CNCPipe.system`)
Provides hardware detection and system information.

**Key Methods:**
- `GetUnlockVersion(out CNCPipe.Sys.UnlockVersions version)` - Detect system type
- `GetEther1616DeviceInfo(out List<CNCPipe.Sys.Ether1616Device> devices)` - Acorn expansion boards
- `GetPLCEXP1616NumberofDevices(out int count)` - AcornSix expansion boards
- `GetECAT1616NumberOfDevices(out int count)` - Hickory expansion boards

#### Axis Control (`CNCPipe.axis`)
Controls individual axis properties and settings.

**Key Methods:**
- `SetCountsPerTurn(axis, counts)` - Set steps per revolution
- `SetTravelLimit(axis, direction, limit)` - Set axis travel limits
- `SetRate(axis, rateType, value)` - Set axis jog rates

## System Type Detection

### Detecting CNC System Type
```csharp
cncPipe.system.GetUnlockVersion(out CNCPipe.Sys.UnlockVersions version);

if (version.ToString().Contains("HICKORY"))
{
    // Hickory system
    Console.WriteLine("Hickory CNC detected");
}
else if (version.ToString().Contains("ACORN_SIX"))
{
    // AcornSix system  
    Console.WriteLine("AcornSix CNC detected");
}
else if (version.ToString().Contains("ACORN"))
{
    // Standard Acorn system
    Console.WriteLine("Acorn CNC detected");
}
```

### System-Specific I/O Detection Example
```csharp
if (version.ToString().Contains("HICKORY"))
{
    cncPipe.system.GetECAT1616NumberOfDevices(out int expansions);
    Console.WriteLine($"Hickory with {expansions} ECAT1616 boards");
    Console.WriteLine($"Total I/O: {32 + (expansions * 16)} inputs/outputs");
}
else if (version.ToString().Contains("ACORN_SIX"))
{
    cncPipe.system.GetPLCEXP1616NumberofDevices(out int expansions);
    Console.WriteLine($"AcornSix with {expansions} PLCEXP1616 boards");
    Console.WriteLine($"Total I/O: {16 + (expansions * 16)} inputs/outputs");
}
else if (version.ToString().Contains("ACORN"))
{
    cncPipe.system.GetEther1616DeviceInfo(out List<CNCPipe.Sys.Ether1616Device> devices);
    Console.WriteLine($"Acorn with {devices.Count} Ether1616 boards");
    Console.WriteLine($"Total I/O: {8 + (devices.Count * 16)} inputs/outputs");
}
```

## Global Axis Configuration Settings

Several axis-related settings are global and apply to the entire system rather than individual axes. These settings affect all stepper axes simultaneously and must be configured carefully.

### Global Step Frequency Configuration

The stepper pulse rate (pulses per second) is a **global system setting** that applies to all axes, not individual per-axis configuration.

**Parameter**: 968 (`ACORN_STEPPER_PULSE_RATE_PARM`)

#### Supported Step Frequencies
The system supports these step frequencies for stepper motor control:
- 100,000 steps/second
- 200,000 steps/second (default if parameter is 0)
- 240,000 steps/second
- 300,000 steps/second
- 400,000 steps/second

#### Step Frequency API Usage
```csharp
// Reading current step frequency
cncPipe.parameter.GetMachineParameterValue(968, out double paramValue);

// Calculate actual step frequency
const int PulseStepFrequency = 1200000;  // Base frequency constant
double stepFrequency = paramValue != 0 ? (PulseStepFrequency / paramValue) : 200000;

Console.WriteLine($"Current step frequency: {stepFrequency:N0} steps/second");

// Setting step frequency  
double desiredStepFrequency = 300000;  // 300,000 steps/second
double parameterValue = PulseStepFrequency / desiredStepFrequency;
cncPipe.parameter.SetMachineParameter(968, parameterValue);
```

#### Step Frequency Calculation
The relationship between the parameter value and actual step frequency is:
```
StepFrequency = PulseStepFrequency / ParameterValue
ParameterValue = PulseStepFrequency / StepFrequency

Where PulseStepFrequency = 1,200,000 (constant)
```

#### Examples
```csharp
// For 200,000 steps/second:
// Parameter 968 = 1,200,000 / 200,000 = 6.0

// For 300,000 steps/second:  
// Parameter 968 = 1,200,000 / 300,000 = 4.0

// For 400,000 steps/second:
// Parameter 968 = 1,200,000 / 400,000 = 3.0
```

### Global Axis Signal Inversion
**Parameter**: 961 (`ACORN_OUTPUT_INVERSION_PARM`)

Controls signal inversion for Step, Direction, Enable, and Quadrature signals across all axes. Uses 4-bit nibbles per axis to encode inversion settings.

#### Per-Axis Signal Inversion Configuration

Each axis has individual signal inversion settings that can be configured through the Axis properties:

```csharp
// Configure individual signal inversions for each axis
public void ConfigureAxisSignalInversions(Data data)
{
    // Axis 1 signal inversions
    data.Axis1.IsStepInverted = true;        // Invert step signal
    data.Axis1.IsDirectionInverted = false;  // Normal direction signal
    data.Axis1.IsEnableInverted = false;     // Normal enable signal
    
    // Axis 2 signal inversions
    data.Axis2.IsStepInverted = false;
    data.Axis2.IsDirectionInverted = true;   // Invert direction signal
    data.Axis2.IsEnableInverted = false;
    
    // Axis 3 signal inversions
    data.Axis3.IsStepInverted = false;
    data.Axis3.IsDirectionInverted = false;
    data.Axis3.IsEnableInverted = true;      // Invert enable signal
    
    // Axis 4 signal inversions
    data.Axis4.IsStepInverted = false;
    data.Axis4.IsDirectionInverted = false;
    data.Axis4.IsEnableInverted = false;
    
    // Save all signal inversion changes to Parameter 961
    Axis.SaveAxisSignalInversions();
}
```

#### Signal Inversion Bit Encoding

Parameter 961 uses 4-bit nibbles for each axis, with the following bit positions:

**Bit Encoding Formula**: `BitPosition = 4 * (AxisNumber - 1) + SignalType`

**Signal Type Values**:
- Step Signal: 0
- Direction Signal: 1  
- Enable Signal: 2
- Quadrature Signal: 3

**Bit Positions**:
```
Axis 1: Bits 0-3   (Step=0, Direction=1, Enable=2, Quadrature=3)
Axis 2: Bits 4-7   (Step=4, Direction=5, Enable=6, Quadrature=7)
Axis 3: Bits 8-11  (Step=8, Direction=9, Enable=10, Quadrature=11)
Axis 4: Bits 12-15 (Step=12, Direction=13, Enable=14, Quadrature=15)
```

#### Low-Level Parameter 961 Manipulation

```csharp
// Direct parameter manipulation (not recommended - use Axis properties instead)
cncPipe.parameter.GetMachineParameterValue(961, out double inversionValue);
int axisInversions = (int)inversionValue;

// Calculate specific bit position
int CalculateBitPosition(int axisNumber, int signalType)
{
    return 4 * (axisNumber - 1) + signalType;
}

// Example: Set Axis 2 Direction Signal inversion (bit 5)
int bitPosition = CalculateBitPosition(2, 1); // Axis 2, Direction (1)
axisInversions = GeneralUtils.ModifyBit(axisInversions, bitPosition, true);

// Write back to parameter
cncPipe.parameter.SetMachineParameter(961, axisInversions);
```

#### Recommended High-Level Approach

```csharp
// Load current signal inversions from Parameter 961
Axis.LoadAxisSignalInversions();

// Configure using Axis properties (recommended)
data.Axis1.IsStepInverted = stepInverted;
data.Axis1.IsDirectionInverted = directionInverted;
data.Axis1.IsEnableInverted = enableInverted;
// ... configure other axes ...

// Save all changes to Parameter 961
Axis.SaveAxisSignalInversions();
```

#### Reading Signal Inversion Status

```csharp
// Read individual axis signal inversions
public void ReadAxisSignalInversions(Data data)
{
    // Load current values from Parameter 961
    Axis.LoadAxisSignalInversions();
    
    Console.WriteLine($"Axis 1 - Step: {data.Axis1.IsStepInverted}, " +
                     $"Direction: {data.Axis1.IsDirectionInverted}, " +
                     $"Enable: {data.Axis1.IsEnableInverted}");
                     
    Console.WriteLine($"Axis 2 - Step: {data.Axis2.IsStepInverted}, " +
                     $"Direction: {data.Axis2.IsDirectionInverted}, " +
                     $"Enable: {data.Axis2.IsEnableInverted}");
                     
    // ... continue for other axes ...
}
```

**Important Note**: As shown in the wizard, "Axis Signal Direction Inversion is not the same as changing the direction of the movement of an axis. Use the Axis Configuration selection to change the direction of movement of an axis."

### Global Drive Fault Delay
**Parameter**: 991 (`PLC_CLEARPATH_OR_G540`)

Sets the drive fault timeout delay in milliseconds for all axes. Used for Clearpath servos and G540 drives.

```csharp
// Read current drive fault delay (milliseconds)
cncPipe.parameter.GetMachineParameterValue(991, out double faultDelay);
Console.WriteLine($"Current drive fault delay: {faultDelay:N0} ms");

// Set drive fault delay to 1500ms for all axes
cncPipe.parameter.SetMachineParameter(991, 1500);
```

**Default Value**: 1000 milliseconds

### Global Low Resolution Mode
**Parameter**: 225 (`AD2_LOW_RESOLUTION_PARM`)

Controls plasma low-resolution adjustment mode for the entire system.

```csharp
// This setting is typically managed through the wizard interface
// and affects plasma cutting precision across all axes
```

### Charge Pump Configuration
**Parameter**: 960 (`CHARGE_PUMP_PARM`)

Controls the charge pump frequency divider for systems that require charge pump signals for drive enable functionality. The charge pump provides a safety signal that must be present for drives to operate.

#### Charge Pump Frequency Calculation
The charge pump frequency is calculated using a divider from a base frequency of 1,200,000 Hz:
```
Charge Pump Frequency = 1,200,000 / Divider
```

#### Common Charge Pump Settings
```csharp
// Disable charge pump (set divider to 0)
cncPipe.parameter.SetMachineParameter(960, 0);

// Enable charge pump with 12.5 kHz frequency (divider = 96)
cncPipe.parameter.SetMachineParameter(960, 96);
// Result: 1,200,000 / 96 = 12,500 Hz

// Custom frequency calculation
double desiredFrequency = 10000;  // 10 kHz
double divider = 1200000 / desiredFrequency;
cncPipe.parameter.SetMachineParameter(960, divider);

// Read current charge pump setting
cncPipe.parameter.GetMachineParameterValue(960, out double chargePumpDivider);
if (chargePumpDivider == 0)
{
    Console.WriteLine("Charge pump is disabled");
}
else
{
    double frequency = 1200000 / chargePumpDivider;
    Console.WriteLine($"Charge pump frequency: {frequency:N0} Hz (divider: {chargePumpDivider})");
}
```

#### Charge Pump Usage Notes
- **Divider = 0**: Charge pump disabled (turned off)
- **Default Divider**: 96 (produces 12.5 kHz frequency) when charge pump is enabled
- **Safety Feature**: Many drives require charge pump signal for operation
- **Output Assignment**: Charge pump output must be assigned to a physical output pin in PLC configuration
- **Frequency Range**: Typically 10-15 kHz for most drive systems

### Summary of Global vs Per-Axis Settings

#### Global Settings (Apply to ALL Axes):
- **Step Frequency**: 1200000/Parameter968 (pulses per second)
- **Signal Inversions**: Parameter 961 (4-bit nibbles per axis)
- **Drive Fault Delay**: Parameter 991 (milliseconds)
- **Low Resolution Mode**: Parameter 225 (plasma systems)
- **Charge Pump**: Parameter 960 (frequency divider)

#### Per-Axis Settings:
- **Steps per Revolution**: Individual via `cncPipe.axis.SetCountsPerTurn(axis, value)`
- **Travel Limits**: Individual via `cncPipe.axis.SetTravelLimit(axis, direction, limit)`
- **Jog Rates**: Individual via `cncPipe.axis.SetRate(axis, rateType, value)`
- **Axis Properties**: Individual linear/rotary, reversed, etc.
- **Homing Configuration**: Individual homing methods and order
- **Acceleration Rates**: Individual axis acceleration settings

### Per-Axis Rate Configuration

Individual axes have specific rate settings that are configured through the CentroidAPI axis interface:

#### Maximum Axis Rate
The maximum movement rate for each axis (different from jog rates):
```csharp
// Get maximum axis rate
cncPipe.axis.GetRate(CNCPipe.Axes.AXIS_1, CNCPipe.Axis.Rate.MAX, out double maxRate);

// Set maximum axis rate  
cncPipe.axis.SetRate(CNCPipe.Axes.AXIS_1, CNCPipe.Axis.Rate.MAX, maxRateValue);
```

#### Fast Jog Rates (Directional)
Fast jog rates can be set independently for positive and negative directions:
```csharp
// Fast jog in positive direction
cncPipe.axis.GetRate(CNCPipe.Axes.AXIS_1, CNCPipe.Axis.Rate.FAST_JOG_PLUS, out double fastJogPlusRate);
cncPipe.axis.SetRate(CNCPipe.Axes.AXIS_1, CNCPipe.Axis.Rate.FAST_JOG_PLUS, fastJogPlusValue);

// Fast jog in negative direction  
cncPipe.axis.GetRate(CNCPipe.Axes.AXIS_1, CNCPipe.Axis.Rate.FAST_JOG_MINUS, out double fastJogMinusRate);
cncPipe.axis.SetRate(CNCPipe.Axes.AXIS_1, CNCPipe.Axis.Rate.FAST_JOG_MINUS, fastJogMinusValue);
```

#### Other Available Axis Rates
```csharp
// Slow jog rate
cncPipe.axis.SetRate(CNCPipe.Axes.AXIS_1, CNCPipe.Axis.Rate.SLOW_JOG, slowJogRate);

// Probe-specific jog rates
cncPipe.axis.SetRate(CNCPipe.Axes.AXIS_1, CNCPipe.Axis.Rate.SLOW_JOG_PROBE, probeSlowJogRate);
cncPipe.axis.SetRate(CNCPipe.Axes.AXIS_1, CNCPipe.Axis.Rate.FAST_JOG_PLUS_PROBE, probeFastPlusRate);
cncPipe.axis.SetRate(CNCPipe.Axes.AXIS_1, CNCPipe.Axis.Rate.FAST_JOG_MINUS_PROBE, probeFastMinusRate);
```

### Additional Global Drive Settings

#### Drive Enable Delay (Global Setting)
**Parameter**: 365 (`DRIVE_POWER_ON_DELAY_PARM`)

Sets the drive enable delay in milliseconds for all axes. This is the delay after enabling drive power before motion can begin.

```csharp
// Read current drive enable delay (milliseconds)
cncPipe.parameter.GetMachineParameterValue(365, out double driveEnableDelay);
Console.WriteLine($"Current drive enable delay: {driveEnableDelay:N0} ms");

// Set drive enable delay to 100ms for all axes
cncPipe.parameter.SetMachineParameter(365, 100);
```

**Note**: This is a global setting that applies to all axes, not a per-axis configuration.

### Complete Per-Axis Rate Configuration Example

```csharp
public void ConfigureAxisRates(CNCPipe cncPipe, CNCPipe.Axes axis)
{
    // Set maximum axis rate (feeds and rapids limited to this)
    cncPipe.axis.SetRate(axis, CNCPipe.Axis.Rate.MAX, 200.0);  // 200 IPM max
    
    // Set directional fast jog rates
    cncPipe.axis.SetRate(axis, CNCPipe.Axis.Rate.FAST_JOG_PLUS, 100.0);   // 100 IPM positive
    cncPipe.axis.SetRate(axis, CNCPipe.Axis.Rate.FAST_JOG_MINUS, 100.0);  // 100 IPM negative
    
    // Set slow jog rate
    cncPipe.axis.SetRate(axis, CNCPipe.Axis.Rate.SLOW_JOG, 10.0);  // 10 IPM slow jog
    
    // Set probe-specific jog rates
    cncPipe.axis.SetRate(axis, CNCPipe.Axis.Rate.SLOW_JOG_PROBE, 5.0);           // 5 IPM probe slow
    cncPipe.axis.SetRate(axis, CNCPipe.Axis.Rate.FAST_JOG_PLUS_PROBE, 25.0);     // 25 IPM probe fast+
    cncPipe.axis.SetRate(axis, CNCPipe.Axis.Rate.FAST_JOG_MINUS_PROBE, 25.0);    // 25 IPM probe fast-
}

// Configure global drive settings
public void ConfigureGlobalDriveSettings(CNCPipe cncPipe)
{
    // Set global drive enable delay
    cncPipe.parameter.SetMachineParameter(365, 100);  // 100ms drive enable delay
    
    // Set global drive fault delay (Parameter 991)
    cncPipe.parameter.SetMachineParameter(991, 1000); // 1000ms drive fault delay
}
```

### Global Settings API Examples

```csharp
// Configure global axis settings
public void ConfigureGlobalAxisSettings(CNCPipe cncPipe)
{
    // Set 300,000 steps/second for all axes
    double stepFreqParam = 1200000.0 / 300000.0;
    cncPipe.parameter.SetMachineParameter(968, stepFreqParam);
    
    // Set 1500ms drive fault delay for all axes
    cncPipe.parameter.SetMachineParameter(991, 1500);
    
    // Read current signal inversion settings
    cncPipe.parameter.GetMachineParameterValue(961, out double inversions);
    Console.WriteLine($"Current axis signal inversions: {(int)inversions:X}");
}

// Read all global axis settings
public void ReadGlobalAxisSettings(CNCPipe cncPipe)
{
    // Step frequency
    cncPipe.parameter.GetMachineParameterValue(968, out double stepParam);
    double stepFreq = stepParam != 0 ? (1200000 / stepParam) : 200000;
    
    // Drive fault delay
    cncPipe.parameter.GetMachineParameterValue(991, out double faultDelay);
    
    // Signal inversions
    cncPipe.parameter.GetMachineParameterValue(961, out double inversions);
    
    Console.WriteLine($"Global Settings:");
    Console.WriteLine($"  Step Frequency: {stepFreq:N0} steps/second");
    Console.WriteLine($"  Drive Fault Delay: {faultDelay:N0} ms");
    Console.WriteLine($"  Signal Inversions: 0x{(int)inversions:X}");
}
```

### Important Notes About Global Settings

1. **System-Wide Impact**: All global settings affect every axis simultaneously
2. **Hardware Compatibility**: Settings must be compatible with all connected drives
3. **Validation**: CNC12 firmware validates settings and may revert to defaults for invalid values
4. **Coordination**: Changes to global settings should be coordinated across the entire machine setup
5. **Backup**: Always backup current settings before making changes to global parameters

## Additional CentroidAPI Configuration Areas

Beyond axis configuration, the CentroidAPI provides access to many other configuration areas discovered in the Centroid Wizard codebase:

### Spindle Configuration Settings

#### Spindle Encoder Settings
```csharp
// Spindle encoder configuration
cncPipe.parameter.GetMachineParameterValue(34, out double encoderCounts); // SPINDLE_COUNTS_REV_PARM
cncPipe.parameter.SetMachineParameter(34, encoderCountsValue);

// Spindle parameter bits (Parameter 78)
cncPipe.parameter.GetMachineParameterValue(78, out double spindleParam);
bool encoderEnabled = GeneralUtils.IsBitSet((int)spindleParam, 0);
bool scalingEnabled = GeneralUtils.IsBitSet((int)spindleParam, 4);
```

#### Comprehensive Spindle Parameter Reference

Based on the Centroid Wizard spindle configuration, here are the complete parameter numbers and settings:

##### Spindle Encoder Configuration

**Spindle Encoder Enable** - Boolean "No"/"Yes"
- **Parameter**: 78 (`SPINDLE_PARM`) - Bit 0
- **API**: 
```csharp
// Read spindle encoder enable status
cncPipe.parameter.GetMachineParameterValue(78, out double spindleParam);
bool encoderEnabled = GeneralUtils.IsBitSet((int)spindleParam, 0);

// Set spindle encoder enable
int spindleParm = (int)spindleParam;
spindleParm = GeneralUtils.ModifyBit(spindleParm, 0, true);  // Enable
spindleParm = GeneralUtils.ModifyBit(spindleParm, 0, false); // Disable
cncPipe.parameter.SetMachineParameter(78, spindleParm);
```

**Spindle Encoder Port Selection**
- **Parameter**: 315 (`AXIS_8_ENCODER_INDEX_PARM`)
- **Values**: 1, 2, or 3 (Encoder port #1, #2, or #3)
- **API**: 
```csharp
// Set to Encoder port #1
cncPipe.parameter.SetMachineParameter(315, 1);
```

**Spindle Encoder Counts** - Number (e.g., 8000)
- **Parameter**: 34 (`SPINDLE_COUNTS_REV_PARM`)
- **API**: 
```csharp
// Set spindle encoder counts to 8000
cncPipe.parameter.SetMachineParameter(34, 8000);
```

##### Spindle Speed Range Configuration

**Spindle Max Speed in High Range** - RPM (e.g., 24000)
- **API Method**: State API (not a direct parameter)
```csharp
// Set maximum spindle speed
cncPipe.state.SetHighRangeSpindleSpeed(CNCPipe.State.Value.MAX, 24000);

// Read maximum spindle speed
cncPipe.state.GetHighRangeSpindleSpeed(CNCPipe.State.Value.MAX, out double maxSpeed);
```

**Spindle Min Speed in High Range** - RPM (e.g., 3000)
- **API Method**: State API (not a direct parameter)
```csharp
// Set minimum spindle speed
cncPipe.state.SetHighRangeSpindleSpeed(CNCPipe.State.Value.MIN, 3000);

// Read minimum spindle speed
cncPipe.state.GetHighRangeSpindleSpeed(CNCPipe.State.Value.MIN, out double minSpeed);
```

**Medium Range Spindle Speed Ratio** - Ratio (e.g., 1)
- **Parameter**: 66 (`MED_LOW_GEAR_RATIO_PARM`)
- **API**: 
```csharp
// Set medium gear ratio
cncPipe.parameter.SetMachineParameter(66, 1.0);
```

**Low Range Spindle Speed Ratio** - Ratio (e.g., 1)
- **Parameter**: 65 (`LOW_GEAR_RATIO_PARM`)
- **API**: 
```csharp
// Set low gear ratio
cncPipe.parameter.SetMachineParameter(65, 1.0);
```

##### Spindle Analog Output Configuration

**Limit Spindle Analog Output to 0-5 Volts** - Boolean "No"/"Yes"
- **Parameter**: 420 (`PLC_ANALOG_PARM`)
- **API**: 
```csharp
// For standard Acorn systems (boolean)
cncPipe.parameter.SetMachineParameter(420, 1);  // Yes (0-5V)
cncPipe.parameter.SetMachineParameter(420, 0);  // No (0-10V)

// For AcornSix/Hickory systems (range selection)
// 0 = 0 to +10VDC, 1 = 0 to +5VDC, 2 = -5 to +5VDC, 3 = -10 to +10VDC
cncPipe.parameter.SetMachineParameter(420, 1);  // 0-5V range
```

##### Spindle Display and Control Settings

**RTG Spindle Speed RPM Display** - Dropdown mode selection
- **Parameter**: 430 (`RTG_SPINDLE_DISPLAY_PARM`)
- **Values**: 
  - 0 = Actual Encoder Spindle Speed
  - 1 = G-code program or RPM sensor Spindle Speed  
  - 2 = Both
- **API**: 
```csharp
// Set RTG display mode
cncPipe.parameter.SetMachineParameter(430, 0);  // Actual Encoder Speed
cncPipe.parameter.SetMachineParameter(430, 1);  // G-code/RPM sensor Speed
cncPipe.parameter.SetMachineParameter(430, 2);  // Both
```

**SpindleOK Delay Timer** - Milliseconds (e.g., 0)
- **Parameter**: 996 (`SPINDLE_OK_DELAY_PARM`)
- **API**: 
```csharp
// Set SpindleOK delay to 0 milliseconds
cncPipe.parameter.SetMachineParameter(996, 0);
```

**Spindle Cooling Fan Delay Timer** - Seconds (e.g., 0)
- **Parameter**: 997 (`SPINDLE_COOLING_FAN_DELAY_TIMER`)
- **API**: 
```csharp
// Set cooling fan delay to 0 seconds
cncPipe.parameter.SetMachineParameter(997, 0);
```

**Spindle Scaling Enabled** - Boolean "No"/"Yes"
- **Parameter**: 78 (`SPINDLE_PARM`) - Bit 4
- **API**: 
```csharp
// Read current spindle parameter
cncPipe.parameter.GetMachineParameterValue(78, out double spindleParam);
bool scalingEnabled = GeneralUtils.IsBitSet((int)spindleParam, 4);

// Set spindle scaling enable
int spindleParm = (int)spindleParam;
spindleParm = GeneralUtils.ModifyBit(spindleParm, 4, true);  // Enable scaling
spindleParm = GeneralUtils.ModifyBit(spindleParm, 4, false); // Disable scaling
cncPipe.parameter.SetMachineParameter(78, spindleParm);
```

##### Spindle Variation Settings

**Spindle Speed Variation Cycle Time** - Seconds (e.g., 0)
- **Parameter**: 982 (`SSV_CYCLE_TIME`)
- **API**: 
```csharp
// Set speed variation cycle time to 0 seconds
cncPipe.parameter.SetMachineParameter(982, 0);
```

**Amount of Speed Variation** - RPM (+/-) (e.g., 0)
- **Parameter**: 983 (`SSV_AMOUNT`)
- **API**: 
```csharp
// Set speed variation amount to 0 RPM
cncPipe.parameter.SetMachineParameter(983, 0);
```

**Feed Rate Variation Cycle Time** - Milliseconds (e.g., 0)
- **Parameter**: 984 (`FRV_CYCLE_TIME`)
- **API**: 
```csharp
// Set feed rate variation cycle time to 0 milliseconds
cncPipe.parameter.SetMachineParameter(984, 0);
```

#### Complete Spindle Configuration Example

```csharp
public void ConfigureSpindleSettings(CNCPipe cncPipe)
{
    // 1. Spindle Encoder Configuration
    cncPipe.parameter.SetMachineParameter(315, 1);     // Encoder port #1
    cncPipe.parameter.SetMachineParameter(34, 8000);   // 8000 encoder counts
    
    // Enable spindle encoder (Parameter 78, Bit 0)
    cncPipe.parameter.GetMachineParameterValue(78, out double spindleParam);
    int spindleParm = (int)spindleParam;
    spindleParm = GeneralUtils.ModifyBit(spindleParm, 0, true);  // Enable encoder
    spindleParm = GeneralUtils.ModifyBit(spindleParm, 4, false); // Disable scaling
    cncPipe.parameter.SetMachineParameter(78, spindleParm);
    
    // 2. Spindle Speed Ranges
    cncPipe.state.SetHighRangeSpindleSpeed(CNCPipe.State.Value.MAX, 24000); // Max 24000 RPM
    cncPipe.state.SetHighRangeSpindleSpeed(CNCPipe.State.Value.MIN, 3000);  // Min 3000 RPM
    cncPipe.parameter.SetMachineParameter(66, 1.0);  // Medium ratio = 1
    cncPipe.parameter.SetMachineParameter(65, 1.0);  // Low ratio = 1
    
    // 3. Analog Output Configuration
    cncPipe.parameter.SetMachineParameter(420, 0);   // No (0-10V output)
    
    // 4. Display and Control Settings
    cncPipe.parameter.SetMachineParameter(430, 1);   // G-code/RPM sensor display
    cncPipe.parameter.SetMachineParameter(996, 0);   // 0ms SpindleOK delay
    cncPipe.parameter.SetMachineParameter(997, 0);   // 0 seconds cooling fan delay
    
    // 5. Variation Settings (all disabled)
    cncPipe.parameter.SetMachineParameter(982, 0);   // 0 seconds speed variation cycle
    cncPipe.parameter.SetMachineParameter(983, 0);   // 0 RPM speed variation amount
    cncPipe.parameter.SetMachineParameter(984, 0);   // 0ms feed rate variation cycle
}
```

#### Summary of Spindle Parameter Numbers

| Setting | Parameter | Type | Description |
|---------|-----------|------|-------------|
| **Spindle Encoder Enable** | 78 (bit 0) | Boolean | Enable/disable spindle encoder |
| **Encoder Port Selection** | 315 | Value (1-3) | Which encoder port to use |
| **Encoder Counts** | 34 | Value | Encoder pulses per revolution |
| **Max Speed** | State API | Value (RPM) | Maximum spindle speed |
| **Min Speed** | State API | Value (RPM) | Minimum spindle speed |
| **Medium Ratio** | 66 | Ratio | Medium gear ratio |
| **Low Ratio** | 65 | Ratio | Low gear ratio |
| **Limit to 0-5V** | 420 | Boolean/Value | Analog output voltage range |
| **RTG Display Mode** | 430 | Value (0-2) | RPM display source selection |
| **SpindleOK Delay** | 996 | Value (ms) | Spindle OK signal delay |
| **Cooling Fan Delay** | 997 | Value (sec) | Fan delay after spindle stop |
| **Scaling Enable** | 78 (bit 4) | Boolean | Enable/disable spindle scaling |
| **Speed Variation Cycle** | 982 | Value (sec) | Speed variation cycle time |
| **Speed Variation Amount** | 983 | Value (RPM) | Speed variation amount |
| **Feed Variation Cycle** | 984 | Value (ms) | Feed rate variation cycle time |
| **Spindle Accel/Decel Time** | ❌ Not Found | Value (ms) | Spindle acceleration/deceleration timing |

## Additional Rigid Tapping and Advanced Spindle Settings

The CentroidAPI also supports advanced rigid tapping and spindle configuration parameters not covered in the basic spindle setup above. These parameters control specialized tapping operations and spindle behavior.

### Additional Spindle and Tapping Parameters

| Setting | Parameter | Description | API Example |
|---------|-----------|-------------|-------------|
| **Spindle Drift (Degrees)** | 82 | Spindle cutoff drift tolerance in degrees for rigid tapping | `cncPipe.parameter.SetMachineParameter(82, 2.5)` |
| **Spindle Accel/Decel Time** | ❌ Not Found | Spindle acceleration/deceleration time in seconds | Parameter not found in current system |
| **M Func To Run At Bottom Of Hole G84 Tapping** | ❌ Not Found | M function executed at bottom of G84 (right-hand) tapping cycle | Parameter not found in current system |
| **M Func To Run At Top Of Hole For G84 Counter Tapping** | ❌ Not Found | M function executed at top of hole for G84 counter tapping | Parameter not found in current system |
| **M Func To Run At Bottom Of Hole G74 Tapping (Left Hand)** | ❌ Not Found | M function executed at bottom of G74 (left-hand) tapping cycle | Parameter not found in current system |
| **M Func To Run At Top Of Hole For G74 Counter Tapping** | ❌ Not Found | M function executed at top of hole for G74 counter tapping | Parameter not found in current system |
| **Rigid Tapping Z Axis Sync Distance** | 241 | Z-axis synchronization distance for rigid tapping in rotational degrees | `cncPipe.parameter.SetMachineParameter(241, 360.0)` |
| **Allow Spindle Override** | 36 (bit 2) | Bit 2 in rigid tapping parameter enables spindle override during tapping | See below |
| **Do Not Wait For Index Pulse** | 36 (bit 1) | Bit 1 in rigid tapping parameter disables index pulse wait | See below |

### Rigid Tapping Parameter Bit Configuration (Parameter 36)

The rigid tapping parameter uses bit encoding for multiple settings:

```csharp
// Read current rigid tapping configuration
cncPipe.parameter.GetMachineParameterValue(36, out double rigidTappingBits);

// Enable/disable specific features using bit manipulation
// Bit 0: Enable rigid tapping (1 = enabled, 0 = disabled)
// Bit 1: Do not wait for index pulse (1 = don't wait, 0 = wait)
// Bit 2: Allow spindle override (1 = allowed, 0 = not allowed)

// Enable rigid tapping with spindle override allowed but wait for index pulse
int newValue = 0;
newValue |= (1 << 0);  // Enable rigid tapping (bit 0)
newValue |= (0 << 1);  // Wait for index pulse (bit 1 = 0)
newValue |= (1 << 2);  // Allow spindle override (bit 2 = 1)

cncPipe.parameter.SetMachineParameter(36, newValue);

// Or use bit manipulation helper functions if available
bool allowSpindleOverride = true;
bool doNotWaitForIndexPulse = false;
bool rigidTappingEnabled = true;

cncPipe.parameter.GetMachineParameterValue(36, out double currentValue);
currentValue = ModifyBit((int)currentValue, 0, rigidTappingEnabled);
currentValue = ModifyBit((int)currentValue, 1, doNotWaitForIndexPulse);
currentValue = ModifyBit((int)currentValue, 2, allowSpindleOverride);
cncPipe.parameter.SetMachineParameter(36, currentValue);
```

### Notes on Tapping M Functions

- **G84 Tapping (Right-Hand)**: Uses parameter 74 for M function at bottom of hole
- **G74 Tapping (Left-Hand)**: Uses parameter 84 for M function at bottom of hole  
- **Top of Hole M Functions**: The system appears to use standard M3/M4 spindle start commands at the top of tapping cycles rather than separate configurable M functions
- **Counter Tapping**: Uses the opposite spindle direction M functions (G74 for counter-tapping G84 operations)

#### Spindle Speed Ranges and Gear Ratios
```csharp
// High range spindle speed limits (State API)
cncPipe.state.SetHighRangeSpindleSpeed(CNCPipe.State.Value.MAX, maxSpeed);
cncPipe.state.SetHighRangeSpindleSpeed(CNCPipe.State.Value.MIN, minSpeed);

// Gear ratios
cncPipe.parameter.SetMachineParameter(65, lowGearRatio);    // LOW_GEAR_RATIO_PARM
cncPipe.parameter.SetMachineParameter(66, mediumGearRatio); // MED_LOW_GEAR_RATIO_PARM
```

#### Rigid Tapping Configuration
```csharp
// Rigid tapping enable/disable and options (Parameter 36)
cncPipe.parameter.GetMachineParameterValue(36, out double rigidTappingParam);
bool rigidTappingEnabled = rigidTappingParam != 0;
bool doNotWaitForIndex = GeneralUtils.IsBitSet((int)rigidTappingParam, 1);
bool allowSpindleOverride = GeneralUtils.IsBitSet((int)rigidTappingParam, 2);

// Rigid tapping speeds and distances
cncPipe.parameter.SetMachineParameter(68, minimumRpmForTapping);  // RT_SLOW_SPINDLE_SPEED_PARM
cncPipe.parameter.SetMachineParameter(69, slowSpindleTime);       // RT_SLOW_SPINDLE_TIME_PARM
```

### PWM and Laser Configuration

#### PWM Output Settings
```csharp
// PWM options (Acorn systems)
cncPipe.parameter.GetMachineParameterValue(969, out double pwmOptions); // ACORN_PWM_OPTIONS_PARM
bool pwmInverted = GeneralUtils.IsBitSet((int)pwmOptions, 0);
bool velocity100Mode = GeneralUtils.IsBitSet((int)pwmOptions, 1);

// PWM frequency and floor settings
cncPipe.parameter.SetMachineParameter(970, pwmFrequency); // ACORN_PWM_FREQUENCY_PARM
cncPipe.parameter.SetMachineParameter(971, pwmFloor);     // ACORN_PWM_FLOOR_PARM

// Laser cooling fan delay
cncPipe.parameter.SetMachineParameter(972, fanDelayMs);   // LASER_COOLING_FAN_DELAY_TIMER
```

### Probing Configuration

#### Probe Settings and Types
```csharp
// Probe input type and settings
cncPipe.parameter.SetMachineParameter(11, probeInputState); // PROBE_NUMBER_AND_STATE_PARM
cncPipe.parameter.SetMachineParameter(12, probeToolNumber); // PROBE_TOOL_NUMBER_PARM

// Probe rates and distances
cncPipe.parameter.SetMachineParameter(14, fastProbeRate);  // FAST_PROBING_RATE_PARM
cncPipe.parameter.SetMachineParameter(15, slowProbeRate);  // SLOW_PROBING_RATE_PARM
cncPipe.parameter.SetMachineParameter(16, maxSearchDistance); // PROBING_MAX_SEARCH_DISTANCE_PARM
cncPipe.parameter.SetMachineParameter(13, recoveryDistance);  // PROBING_RECOVERY_DISTANCE_PARM

// Probe protection and warnings
cncPipe.parameter.SetMachineParameter(153, probeProtection); // PROBE_PROTECTION_PARM
cncPipe.parameter.SetMachineParameter(899, probeWarning);    // DISPLAY_PROBE_WARNING_PARAM
```

#### Per-Axis Probe Jog Rates
```csharp
// Set probe-specific jog rates for each axis
cncPipe.axis.SetRate(CNCPipe.Axes.AXIS_1, CNCPipe.Axis.Rate.SLOW_JOG_PROBE, slowJogRate);
cncPipe.axis.SetRate(CNCPipe.Axes.AXIS_1, CNCPipe.Axis.Rate.FAST_JOG_MINUS_PROBE, fastMinusRate);
cncPipe.axis.SetRate(CNCPipe.Axes.AXIS_1, CNCPipe.Axis.Rate.FAST_JOG_PLUS_PROBE, fastPlusRate);
```

### Control Panel and Interface Settings

#### VCP (Virtual Control Panel) Configuration
```csharp
// Rapid override and jogging options
cncPipe.parameter.GetMachineParameterValue(56, out double rapidOverrideParam); // ENABLE_RAPID_OVERRIDE_PARM
bool rapidOverrideEnabled = GeneralUtils.IsBitSet((int)rapidOverrideParam, 0) && 
                           GeneralUtils.IsBitSet((int)rapidOverrideParam, 1);
bool rapidFeedLinkEnabled = GeneralUtils.IsBitSet((int)rapidOverrideParam, 2);

// Jogging startup options
cncPipe.parameter.GetMachineParameterValue(148, out double joggingOptions); // JOGGING_OPTIONS
bool continuousJogOnStart = GeneralUtils.IsBitSet((int)joggingOptions, 0);
bool fastJogOnStart = GeneralUtils.IsBitSet((int)joggingOptions, 1);

// Console type setting (State API)
cncPipe.state.SetConsoleType(CNCPipe.State.ConsoleTypes.VIRTUAL);           // VCP only
cncPipe.state.SetConsoleType(CNCPipe.State.ConsoleTypes.JOGBOARD);          // Jog panel only
cncPipe.state.SetConsoleType(CNCPipe.State.ConsoleTypes.JOGBOARD_WITH_VCP); // Both
```

#### Operator Control Panel Settings
```csharp
// USB panel knob multipliers
cncPipe.parameter.SetMachineParameter(580, feedKnobMultiplier);   // EXT_USB_FEED_KNOB_MULTIPLIER
cncPipe.parameter.SetMachineParameter(581, spindleKnobMultiplier); // EXT_USB_SPINDLE_KNOB_MULTIPLIER
cncPipe.parameter.SetMachineParameter(582, rapidKnobMultiplier);   // EXT_USB_RAPID_KNOB_MULTIPLIER

// Custom knob rates
cncPipe.parameter.SetMachineParameter(583, feedCustomRate);     // EXT_USB_FEED_KNOB_CUSTOM_RATE
cncPipe.parameter.SetMachineParameter(584, spindleCustomRate);  // EXT_USB_SPINDLE_KNOB_CUSTOM_RATE
cncPipe.parameter.SetMachineParameter(585, rapidCustomRate);   // EXT_USB_RAPID_KNOB_CUSTOM_RATE
```

### ATC (Automatic Tool Changer) Configuration

#### Basic ATC Settings
```csharp
// Tool changer type and features
cncPipe.parameter.SetMachineParameter(6, toolChangerType);    // TOOL_CHANGER_INSTALLED_PARM
cncPipe.parameter.SetMachineParameter(160, enhancedATCMode); // ENHANCED_ATC_PARM
cncPipe.parameter.SetMachineParameter(161, maxBins);         // ATC_MAX_BINS_PARM
cncPipe.parameter.SetMachineParameter(164, atcFeatures);     // ATC_FEATURE_PARM
```

### Auxiliary Functions

#### Auxiliary Key Programming
```csharp
// Auxiliary key functions (Parameters 188-199)
for (int auxKey = 1; auxKey <= 12; auxKey++)
{
    int paramNumber = 187 + auxKey; // AUX_KEY_FUNC_BASE_PARM + offset
    cncPipe.parameter.SetMachineParameter(paramNumber, auxKeyFunction);
}
```

#### Lube Pump Configuration
```csharp
// Lube pump settings
cncPipe.parameter.SetMachineParameter(179, lubePumpOptions); // LUBE_PUMP_PARM
```

### Touch Plate and Tool Measurement

#### Touch Plate Configuration
```csharp
// Touch plate dimensions and settings (typically stored in wizard settings)
// These are used to calculate touch plate operations but may not directly
// correspond to CNC12 parameters as they're wizard-specific configurations

// Touch plate input state
cncPipe.parameter.SetMachineParameter(11, touchPlateInputType); // Shares with probe input
```

#### Tool Touch-Off Settings

Tool touch-off (TT) configuration manages automatic tool measurement and length compensation. The system supports both fixed position and moveable touch-off devices.

##### Core Tool Touch-Off Parameters

| Parameter | Purpose | Description | Values |
|-----------|---------|-------------|--------|
| 44 (Mill) / 244 (Lathe) | Touch Off Tool PLC Input | PLC input for tool touch off triggered signal | Input number |
| 257 | Tool Touch Off Detect Input | PLC input for touch off detection/protection | Input number |
| 405 | Tool Touch Off Type | Mechanical type configuration | 0=Standard, 1=Enhanced |
| 407 | Tool Touch Off Input Type | Expected input state | 0=NO, 1=NC |
| 71 | Tool Touch Off Height | Height of touch-off device (negative value) | Height in machine units |
| 17 | Fixed Location Mode | Use fixed location for tool touch off | 0=Moveable, 3=Fixed |
| 43 | Tool Measure Properties | Bit field for measurement options | Bit flags (Mill only) |
| 3 (bit 1) | Height Calculation Method | Tool measurement reference method | Bit 1 in modal tool parameter |

##### Workpiece Reference Points (Fixed Location Coordinates)
- **G30 P3 X**: Tool touch off X coordinate 
- **G30 P3 Y**: Tool touch off Y coordinate
- **G30 P3 Z**: Tool touch off Z coordinate

##### Detailed Tool Touch-Off Configuration

**Touch Off Tool PLC Input Assignment**
```csharp
// Mill systems use parameter 44, Lathe systems use parameter 244
int paramNum = isMill ? 44 : 244;

// Set tool touch off input to input 15
cncPipe.parameter.SetMachineParameter(paramNum, 15);

// Read current tool touch off input assignment
cncPipe.parameter.GetMachineParameterValue(paramNum, out double ttInput);
int toolTouchInput = (int)ttInput;
```

**Tool Touch Off Detection Input** - Parameter 257
```csharp
// Set detection input to input 16 for protection/monitoring
cncPipe.parameter.SetMachineParameter(257, 16);

// Disable detection input
cncPipe.parameter.SetMachineParameter(257, 0);

// Read detection input assignment
cncPipe.parameter.GetMachineParameterValue(257, out double detectInput);
```

**Tool Touch Off Type** - Parameter 405
```csharp
// Set to standard tool touch off type
cncPipe.parameter.SetMachineParameter(405, 0);

// Set to enhanced tool touch off type
cncPipe.parameter.SetMachineParameter(405, 1);

// Read tool touch off type
cncPipe.parameter.GetMachineParameterValue(405, out double ttType);
```

**Tool Touch Off Input State** - Parameter 407
```csharp
// Set normally open input type
cncPipe.parameter.SetMachineParameter(407, 0);

// Set normally closed input type
cncPipe.parameter.SetMachineParameter(407, 1);

// Read input type
cncPipe.parameter.GetMachineParameterValue(407, out double inputType);
bool isNormallyClosed = inputType == 1;
```

**Tool Touch Off Height** - Parameter 71
```csharp
// Set tool touch off device height to 2.0 inches
// Note: Parameter 71 stores negative value, so 2.0" becomes -2.0
cncPipe.parameter.SetMachineParameter(71, -2.0);

// Read tool touch off height
cncPipe.parameter.GetMachineParameterValue(71, out double height);
double actualHeight = height * -1;  // Convert back to positive value
```

**Fixed Location Mode** - Parameter 17
```csharp
// Enable fixed location mode (use G30 P3 coordinates)
cncPipe.parameter.SetMachineParameter(17, 3);

// Disable fixed location mode (moveable touch-off device)
cncPipe.parameter.SetMachineParameter(17, 0);

// Read fixed location mode status
cncPipe.parameter.GetMachineParameterValue(17, out double fixedMode);
bool isFixedLocation = fixedMode == 3;
```

**Fixed Location Coordinates** - G30 P3 Reference Points
```csharp
// Set fixed tool touch off coordinates
cncPipe.SetWorkpieceReference(3, 1, 5.0);    // X = 5.0"
cncPipe.SetWorkpieceReference(3, 2, 3.0);    // Y = 3.0"
cncPipe.SetWorkpieceReference(3, 3, -1.0);   // Z = -1.0"

// Read fixed coordinates
cncPipe.GetWorkpieceReference(3, 1, out double xPos);
cncPipe.GetWorkpieceReference(3, 2, out double yPos);
cncPipe.GetWorkpieceReference(3, 3, out double zPos);
```

**Height Calculation Method** - Parameter 3, Bit 1
```csharp
// Read current modal tool parameter
cncPipe.parameter.GetMachineParameterValue(3, out double modalToolParam);
int toolParam = (int)modalToolParam;

// Check current height calculation method
bool toolMeasurementReference = GeneralUtils.IsBitSet(toolParam, 1);

// Set height calculation method
toolParam = GeneralUtils.ModifyBit(toolParam, 1, true);   // Use reference method
toolParam = GeneralUtils.ModifyBit(toolParam, 1, false);  // Use standard method
cncPipe.parameter.SetMachineParameter(3, toolParam);
```

**Tool Measure Properties (Mill Only)** - Parameter 43
```csharp
// This parameter uses bit fields for various tool measurement options
cncPipe.parameter.GetMachineParameterValue(43, out double measureProps);
int props = (int)measureProps;

// Bit 0: Subtract TT Device Height
bool subtractHeight = GeneralUtils.IsBitSet(props, 0);
props = GeneralUtils.ModifyBit(props, 0, true);  // Enable height subtraction

// Bit 1: Use TT Device for Z Reference
bool useForZRef = GeneralUtils.IsBitSet(props, 1);
props = GeneralUtils.ModifyBit(props, 1, false); // Disable Z reference use

// Save modified properties
cncPipe.parameter.SetMachineParameter(43, props);
```

##### Tool Touch-Off Protection and Warning Settings

The tool touch-off system shares protection parameters with the probe system:

**Warning Display Enable** - Parameter 410 (shared with probe)
```csharp
// Enable tool touch-off warning displays
cncPipe.parameter.SetMachineParameter(410, 1);

// Disable warning displays
cncPipe.parameter.SetMachineParameter(410, 0);
```

**Tool Touch-Off Protection** - Parameter 153 (shared with probe)
```csharp
// Enable tool touch-off protection features
cncPipe.parameter.SetMachineParameter(153, 1);

// Disable protection features
cncPipe.parameter.SetMachineParameter(153, 0);
```

**Spindle Inhibit During Tool Touch** - Parameter 416 (shared with probe)
```csharp
// Enable spindle inhibit during tool touch operations
cncPipe.parameter.SetMachineParameter(416, 2);  // Tool touch inhibit mode

// Read spindle inhibit setting
cncPipe.parameter.GetMachineParameterValue(416, out double inhibit);
bool toolTouchInhibit = (inhibit == 2);
```

##### Complete Tool Touch-Off Configuration Example

```csharp
public void ConfigureToolTouchOff(CNCPipe cncPipe, bool isMill)
{
    // 1. Set PLC input assignments
    int ttInputParam = isMill ? 44 : 244;
    cncPipe.parameter.SetMachineParameter(ttInputParam, 15);  // Tool touch input = Input 15
    cncPipe.parameter.SetMachineParameter(257, 16);           // Detection input = Input 16
    
    // 2. Configure input type and device type
    cncPipe.parameter.SetMachineParameter(407, 1);  // Normally closed input
    cncPipe.parameter.SetMachineParameter(405, 0);  // Standard tool touch off type
    
    // 3. Set tool touch off height (2.0" device height)
    cncPipe.parameter.SetMachineParameter(71, -2.0);  // Negative value for parameter 71
    
    // 4. Configure fixed location mode and coordinates
    cncPipe.parameter.SetMachineParameter(17, 3);    // Enable fixed location mode
    cncPipe.SetWorkpieceReference(3, 1, 12.0);       // X = 12.0"
    cncPipe.SetWorkpieceReference(3, 2, 6.0);        // Y = 6.0"
    cncPipe.SetWorkpieceReference(3, 3, -1.0);       // Z = -1.0"
    
    // 5. Set height calculation method
    cncPipe.parameter.GetMachineParameterValue(3, out double modalToolParam);
    int toolParam = GeneralUtils.ModifyBit((int)modalToolParam, 1, true);
    cncPipe.parameter.SetMachineParameter(3, toolParam);
    
    // 6. Configure protection and warning settings
    cncPipe.parameter.SetMachineParameter(410, 1);  // Enable warnings
    cncPipe.parameter.SetMachineParameter(153, 1);  // Enable protection
    cncPipe.parameter.SetMachineParameter(416, 2);  // Enable spindle inhibit
    
    // 7. Mill-specific tool measure properties
    if (isMill)
    {
        cncPipe.parameter.GetMachineParameterValue(43, out double measureProps);
        int props = (int)measureProps;
        props = GeneralUtils.ModifyBit(props, 0, true);   // Subtract device height
        props = GeneralUtils.ModifyBit(props, 1, false);  // Don't use for Z ref
        cncPipe.parameter.SetMachineParameter(43, props);
    }
}
```

##### Summary of Tool Touch-Off Parameters

| Setting | Parameter | Mill/Lathe | Description |
|---------|-----------|------------|-------------|
| **PLC Input Assignment** | 44 / 244 | Mill / Lathe | Physical input for tool touch signal |
| **Detection Input** | 257 | Both | Optional detection/protection input |
| **Input Type** | 407 | Both | NO(0) or NC(1) input configuration |
| **Device Type** | 405 | Both | Standard(0) or Enhanced(1) device |
| **Device Height** | 71 | Both | Height in machine units (stored negative) |
| **Fixed Location Mode** | 17 | Both | Moveable(0) or Fixed(3) location |
| **Fixed X Position** | G30 P3 X | Both | X coordinate for fixed location |
| **Fixed Y Position** | G30 P3 Y | Both | Y coordinate for fixed location |
| **Fixed Z Position** | G30 P3 Z | Both | Z coordinate for fixed location |
| **Height Calculation** | 3 (bit 1) | Both | Tool measurement reference method |
| **Measure Properties** | 43 | Mill only | Bit field for measurement options |
| **Warning Display** | 410 | Both | Enable/disable warning displays |
| **Protection Enable** | 153 | Both | Enable/disable protection features |
| **Spindle Inhibit** | 416 | Both | Spindle behavior during touch operations |

### System Preferences and Display Options

#### CNC Control Preferences
```csharp
// Various display and control preferences
cncPipe.parameter.SetMachineParameter(7, colorScheme);     // COLOR_SCHEME_PARM
cncPipe.parameter.SetMachineParameter(9, languageOption); // LANGUAGE_PARM
cncPipe.parameter.SetMachineParameter(113, hideMenus);    // HIDE_MENUS_PARM
```

### Hardware-Specific Settings

#### MPG (Manual Pulse Generator) Configuration
```csharp
// Hardwired MPG axis assignments
for (int mpgIndex = 0; mpgIndex < 6; mpgIndex++)
{
    int axisParam = 530 + mpgIndex;      // HARDWIRED_MPG_4_AXIS base
    int encoderParam = 533 + (mpgIndex * 3); // MPG_4_ENCODER_INPUT base
    
    cncPipe.parameter.SetMachineParameter(axisParam, selectedAxis);
    cncPipe.parameter.SetMachineParameter(encoderParam, encoderInput);
}
```

#### Scale and Rotary Input Configuration
```csharp
// Scale input settings for measurement devices
// Rotary axis configuration for 4th/5th axis setups
// These involve complex parameter relationships specific to rotary operations
```

## Rotary Axis Configuration Settings

The Centroid system provides comprehensive rotary axis configuration through several parameters and axis properties:

### Rotary Axis Selection

#### Which Axis is Configured as Rotary
Each axis can be individually configured as linear or rotary through axis properties:

```csharp
// Configure axis as rotary or linear (per-axis setting)
data.Axis1.LinearOrRotary = AxisMotionType.Rotary;   // Set axis 1 as rotary
data.Axis2.LinearOrRotary = AxisMotionType.Linear;   // Set axis 2 as linear
data.Axis3.LinearOrRotary = AxisMotionType.Linear;   // Set axis 3 as linear  
data.Axis4.LinearOrRotary = AxisMotionType.Rotary;   // Set axis 4 as rotary

// This setting is stored in individual axis property parameters (91-94, 166-169)
// Bit 0 of each axis property parameter: 0=Linear, 1=Rotary
```

#### Fourth Axis Selection Parameter
**Parameter**: 131 (`FOURTH_AXIS_SELECTION_PARM`)

Special parameter for fourth axis rotary configuration using encoded values:

```csharp
// Fourth axis selection encoding (Parameter 131)
// Formula: tensPlace + onesPlace
// tensPlace = (axisLabel * 10), onesPlace = 1 (for 'N' label)

// Common fourth axis configurations:
cncPipe.parameter.SetMachineParameter(131, 11);  // A axis (10 + 1)
cncPipe.parameter.SetMachineParameter(131, 21);  // B axis (20 + 1)  
cncPipe.parameter.SetMachineParameter(131, 31);  // C axis (30 + 1)
cncPipe.parameter.SetMachineParameter(131, 0);   // No fourth axis

// Axis label encoding for tens place:
// A=10, B=20, C=30, U=40, V=50, W=60, X=70, Y=80, Z=90
```

### Rotary Jog Increment
**Parameter**: 41 (`ROTARY_JOG_INCREMENT_PARM`)

Sets the jog increment for rotary axes in degrees:

```csharp
// Set rotary jog increment to 0.1 degrees
cncPipe.parameter.SetMachineParameter(41, 0.1);

// Read current rotary jog increment
cncPipe.parameter.GetMachineParameterValue(41, out double rotaryJogIncrement);
Console.WriteLine($"Rotary jog increment: {rotaryJogIncrement} degrees");
```

### Rotary DRO Display Type
**Storage**: Axis Property Parameters (individual per axis)
**Bit Position**: Bit 1 of each axis property parameter

Controls whether rotary axes display in degrees (wrap-around) or rotations:

```csharp
// Configure rotary DRO display type per axis
data.Axis4.RotaryDRODisplay = RotaryDRODisplayType.ShowRotations;  // Show rotations
data.Axis4.RotaryDRODisplay = RotaryDRODisplayType.WrapAround;     // Show degrees (0-360°)

// Display type options:
// - ShowRotations: Display as rotation count (e.g., 2.5 rotations)
// - WrapAround: Display as degrees with wraparound (e.g., 0-360°)
```

### Rotary Feedrate Control Settings
**Parameter**: 2 (`CNC_COMPATIBILITY_PARM`)

Two important rotary feedrate behaviors are controlled by specific bits in the compatibility parameter:

#### Slave Rotary Axis Feedrate to Linear Move
**Bit**: 3 of Parameter 2

```csharp
// Read current compatibility parameter
cncPipe.parameter.GetMachineParameterValue(2, out double compatibilityParam);
int compatParm = (int)compatibilityParam;

// Check if rotary feedrate slaving is enabled
bool slaveRotaryFeedrate = GeneralUtils.IsBitSet(compatParm, 3);

// Enable/disable rotary feedrate slaving
compatParm = GeneralUtils.ModifyBit(compatParm, 3, true);   // Enable slaving
compatParm = GeneralUtils.ModifyBit(compatParm, 3, false);  // Disable slaving

// Write back to parameter
cncPipe.parameter.SetMachineParameter(2, compatParm);
```

#### Prevent Rotary Modal Feedrate
**Bit**: 5 of Parameter 2

Controls whether rotary-only moves use modal feedrates from previous rotary and non-rotary moves:

```csharp
// Read current compatibility parameter  
cncPipe.parameter.GetMachineParameterValue(2, out double compatibilityParam);
int compatParm = (int)compatibilityParam;

// Check if rotary modal feedrate prevention is enabled
bool preventRotaryModal = GeneralUtils.IsBitSet(compatParm, 5);

// Enable/disable rotary modal feedrate prevention
compatParm = GeneralUtils.ModifyBit(compatParm, 5, true);   // Prevent modal feedrate
compatParm = GeneralUtils.ModifyBit(compatParm, 5, false);  // Allow modal feedrate

// Write back to parameter
cncPipe.parameter.SetMachineParameter(2, compatParm);
```

### Complete Rotary Configuration Example

```csharp
public void ConfigureRotaryAxis(Data data, int axisNumber, char axisLabel)
{
    // 1. Set axis as rotary
    var axis = data.Axes.Find(x => x.AxisNumber == axisNumber);
    axis.LinearOrRotary = AxisMotionType.Rotary;
    axis.Label = axisLabel;
    
    // 2. Set rotary DRO display type
    axis.RotaryDRODisplay = RotaryDRODisplayType.ShowRotations;  // or WrapAround
    
    // 3. Configure rotary jog increment (global setting)
    cncPipe.parameter.SetMachineParameter(41, 0.1);  // 0.1 degrees
    
    // 4. Configure fourth axis selection (if axis 4)
    if (axisNumber == 4)
    {
        int tensPlace = GetAxisLabelValue(axisLabel) * 10;  // A=10, B=20, C=30, etc.
        int onesPlace = 1;  // Always 1 for 'N' label in wizard
        cncPipe.parameter.SetMachineParameter(131, tensPlace + onesPlace);
    }
    
    // 5. Configure rotary feedrate behavior
    cncPipe.parameter.GetMachineParameterValue(2, out double compatParam);
    int compatParm = (int)compatParam;
    
    // Enable rotary feedrate slaving
    compatParm = GeneralUtils.ModifyBit(compatParm, 3, true);
    // Prevent rotary modal feedrate
    compatParm = GeneralUtils.ModifyBit(compatParm, 5, true);
    
    cncPipe.parameter.SetMachineParameter(2, compatParm);
    
    // 6. Save axis changes
    axis.Save();
}

private int GetAxisLabelValue(char label)
{
    return label switch
    {
        'A' => 1, 'B' => 2, 'C' => 3, 'U' => 4, 'V' => 5, 
        'W' => 6, 'X' => 7, 'Y' => 8, 'Z' => 9,
        _ => 1  // Default to A
    };
}
```

### Summary of Rotary Settings Parameters

| Setting | Parameter | Type | Description |
|---------|-----------|------|-------------|
| **Rotary Axis Selection** | Axis Properties (91-94, 166-169) | Per-axis bit 0 | Linear(0) or Rotary(1) |
| **Fourth Axis Selection** | 131 | Special encoding | Axis label selection for 4th axis |
| **Rotary Jog Increment** | 41 | Global value | Jog increment in degrees |
| **Rotary DRO Display** | Axis Properties (91-94, 166-169) | Per-axis bit 1 | WrapAround(1) or Rotations(0) |
| **Slave Rotary Feedrate** | 2 | Global bit 3 | Enable/disable feedrate slaving |
| **Prevent Modal Feedrate** | 2 | Global bit 5 | Prevent rotary modal feedrate usage |

### Error Handling for Configuration Settings

```csharp
public bool SetConfigurationParameter(int paramNumber, double value, string settingName)
{
    try
    {
        CNCPipe.ReturnCode result = cncPipe.parameter.SetMachineParameter(paramNumber, value);
        
        if (result == CNCPipe.ReturnCode.SUCCESS)
        {
            Console.WriteLine($"{settingName} set successfully to {value}");
            return true;
        }
        else if (result == CNCPipe.ReturnCode.STATUS_UNKNOWN)
        {
            Console.WriteLine($"Parameter {paramNumber} ({settingName}) is read-only or invalid");
            return false;
        }
        else
        {
            Console.WriteLine($"Failed to set {settingName}: {result}");
            return false;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Exception setting {settingName}: {ex.Message}");
        return false;
    }
}
```

### Configuration Validation and Dependencies

Many configuration settings have interdependencies and validation requirements:

1. **PWM frequency** is shared between spindle and laser configurations
2. **Probe settings** affect both probing and touch plate operations  
3. **Axis properties** must be coordinated with drive types and step frequencies
4. **ATC settings** require compatible I/O assignments
5. **MPG assignments** must not conflict with other encoder uses

Always verify configuration changes don't conflict with existing settings and hardware capabilities.

## I/O Board Detection and Configuration

The CentroidAPI provides sophisticated I/O board detection capabilities that allow automatic discovery of available inputs and outputs across different Centroid system types.

### System-Specific I/O Characteristics

#### Acorn System I/O Layout
- **Base I/O**: 8 inputs, 8 outputs (numbered 1-8)
- **Expansion**: Ether1616 boards (16 inputs + 16 outputs each)
- **Expansion Start**: I/O 17 for first expansion board
- **Maximum Expansion**: Multiple Ether1616 boards supported

#### AcornSix System I/O Layout  
- **Base I/O**: 16 inputs, 16 outputs (numbered 1-16)
- **Expansion**: PLCEXP1616 boards (16 inputs + 16 outputs each)
- **Expansion Start**: I/O 65 for first expansion board
- **Maximum Expansion**: Multiple PLCEXP1616 boards supported

#### Hickory System I/O Layout
- **Base I/O**: 32 inputs, 32 outputs (numbered 1-32)
- **Expansion**: ECAT1616 boards (16 inputs + 16 outputs each)
- **Expansion Start**: I/O 129 for first expansion board
- **Maximum Expansion**: Multiple ECAT1616 boards supported

### I/O Detection Implementation

#### Acorn I/O Detection
```csharp
public static int[] GetAcornAvailableInputs(CNCPipe cncPipe)
{
    var availableInputs = new List<int>();
    
    // Acorn has 8 base inputs (inputs 1-8)
    for (int i = 1; i <= 8; i++)
    {
        availableInputs.Add(i);
    }
    
    // Check for Ether1616 expansion boards (inputs 17+)
    cncPipe.system.GetEther1616DeviceInfo(out List<CNCPipe.Sys.Ether1616Device> devices);
    
    if (devices != null && devices.Count > 0)
    {
        int startIO = 17;  // Acorn expansion starts at I/O 17
        for (int board = 0; board < devices.Count; board++)
        {
            for (int i = 0; i < 16; i++)  // Each Ether1616 provides 16 I/O
            {
                availableInputs.Add(startIO + (board * 16) + i);
            }
        }
    }
    
    return availableInputs.ToArray();
    
    // Example results:
    // No expansion boards: [1, 2, 3, 4, 5, 6, 7, 8]
    // 1 Ether1616 board:   [1, 2, 3, 4, 5, 6, 7, 8, 17, 18, 19, ..., 32]
    // 2 Ether1616 boards:  [1, 2, 3, 4, 5, 6, 7, 8, 17, 18, 19, ..., 32, 33, 34, ..., 48]
}
```

#### AcornSix I/O Detection
```csharp
public static int[] GetAcornSixAvailableInputs(CNCPipe cncPipe)
{
    var availableInputs = new List<int>();
    
    // AcornSix has 16 base inputs (inputs 1-16)
    for (int i = 1; i <= 16; i++)
    {
        availableInputs.Add(i);
    }
    
    // Check for PLCEXP1616 expansion boards (inputs 65+)
    cncPipe.system.GetPLCEXP1616NumberofDevices(out int numExpansions);
    
    if (numExpansions > 0)
    {
        int startIO = 65;  // AcornSix expansion starts at I/O 65
        for (int board = 0; board < numExpansions; board++)
        {
            for (int i = 0; i < 16; i++)  // Each PLCEXP1616 provides 16 I/O
            {
                availableInputs.Add(startIO + (board * 16) + i);
            }
        }
    }
    
    return availableInputs.ToArray();
    
    // Example results:
    // No expansion boards: [1, 2, 3, 4, ..., 16]
    // 1 PLCEXP1616 board:  [1, 2, 3, 4, ..., 16, 65, 66, 67, ..., 80]
    // 2 PLCEXP1616 boards: [1, 2, 3, 4, ..., 16, 65, 66, 67, ..., 80, 81, 82, ..., 96]
}
```

#### Hickory I/O Detection
```csharp
public static int[] GetHickoryAvailableInputs(CNCPipe cncPipe)
{
    var availableInputs = new List<int>();
    
    // Hickory has 32 base inputs (inputs 1-32)
    for (int i = 1; i <= 32; i++)
    {
        availableInputs.Add(i);
    }
    
    // Check for ECAT1616 expansion boards (inputs 129+)
    cncPipe.system.GetECAT1616NumberOfDevices(out int numExpansions);
    
    if (numExpansions > 0)
    {
        int startIO = 129;  // Hickory expansion starts at I/O 129
        for (int board = 0; board < numExpansions; board++)
        {
            for (int i = 0; i < 16; i++)  // Each ECAT1616 provides 16 I/O
            {
                availableInputs.Add(startIO + (board * 16) + i);
            }
        }
    }
    
    return availableInputs.ToArray();
    
    // Example results:
    // No expansion boards: [1, 2, 3, 4, ..., 32]
    // 1 ECAT1616 board:    [1, 2, 3, 4, ..., 32, 129, 130, 131, ..., 144]
    // 2 ECAT1616 boards:   [1, 2, 3, 4, ..., 32, 129, 130, 131, ..., 144, 145, 146, ..., 160]
}
```

### Universal I/O Detection Implementation

#### Complete System-Agnostic Implementation
```csharp
public static int[] GetAllAvailableInputs(CNCPipe cncPipe)
{
    var availableInputs = new List<int>();
    
    // Get system type to determine I/O layout
    cncPipe.system.GetUnlockVersion(out CNCPipe.Sys.UnlockVersions unlockVersion);
    
    // Base I/O available on all systems (minimum 8 inputs)
    int baseInputCount = 8;
    int expansionStartIO = 17;
    
    // Determine system-specific configuration
    bool isAcorn = unlockVersion.ToString().Contains("ACORN") && !unlockVersion.ToString().Contains("ACORN_SIX");
    bool isAcornSix = unlockVersion.ToString().Contains("ACORN_SIX");
    bool isHickory = unlockVersion.ToString().Contains("HICKORY");
    
    if (isAcornSix)
    {
        baseInputCount = 16;
        expansionStartIO = 65;
    }
    else if (isHickory)
    {
        baseInputCount = 32;
        expansionStartIO = 129;
    }
    
    // Add base inputs
    for (int i = 1; i <= baseInputCount; i++)
    {
        availableInputs.Add(i);
    }
    
    // Add expansion board inputs
    int expansionCount = 0;
    if (isAcorn)
    {
        cncPipe.system.GetEther1616DeviceInfo(out List<CNCPipe.Sys.Ether1616Device> devices);
        expansionCount = devices?.Count ?? 0;
    }
    else if (isAcornSix)
    {
        cncPipe.system.GetPLCEXP1616NumberofDevices(out expansionCount);
    }
    else if (isHickory)
    {
        cncPipe.system.GetECAT1616NumberOfDevices(out expansionCount);
    }
    
    // Calculate expansion I/O numbers
    if (expansionCount > 0)
    {
        for (int board = 0; board < expansionCount; board++)
        {
            for (int i = 0; i < 16; i++)  // All expansion boards provide 16 I/O each
            {
                availableInputs.Add(expansionStartIO + (board * 16) + i);
            }
        }
    }
    
    return availableInputs.ToArray();
}

public static int[] GetAllAvailableOutputs(CNCPipe cncPipe)
{
    // Output numbering follows identical pattern to inputs
    return GetAllAvailableInputs(cncPipe);
}
```

### Board Information Data Structure

#### Comprehensive Board Detection
```csharp
public class BoardInfo
{
    public string SystemType { get; set; }
    public int BaseInputs { get; set; }
    public int BaseOutputs { get; set; }
    public int ExpansionInputs { get; set; }
    public int ExpansionOutputs { get; set; }
    public int Ether1616Count { get; set; }
    public int PLCEXP1616Count { get; set; }
    public int ECAT1616Count { get; set; }
    
    public int TotalInputs => BaseInputs + ExpansionInputs;
    public int TotalOutputs => BaseOutputs + ExpansionOutputs;
    
    public override string ToString()
    {
        return $"{SystemType}: {TotalInputs} inputs, {TotalOutputs} outputs " +
               $"(Base: {BaseInputs}/{BaseOutputs}, Expansion: {ExpansionInputs}/{ExpansionOutputs})";
    }
}

public static BoardInfo GetBoardInfo(CNCPipe cncPipe)
{
    cncPipe.system.GetUnlockVersion(out CNCPipe.Sys.UnlockVersions unlockVersion);
    
    var boardInfo = new BoardInfo
    {
        SystemType = GetSystemTypeName(unlockVersion),
        BaseInputs = 8,
        BaseOutputs = 8
    };
    
    bool isAcorn = unlockVersion.ToString().Contains("ACORN") && !unlockVersion.ToString().Contains("ACORN_SIX");
    bool isAcornSix = unlockVersion.ToString().Contains("ACORN_SIX");
    bool isHickory = unlockVersion.ToString().Contains("HICKORY");
    
    if (isAcorn)
    {
        cncPipe.system.GetEther1616DeviceInfo(out List<CNCPipe.Sys.Ether1616Device> devices);
        boardInfo.Ether1616Count = devices?.Count ?? 0;
        boardInfo.ExpansionInputs = boardInfo.Ether1616Count * 16;
        boardInfo.ExpansionOutputs = boardInfo.Ether1616Count * 16;
    }
    else if (isAcornSix)
    {
        boardInfo.BaseInputs = 16;
        boardInfo.BaseOutputs = 16;
        
        cncPipe.system.GetPLCEXP1616NumberofDevices(out int numExpansions);
        boardInfo.PLCEXP1616Count = numExpansions;
        boardInfo.ExpansionInputs = numExpansions * 16;
        boardInfo.ExpansionOutputs = numExpansions * 16;
    }
    else if (isHickory)
    {
        boardInfo.BaseInputs = 32;
        boardInfo.BaseOutputs = 32;
        
        cncPipe.system.GetECAT1616NumberOfDevices(out int numExpansions);
        boardInfo.ECAT1616Count = numExpansions;
        boardInfo.ExpansionInputs = numExpansions * 16;
        boardInfo.ExpansionOutputs = numExpansions * 16;
    }
    
    return boardInfo;
}

private static string GetSystemTypeName(CNCPipe.Sys.UnlockVersions version)
{
    if (version.ToString().Contains("HICKORY")) return "Hickory";
    if (version.ToString().Contains("ACORN_SIX")) return "AcornSix";
    if (version.ToString().Contains("ACORN")) return "Acorn";
    return "Unknown";
}
```

### Key I/O Numbering Rules

#### Base I/O Numbering
- All systems start at I/O number 1
- **Acorn**: 1-8 (8 total)
- **AcornSix**: 1-16 (16 total)  
- **Hickory**: 1-32 (32 total)

#### Expansion Starting Points
Each system has a specific starting point for expansion I/O:
- **Acorn**: Expansion starts at 17
- **AcornSix**: Expansion starts at 65
- **Hickory**: Expansion starts at 129

#### Expansion Board Capacity
All expansion boards provide 16 I/O each:
- **Ether1616**: 16 inputs + 16 outputs
- **PLCEXP1616**: 16 inputs + 16 outputs
- **ECAT1616**: 16 inputs + 16 outputs

#### Sequential Board Numbering
Multiple expansion boards are numbered sequentially:
- **Board 0**: startIO + (0 * 16) = startIO to startIO + 15
- **Board 1**: startIO + (1 * 16) = startIO + 16 to startIO + 31
- **Board 2**: startIO + (2 * 16) = startIO + 32 to startIO + 47

#### Input and Output Symmetry
- Input and output numbering follows identical patterns
- Each expansion board adds the same count to both inputs and outputs
- Available inputs and outputs use the same numbering schemes

### Practical I/O Detection Examples

#### Complete System Detection Example
```csharp
// Initialize CNCPipe
CNCPipe cncPipe = new CNCPipe();
// ... connection setup ...

// Get system information
BoardInfo info = GetBoardInfo(cncPipe);
Console.WriteLine($"System: {info}");

// Get all available I/O
int[] inputs = GetAllAvailableInputs(cncPipe);
int[] outputs = GetAllAvailableOutputs(cncPipe);

Console.WriteLine($"Available inputs: {string.Join(", ", inputs)}");
Console.WriteLine($"Available outputs: {string.Join(", ", outputs)}");

// Check specific I/O availability
if (IsInputAvailable(cncPipe, 25))
{
    Console.WriteLine("Input 25 is available for use");
}
else
{
    Console.WriteLine("Input 25 is not available on this system");
}
```

#### Real-World Configuration Examples

**Acorn with 2 Ether1616 Boards**
```
Base I/O: [1, 2, 3, 4, 5, 6, 7, 8]
Board 0:  [17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32]
Board 1:  [33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48]
Total:    56 inputs, 56 outputs
```

**AcornSix with 1 PLCEXP1616 Board**
```
Base I/O: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16]
Board 0:  [65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80]
Total:    32 inputs, 32 outputs
```

**Hickory with 1 ECAT1616 Board**
```
Base I/O: [1, 2, 3, ..., 32]
Board 0:  [129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144]
Total:    48 inputs, 48 outputs
```

### I/O Detection Notes and Considerations

#### Ether1616 Device Information
The `CNCPipe.Sys.Ether1616Device` class contains:
- `DeviceNumber`: Device identifier
- `IP`: IP address of the device

The Centroid Wizard code shows a different calculation for Ether1616 starting I/O numbers:
```csharp
StartingIONumber = 32 + (Convert.ToInt32(device.DeviceNumber) * 16)
```
This suggests there may be variations in I/O numbering depending on implementation context.

#### Error Handling
System detection methods do not return error codes like parameter methods. They use void returns with out parameters. Always check for null device lists when working with Ether1616 devices.

#### Performance
I/O detection involves multiple API calls and should not be called frequently. Consider caching results when possible.

## Best Practices for CentroidAPI Integration

### Connection Management
- Always check return codes before using output values
- Implement proper error handling for communication failures
- Use appropriate exception handling for parameter validation

### Parameter Safety
- Verify parameter numbers against CNC12 documentation
- Check for STATUS_UNKNOWN return codes on write operations
- Be aware that some parameters are read-only or conditional

### Performance Considerations
- Parameter reads/writes involve communication with CNC12
- Cache frequently accessed values when appropriate
- Avoid excessive polling of real-time values

### System Compatibility
- Always detect system type before using system-specific features
- Use appropriate I/O numbering schemes for each system type
- Test expansion board detection before relying on extended I/O

## Typical CentroidAPI Integration Pattern

```csharp
public class CNCInterface
{
    private CNCPipe _cncPipe;
    
    public bool Initialize()
    {
        try
        {
            _cncPipe = new CNCPipe();
            // Add your connection logic here
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CNC initialization failed: {ex.Message}");
            return false;
        }
    }
    
    public double GetParameter(int paramNum)
    {
        CNCPipe.ReturnCode result = _cncPipe.parameter.GetMachineParameterValue(paramNum, out double value);
        
        if (result != CNCPipe.ReturnCode.SUCCESS)
        {
            throw new Exception($"Parameter {paramNum} read failed: {result}");
        }
        
        return value;
    }
    
    public void SetParameter(int paramNum, double value)
    {
        CNCPipe.ReturnCode result = _cncPipe.parameter.SetMachineParameter(paramNum, value);
        
        if (result == CNCPipe.ReturnCode.STATUS_UNKNOWN)
        {
            throw new Exception($"Parameter {paramNum} is read-only or invalid");
        }
        else if (result != CNCPipe.ReturnCode.SUCCESS)
        {
            throw new Exception($"Parameter {paramNum} set failed: {result}");
        }
    }
}
```

## CentroidAPI Dependencies and Requirements

### Required References
- CentroidAPI.dll (Centroid-provided assembly)
- Appropriate .NET Framework version (typically .NET Framework 4.x)

### System Requirements
- Active CNC12 installation
- Proper licensing for CentroidAPI usage
- Compatible Centroid hardware (Acorn, AcornSix, Hickory systems)

### Development Environment
- Visual Studio or compatible IDE
- Reference to CentroidAPI project or assembly
- Access to CNC12 system for testing

## CentroidAPI Troubleshooting

### Common Issues

1. **Parameter Access Failures**
   - Check parameter number validity
   - Verify CNC12 system is running
   - Ensure proper API initialization

2. **STATUS_UNKNOWN Return Codes**
   - Parameter may be read-only
   - Parameter may not apply to current machine configuration
   - Check CNC12 parameter documentation

3. **I/O Detection Issues**
   - Verify expansion board connections
   - Check system startup sequence
   - Ensure proper board addressing

4. **Connection Problems**
   - Verify CNC12 service is running
   - Check for proper API licensing
   - Ensure compatible system versions

### Debug Information
```csharp
// System diagnostic information
cncPipe.system.GetUnlockVersion(out CNCPipe.Sys.UnlockVersions version);
Console.WriteLine($"System: {version}");

// Test basic parameter access
try
{
    CNCPipe.ReturnCode result = cncPipe.parameter.GetMachineParameterValue(0, out double testValue);
    Console.WriteLine($"Parameter 0 access: {result}");
}
catch (Exception ex)
{
    Console.WriteLine($"Basic parameter test failed: {ex.Message}");
}
```

## Missing Parameters - Not Found in Current System

The following parameters were requested but do not appear to have corresponding parameter numbers in the current CNC12 system:

### Spindle Configuration
- **❌ Spindle Accel/Decel Time** - Spindle acceleration/deceleration timing control
  - *Note*: This functionality may be controlled through spindle drive configuration rather than CNC12 parameters

### Tapping Cycle M-Functions
- **❌ M Func To Run At Bottom Of Hole G84 Tapping** - M function for G84 right-hand tapping bottom
- **❌ M Func To Run At Top Of Hole For G84 Counter Tapping** - M function for G84 counter tapping top
- **❌ M Func To Run At Bottom Of Hole G74 Tapping (Left Hand)** - M function for G74 left-hand tapping bottom  
- **❌ M Func To Run At Top Of Hole For G74 Counter Tapping** - M function for G74 counter tapping top

### Implementation Notes
These parameters may be:
1. **Not implemented** in the current CNC12 version
2. **Controlled differently** through PLC logic or M-code programming
3. **Hardware-specific** settings managed by the spindle drive rather than CNC12 parameters
4. **Future features** not yet released in the current system version

For tapping cycle customization, consider using:
- Custom M-code programs in the PLC
- Subroutine calls within tapping cycles
- Manual G-code sequences rather than canned cycles

---

*This comprehensive documentation combines PLC file management, I/O definition handling, axis configuration, and CentroidAPI integration for complete Centroid CNC system setup and control.*