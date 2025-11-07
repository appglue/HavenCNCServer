# HavenCNCServer PLC I/O Analysis Report

## Overview
This report analyzes the HavenCNCServer PLC source file (`acorn_router_plc.src`) to determine which inputs and outputs are declared versus actually used in the logic code.

## File Structure Analysis
- **Lines 1-647**: Comments and headers
- **Lines 648-783**: INPUT DEFINITIONS 
- **Lines 784-3000**: OUTPUT DEFINITIONS
- **Lines 3001-3080**: Stage definitions
- **Lines 3081+**: ACTUAL LOGIC CODE

## INPUT DEFINITIONS (Lines 648-783)

### Standard Inputs (INP1-INP80)
**Declared Inputs:**
- **INP1** - EStopOk
- **INP2** - ToolClamped_I  
- **INP3** - ToolIsPresent_I
- **INP4** - ToolIsUnclamped
- **INP5** - ToolUnclampButton
- **INP6** - AirPressureLowStop
- **INP8** - ToolTouchOffTriggered
- **INP9** - FirstAxisHomeOk
- **INP10** - SecondAxisHomeOk
- **INP11** - ThirdAxisHomeOk
- **INP12** - SlavedHomeInput
- **INP14** - HomeAll
- **INP65** - Axis1DriveOk
- **INP66** - Axis2DriveOk
- **INP67** - Axis3DriveOk
- **INP68** - Axis4DriveOk
- **INP69** - SpindleOk
- **INP70-INP783** - Various specialized inputs (MPG, Jog Panel, etc.)

### MPG Inputs (INP769-INP784)
**Declared MPG Inputs:**
- **INP770** - DSPProbe
- **INP772** - ProbeAux
- **INP773** - MPG_Inc_X_1
- **INP774** - MPG_Inc_X_10
- **INP775** - MPG_Inc_X_100
- **INP776-INP783** - MPG_AXIS_1 through MPG_AXIS_8

### Jog Panel Inputs (INP1057-INP1312)
**Declared Jog Panel Inputs:**
- **INP1057** - SpinOverPlusKey
- **INP1058** - SpinAutoManKey
- **INP1059-INP1111** - Various Aux and jog keys
- **INP1249-INP1264** - JpFeedOrKnobBit0 through JpFeedOrKnobBit15

## OUTPUT DEFINITIONS (Lines 784-3000)

### Standard Outputs (OUT1-OUT80)
**Declared Outputs:**
- **OUT1** - SpinFWD
- **OUT2** - RouterDustCollection
- **OUT3** - VFDResetOut_O
- **OUT4** - RouterVacuumHoldDown
- **OUT5** - UnclampTool
- **OUT6** - AirBlowNozzle
- **OUT7** - DustFootActivate
- **OUT12** - LaserEnable
- **OUT17-OUT32** - SpinAnalogOutBit0_O through SpinAnalogOutBit15_O (16-bit analog)
- **OUT33-OUT48** - AuxAnalogOutBit0_O through AuxAnalogOutBit15_O (16-bit analog)
- **OUT49-OUT63** - Various DAC mode and step rate outputs
- **OUT769** - MPG_LED_OUT

### Jog Panel LED Outputs (OUT1057-OUT1312)
**Declared Jog Panel LED Outputs:**
- **OUT1057** - SpinOverPlusLED
- **OUT1058** - SpinAutoModeLED
- **OUT1059-OUT1145** - Various LED outputs for panel indicators

## LOGIC ANALYSIS - ACTUALLY USED INPUTS

### **USED INPUTS** (Found in logic code lines 3081+):

#### **Primary System Inputs (USED):**
- **EStopOk (INP1)** - ✅ USED extensively throughout logic
  - Line ~6588: `IF !EStopOk || (!EStopOk2 && !EStopOK2Disabled_M) THEN (SetResetPD)`
  - Line ~6605: `IF EStopOk && (EStopOk2 || EStopOK2Disabled_M) THEN (EStopOkPD)`
  - Used in hundreds of other locations for safety interlocks

- **ToolClamped_I (INP2)** - ✅ USED in tool management logic
  - Line ~7845: `IF DrawBarIsDown_I && ToolClamped_I THEN (ToolClampedState_M)`

- **ToolIsPresent_I (INP3)** - ✅ USED in tool presence detection
  - Line ~7851: `IF ToolIsPresent_I THEN (ToolIsPresentState_M)`

