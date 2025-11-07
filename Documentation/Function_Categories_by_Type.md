# CNC Function Categories by Type and Usage

**Generated:** November 7, 2025  
**Based on:** functions.xml analysis and PLC logic usage analysis

## Summary Statistics

- **Total Functions in XML:** 285
- **Functions Used in PLC Logic:** 251 (88.07%)
- **Functions Not Used:** 34 (11.93%)
- **Total Usage Instances:** 606

---

## USED INPUT FUNCTIONS (Found in PLC Logic)

### Safety & Emergency Stop (6 functions)
- **EStopOk** (7 uses) - EStop button input. Connect EStop according to Acorn schematics.
- **EStopOk2** (7 uses) - Secondary EStop button input.
- **ATC_AirPressureOk** (4 uses) - The input that is used for detecting if the Air Pressure is at an acceptable level.

### Spindle Control & Monitoring (8 functions)
- **SpindleOk** (1 use) - Signal from spindle drive letting Acorn know spindle drive is okay with no faults.


### Tool Management & ATC (23 functions)
- **ToolIsUnclamped** (1 use) - Input used with a sensor to verify that the tool is actually unclamped.
- **ToolUnclampButton** (3 uses) - External Button wired to input to activate ToolUnclamp.
- **ToolClamped_I** (1 use) - Input indicating tool is clamped.
- **ToolIsPresent_I** (1 use) - Input Used to Detect if a Tool is in the tool holder.
- **DrawBarReleased** (3 uses) - An input that is typically used on ATC router spindles.
- **DrawBarIsUp_I** (2 uses) - Input when Draw Bar is Up, Releasing the Tool.
- **DrawBarIsDown_I** (2 uses) - Input when Draw Bar is Down, No Tool In Spindle.

### Axis Control & Limits (8 functions)
- **FirstAxisHomeOk** (1 use) - Individual input for an individual home switch.
- **FirstAxisHomeLimitOk** (1 use) - Individual input for a combination home and limit switch.
- **SecondAxisHomeOk** (1 use) - Individual input for an individual home switch.
- **SecondAxisHomeLimitOk** (1 use) - Individual input for a combination home and limit switch.
- **HomeAll** (1 use) - One Input for all home switches.
- **HomeLimitAll** (2 uses) - Individual input for a combination home and limit switch.
- **SlavedHomeInput** (2 uses) - Home switch input used for autosquaring of paired axes.
- **DriveOk** (2 uses) - Signal from axis drive letting Acorn know axis drive is okay with no faults.

### Individual Axis Drive Status (4 functions)
- **Axis1DriveOk** (2 uses) - Individual Drive Fault input for X axis.
- **Axis2DriveOk** (2 uses) - Individual Drive Fault input for Y axis.
- **Axis3DriveOk** (2 uses) - Individual Drive Fault input for Z axis.
- **Axis4DriveOk** (2 uses) - Individual Drive Fault input for 4th axis.


### Probe Functions (3 functions)
- **ProbeDetect** (2 uses) - Input used to let CNC12 know that a touch probe is plugged in.
- **ProbeTripped** (1 use) - Touch probe tripped input.

### Coolant & Air Pressure (3 functions)
- **LubeOk** (2 uses) - Low oil level indicator switch input.
- **AirPressureLowMessage** (2 uses) - Input for Low Pressure Alarm. Issues a message when the input is active.
- **AirPressureLowStop** (2 uses) - Input for a low-pressure sensor. Issues a message and E-Stop condition.

### Control Buttons (4 functions)
- **CycleStart2** (2 uses) - External CycleStart button assigned to an input.
- **FeedHold2** (1 use) - External FeedHold button assigned to an input.
- **CycleCancel2** (5 uses) - External CycleCancel button assigned to an input.
- **ToolCheck2** (1 use) - External ToolCheck button assigned to an input.

### Plasma/Laser Functions (6 functions)
- **TorchArcOk_I** (1 use) - Plasma torch arc ok signal.
- **TorchFloatSwitch_I** (5 uses) - Plasma torch float switch input.
- **LaserReady_I** (2 uses) - Fiber laser ready signal.
- **FiberLaserOn_I** (1 use) - Fiber laser on signal.
- **FiberLaserOk_I** (1 use) - Fiber laser ok signal.
- **LaserHeadOk_I** (1 use) - Fiber laser head ok signal.
- **LaserHeadInPos_I** (1 use) - Fiber laser head in position signal.




