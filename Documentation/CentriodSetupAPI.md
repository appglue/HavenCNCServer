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
| 11 | Probe Number and State | Probe input assignment and type | Input number + probe type bits |
| 406 | Probe Input Type | Normal open/closed configuration | 0=NO, 1=NC |
| 409 | Probe Type | Physical probe type | 0=Conductive, 1=Non-conductive |
| 12 | Probe Tool Number | Tool number assigned to probe | Tool number (1-999) |
| 13 | Probe Recovery Distance | Retract distance after contact | Distance in machine units |
| 14 | Fast Probe Rate | Fast probing speed | Units per minute |
| 15 | Slow Probe Rate | Slow probing speed | Units per minute |
| 416 | Probe Inhibit | Probe protection settings | Bit field for protection options |
| 410 | Display Probe Warning | Show probe warnings | 0=No, 1=Yes |

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

```csharp
// Read global axis signal inversions
cncPipe.parameter.GetMachineParameterValue(961, out double inversionValue);
int axisInversions = (int)inversionValue;

// Set global axis signal inversions
cncPipe.parameter.SetMachineParameter(961, newInversionValue);
```

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

### Summary of Global vs Per-Axis Settings

#### Global Settings (Apply to ALL Axes):
- **Step Frequency**: 1200000/Parameter968 (pulses per second)
- **Signal Inversions**: Parameter 961 (4-bit nibbles per axis)
- **Drive Fault Delay**: Parameter 991 (milliseconds)
- **Low Resolution Mode**: Parameter 225 (plasma systems)

#### Per-Axis Settings:
- **Steps per Revolution**: Individual via `cncPipe.axis.SetCountsPerTurn(axis, value)`
- **Travel Limits**: Individual via `cncPipe.axis.SetTravelLimit(axis, direction, limit)`
- **Jog Rates**: Individual via `cncPipe.axis.SetRate(axis, rateType, value)`
- **Axis Properties**: Individual linear/rotary, reversed, etc.
- **Homing Configuration**: Individual homing methods and order
- **Acceleration Rates**: Individual axis acceleration settings

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
```csharp
// Tool offset measurement and reference methods
// These settings control automatic tool measurement and offset calculation
// Implementation depends on specific measurement device (touch plate, probe, etc.)
```

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

---

*This comprehensive documentation combines PLC file management, I/O definition handling, axis configuration, and CentroidAPI integration for complete Centroid CNC system setup and control.*