- **ToolIsUnclamped (INP4)** - ✅ USED in tool unclamp logic
  - Line ~7835: `IF (ToolIsUnclamped && DrawBarReleasedDisable_M...`

- **ToolUnclampButton (INP5)** - ✅ USED in manual tool unclamping
  - Line ~7869: `IF VFDZeroSpeed && (((ToolUnclampButton || SkinToolRelease_M...`
  - Line ~7875: `IF ToolUnclampButton THEN (ToolUnclampButtonState_M)`

- **AirPressureLowStop (INP6)** - ❌ NOT USED directly in logic code
  - Declared but no direct references found in logic sections

- **ToolTouchOffTriggered (INP8)** - ✅ USED in tool touch-off logic
  - Line ~6708: `IF ToolTouchOffTriggered && AuxToolDetect_M THEN (AuxToolTripped_M)`

#### **Home/Limit Inputs (USED):**
- **FirstAxisHomeOk (INP9)** - ✅ USED in homing logic
  - Line ~8141: `IF !HomeAll || !SlavedHomeInput || !FirstAxisHomeOk || !FirstAxisHomeLimitOk...`

- **SecondAxisHomeOk (INP10)** - ✅ USED in homing logic
  - Line ~8141: Similar usage pattern

- **ThirdAxisHomeOk (INP11)** - ✅ USED in homing logic
  - Line ~8141: Similar usage pattern

- **SlavedHomeInput (INP12)** - ✅ USED in paired homing
  - Line ~8141: `IF !HomeAll || !SlavedHomeInput...`
  - Line ~8143: `IF SlavedHomeInput THEN (SlavedHomeTripOk_M)`

- **HomeAll (INP14)** - ✅ USED in home-all functionality
  - Line ~8141: `IF !HomeAll || !SlavedHomeInput...`

#### **Drive Status Inputs (USED):**
- **Axis1DriveOk (INP65)** - ✅ USED in drive fault monitoring
  - Line ~6493: `IF SV_AXIS_VALID_1 && SV_PC_POWER_AXIS_1 && (!Axis1DriveOk && DriveFaultTimer)`

- **Axis2DriveOk (INP66)** - ✅ USED in drive fault monitoring
  - Line ~6495: Similar pattern for Axis 2

- **Axis3DriveOk (INP67)** - ✅ USED in drive fault monitoring
  - Line ~6497: Similar pattern for Axis 3

- **Axis4DriveOk (INP68)** - ✅ USED in drive fault monitoring
  - Line ~6499: Similar pattern for Axis 4

- **SpindleOk (INP69)** - ✅ USED in spindle fault monitoring
  - Line ~8048: `IF SpindleFaultTimer_T && !SpindleOk THEN FaultMsg_W = SPINDLE_FAULT_MSG`

#### **Jog Panel Inputs (MANY USED):**
Examples of heavily used jog panel inputs:
- **CycleStartKey_I (INP1106)** - ✅ USED extensively
  - Line ~4851: `IF ((CycleStartKey_I || KbCycleStart_M...`
  - Used throughout for cycle start functionality

- **CycleCancelKey_I (INP1102)** - ✅ USED extensively  
  - Line ~4847: `IF (CycleCancelKey_I || KbCycleCancel_M...`

- **FeedHoldKey (INP1105)** - ✅ USED in feed hold logic
  - Line ~4395: `IF FeedHoldKey || KbFeedHold_M...`

- **Ax1PlusJogKey_I (INP1095)** - ✅ USED in jogging logic
  - Line ~6283: `IF (Ax1PlusJogKey_I || KbJogAx1Plus_M...`

## LOGIC ANALYSIS - ACTUALLY USED OUTPUTS

### **USED OUTPUTS** (Found in logic code lines 3081+):

#### **Primary Control Outputs (USED):**
- **SpinFWD (OUT1)** - ✅ USED in spindle control
  - Line ~5623: `IF SpindleEnableOut_M && !M37 && !SpindleDirectionOut_M && SpindleBrakeTimer THEN (SpinFWD)`

- **RouterDustCollection (OUT2)** - ✅ USED in dust collection logic
  - Line ~4928: `THEN (Flood), (CoolFloodLED), (SelectCoolantFlood), (RouterDustCollection)`