---

## USED OUTPUT FUNCTIONS (Found in PLC Logic)

### Safety & Emergency Output (4 functions)
- **NoFaultOut** (1 use) - Output Signal from Acorn indicating that Acorn is okay and not in EStop.
- **SafetyDoorLockOpen_O** (2 uses) - Used to energize a lock solenoid to allow the safety door to be opened.
- **DriveResetOut** (1 use) - An output used to clear faults from a drive.
- **VFDResetOut_O** (1 use) - Output used to reset a VFD after a fault.

### Spindle Control (9 functions)
- **SpinFWD** (1 use) - Relay output used to command spindle forward.
- **SpinREV** (1 use) - Relay output used to command spindle reverse.
- **SpindleBrakeRelease** (3 uses) - A relay output used to control a spindle brake.
- **VFDEnable_O** (1 use) - Industry standard way of commanding spindle VFD enable.
- **VFDDirection_O** (1 use) - Industry standard way of commanding spindle VFD direction.
- **SpindleCooling** (4 uses) - Output to control spindle cooling typically a fan or water pump.
- **SpindleCooling_Fan** (2 uses) - Output for use with a Spindle Cooling Fan.

### Tool Management & ATC (18 functions)
- **UnclampTool** (7 uses) - Output used to release the tool drawbar to unclamp the tool.
- **DrawBarUp_O** (2 uses) - Output to Move Drawbar Up, Releasing the Tool.
- **ATCAirBlowActivate** (1 use) - Output to control air blow solenoid.

### Coolant & Auxiliary (7 functions)
- **Flood** (2 uses) - Flood pump on/off relay output (M8 on M9 off).
- **Mist** (2 uses) - Mister solenoid on/off relay output (M7 on M9 off).
- **LubePump** (6 uses) - Output that controls an automatic lube pump.
- **RouterDustCollection** (2 uses) - Output to control dust collection motor through a relay.
- **RouterVacuumHoldDown** (3 uses) - Output to control material Vacuum hold down.
- **DustCollectionOn** (6 uses) - Output to control dust collection system.



### General Purpose Outputs (8 functions)
- **OUTPUT1** (1 use) - General purpose output used to turn a relay on/off. Uses M61/M81.
- **OUTPUT2** (1 use) - General purpose output used to turn a relay on/off. Uses M62/M82.
- **OUTPUT3** (1 use) - General purpose output used to turn a relay on/off. Uses M63/M83.
- **OUTPUT4** (1 use) - General purpose output used to turn a relay on/off. Uses M64/M84.
- **OUTPUT5** (1 use) - General purpose output used to turn a relay on/off. Uses M65/M85.
- **OUTPUT6** (1 use) - General purpose output used to turn a relay on/off. Uses M66/M86.
- **OUTPUT7** (1 use) - General purpose output used to turn a relay on/off. Uses M67/M87.
- **OUTPUT8** (1 use) - General purpose output used to turn a relay on/off. Uses M68/M88.

### Router/Mill Specific (5 functions)
- **DustFootActivate** (2 uses) - Output to control dust foot.
- **LaserAlignActivate** (2 uses) - Output to control crosshair material alignment laser marking.
- **PopUpPins** (3 uses) - Output to control material alignment pins typically air solenoid.
- **AirBlowNozzle** (2 uses) - Activates the general purpose Air Blow Nozzle Output.
- **WorkLight** (1 use) - Output for a Worklight Starts On at Powerup.

### Laser Functions (8 functions)
- **LaserEnable** (1 use) - Output to control the Safety Interlock Circuit of a diode laser.
- **LaserReset** (1 use) - Output to send a reset signal to the Safety Interlock Circuit.
- **LaserCooling_Fan** (2 uses) - Output for use with a Laser Cooling Fan.
- **LaserEnable_O** (2 uses) - Fiber laser enable output.
- **AlignmentLaserEnable_O** (1 use) - Fiber alignment laser enable output.
- **LaserStandby_O** (1 use) - Fiber laser standby mode output.
- **FiberLaserReset_O** (1 use) - Fiber laser reset output.
- **LaserDeploy_O** (1 use) - Output to move the laser carriage into working position.


### PWM and Special Outputs (2 functions)
- **PWMSelect** (1 use) - Output to enable PWM.


