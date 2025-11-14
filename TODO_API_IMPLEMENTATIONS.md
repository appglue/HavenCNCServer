# API Implementation TODO List

## CNCProgramController.cs

- [x] **GetCurrentGCode** - Get current G-code executing on the machine
  - Location: `Controllers/CNCProgramController.cs` line ~860
  - Implementation: Returns all G-code lines from running CNCJob via GCodeLines property
  
- [x] **GetCurrentLineNumber** - Get current line number of executing program
  - Location: `Controllers/CNCProgramController.cs` line ~884
  - Implementation: Returns LineNumber from running CNCJob
  
- [ ] **CheckTool** - Check tool functionality/verification
  - Location: `Controllers/CNCProgramController.cs` line ~1249
  - Notes: Need to implement using CentroidAPI

## CNCSpindleController.cs (All methods unimplemented)

### Spindle Control
- [ ] **StartSpindle** - Start the spindle (with optional speed parameter)
  - Location: `Controllers/CNCSpindleController.cs` line ~36
  - Notes: Use CentroidAPI or skin events
  
- [ ] **StopSpindle** - Stop the spindle
  - Location: `Controllers/CNCSpindleController.cs` line ~55
  - Notes: Use CentroidAPI or skin events
  
- [ ] **WarmUpSpindle** - Warm up the spindle
  - Location: `Controllers/CNCSpindleController.cs` line ~74
  - Notes: Implement warm-up sequence
  
- [ ] **IsSpindleRunning** - Check if spindle is currently running
  - Location: `Controllers/CNCSpindleController.cs` line ~92
  - Notes: Check spindle state via API

### Spindle Speed Control
- [ ] **GetSpindleSpeed** - Get current spindle RPM
  - Location: `Controllers/CNCSpindleController.cs` line ~114
  - Notes: Read from CentroidAPI DRO or system variable
  
- [ ] **SetSpindleSpeed** - Set spindle speed (RPM)
  - Location: `Controllers/CNCSpindleController.cs` line ~135
  - Notes: Send M3/M4 command with S parameter
  
- [ ] **AdjustSpindleSpeed** - Adjust spindle speed by percentage factor
  - Location: `Controllers/CNCSpindleController.cs` line ~156
  - Notes: Spindle override adjustment (-200 to +200)
  
- [ ] **GetCurrentSpindleSpeedFactor** - Get current spindle override percentage
  - Location: `Controllers/CNCSpindleController.cs` line ~178
  - Notes: Read spindle override system variable
  
- [ ] **ResetSpindleSpeedFactor** - Reset spindle override to 100%
  - Location: `Controllers/CNCSpindleController.cs` line ~197
  - Notes: Reset spindle override to default

## Summary
- **Total Methods**: 12
- **Completed**: 2
- **Remaining**: 10

---
**Note**: Override Input/Output methods have been removed as they are not needed.