- **VFDResetOut_O (OUT3)** - ✅ USED in drive reset logic  
  - Line ~7917: `IF !EstopOK || (!EStopOk2 && !EStopOk2Disabled_M) THEN (VFDResetOut_O)`

- **RouterVacuumHoldDown (OUT4)** - ✅ USED in vacuum control
  - Line ~4941: `THEN (Mist), (CoolMistLED), (SelectCoolantMist), (RouterVacuumHoldDown)`

- **UnclampTool (OUT5)** - ✅ USED in tool unclamping
  - Line ~7869: `IF VFDZeroSpeed && (((ToolUnclampButton || SkinToolRelease_M...`
  - Line ~7869: `THEN (UnclampTool), (DrawBarUp_O)`

- **AirBlowNozzle (OUT6)** - ✅ USED in air blow control
  - Line ~7995: `IF AirBlowNozzle_SV THEN (AirBlowNozzle)`

- **DustFootActivate (OUT7)** - ✅ USED in dust foot control
  - Line ~7981: `IF DustFootActivate_SV THEN (DustFootActivate)`

- **LaserEnable (OUT12)** - ✅ USED in laser control
  - Line ~7816: `IF M37 && !SV_STOP THEN (LaserEnable)`

#### **Analog Outputs (USED):**
- **SpinAnalogOutBit0_O through SpinAnalogOutBit15_O (OUT17-32)** - ✅ USED for spindle speed control
  - Line ~5982: `IF True_M THEN WTB SixteenBitSpeed_W SpinAnalogOutBit0_O 16`

- **AuxAnalogOutBit0_O through AuxAnalogOutBit15_O (OUT33-48)** - ✅ USED for auxiliary analog control
  - Line ~6022: Multiple WTB operations for auxiliary analog outputs

#### **LED Outputs (MANY USED):**
Examples:
- **SpinAutoModeLED (OUT1058)** - ✅ USED in spindle mode indication
  - Line ~4928: Various LED control logic

- **x1JogLED_O (OUT1083)** - ✅ USED in jog increment indication
  - Line ~4183: `IF x1JogPD || OnAtPowerUp_M...THEN SET x1JogLED_O`

## UNUSED INPUTS (Declared but NOT referenced in logic)

### **NEVER USED INPUTS:**
- **AirPressureLowStop (INP6)** - ❌ DECLARED but NOT USED
- **INP7** - ❌ Not declared or used
- **INP13** - ❌ Not declared or used  
- **INP15-INP64** - ❌ Not declared in this configuration
- **INP70-INP768** - ❌ Many declared but not used in this router configuration

### **Jog Panel Inputs (Some unused):**
Many jog panel inputs are declared but may not be used depending on the physical panel configuration.

## UNUSED OUTPUTS (Declared but NOT referenced in logic)

### **NEVER USED OUTPUTS:**
- **OUT8-OUT11** - ❌ Not declared in this configuration
- **OUT13-OUT16** - ❌ Not declared in this configuration  
- Many expansion outputs **OUT65-OUT80** are declared but not used

## SUMMARY

### **Critical Inputs ACTUALLY USED:**
1. **EStopOk (INP1)** - Essential safety input
2. **Tool-related inputs (INP2-5, INP8)** - Tool management
3. **Home inputs (INP9-12, INP14)** - Homing functionality  
4. **Drive status inputs (INP65-69)** - Drive monitoring
5. **Jog panel inputs** - Machine control interface

### **Critical Outputs ACTUALLY USED:**
1. **SpinFWD (OUT1)** - Spindle control
2. **RouterDustCollection (OUT2)** - Dust management
3. **VFDResetOut_O (OUT3)** - Drive reset
4. **RouterVacuumHoldDown (OUT4)** - Vacuum control
5. **UnclampTool (OUT5)** - Tool management
6. **AirBlowNozzle (OUT6)** - Air management
7. **DustFootActivate (OUT7)** - Dust foot control
8. **LaserEnable (OUT12)** - Laser control
9. **Analog outputs (OUT17-48)** - Speed/analog control
10. **LED outputs** - Status indication

### **Configuration Notes:**
This analysis reveals that the PLC is configured as a **router** system with:
- Dust collection functionality
- Vacuum hold-down capability  
- Tool management system
- Laser capability
- Standard spindle control
- Comprehensive jog panel interface

The unused I/O points are likely reserved for future expansion or alternative machine configurations.