# PLC I/O Usage Summary

## Overview
Analysis of the actual I/O usage vs declarations in the HavenCNCServer PLC logic code.

## Key Findings

### Actually Used Inputs (17 primary inputs)
| Input | Number | Name | Usage |
|-------|--------|------|-------|
| ✅ | INP1 | EStopOk | Critical safety - used extensively |
| ✅ | INP2 | ToolClamped_I | Tool management |
| ✅ | INP3 | ToolIsPresent_I | Tool detection |
| ✅ | INP4 | ToolIsUnclamped | Tool state |
| ✅ | INP5 | ToolUnclampButton | Manual tool control |
| ❌ | INP6 | AirPressureLowStop | **DECLARED BUT NOT USED** |
| ✅ | INP8 | ToolTouchOffTriggered | Tool touch-off |
| ✅ | INP9 | FirstAxisHomeOk | Homing system |
| ✅ | INP10 | SecondAxisHomeOk | Homing system |
| ✅ | INP11 | ThirdAxisHomeOk | Homing system |
| ✅ | INP12 | SlavedHomeInput | Paired homing |
| ✅ | INP14 | HomeAll | Home all axes |
| ✅ | INP65 | Axis1DriveOk | Drive fault monitoring |
| ✅ | INP66 | Axis2DriveOk | Drive fault monitoring |
| ✅ | INP67 | Axis3DriveOk | Drive fault monitoring |
| ✅ | INP68 | Axis4DriveOk | Drive fault monitoring |
| ✅ | INP69 | SpindleOk | Spindle fault monitoring |

**Plus many jog panel inputs (INP1057-INP1312) for machine control interface**

### Actually Used Outputs (10 primary outputs)
| Output | Number | Name | Usage |
|--------|--------|------|-------|
| ✅ | OUT1 | SpinFWD | Spindle forward control |
| ✅ | OUT2 | RouterDustCollection | Dust management |
| ✅ | OUT3 | VFDResetOut_O | Drive reset |
| ✅ | OUT4 | RouterVacuumHoldDown | Vacuum control |
| ✅ | OUT5 | UnclampTool | Tool unclamping |
| ✅ | OUT6 | AirBlowNozzle | Air blow system |
| ✅ | OUT7 | DustFootActivate | Dust foot control |
| ✅ | OUT12 | LaserEnable | Laser control |
| ✅ | OUT17-32 | SpinAnalogOutBit0-15 | 16-bit spindle speed |
| ✅ | OUT33-48 | AuxAnalogOutBit0-15 | 16-bit auxiliary analog |

**Plus many LED outputs (OUT1057-OUT1312) for status indication**

## Configuration Type
**Router System** with:
- ✅ Dust collection
- ✅ Vacuum hold-down
- ✅ Tool management
- ✅ Laser capability
- ✅ Spindle control
- ✅ Comprehensive jog panel

## Notable Unused I/O
- **INP6 (AirPressureLowStop)** - Declared but never referenced in logic
- **INP7, INP13, INP15-64** - Not declared for this configuration
- **OUT8-11, OUT13-16** - Not declared for this configuration
- Many expansion I/O points reserved for future use

## Usage Statistics
- **Declared Inputs**: ~80+ inputs
- **Actually Used Inputs**: 17 primary + jog panel inputs
- **Declared Outputs**: ~80+ outputs  
- **Actually Used Outputs**: 10 primary + analog bits + LED outputs
- **Unused Declared I/O**: Several inputs/outputs declared but not used in logic

This analysis shows the PLC is configured for a specific router application with most core functionality implemented but room for expansion.