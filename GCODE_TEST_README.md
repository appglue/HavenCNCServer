# G-Code Test Dialog - Centroid API Integration

## Overview
The G-Code Test Dialog provides a simple interface for testing G-code execution through the Centroid API. This tool allows you to:

- Enter G-code manually in a text editor
- Save and load G-code files
- Test Centroid API connectivity
- Simulate G-code execution (with framework for real execution)

## Features

### 🔧 **File Operations**
- **New File**: Create a new G-code file with sample content
- **Open File**: Load existing G-code files (.nc, .gcode, .txt)
- **Save File**: Save your G-code to disk

### 🖥️ **G-Code Editor**
- Syntax-friendly editor with monospace font (Consolas)
- Line numbers and scrolling support
- Sample G-code provided by default

### ⚡ **Execution Controls**
- **Run G-Code**: Execute the G-code through Centroid API
- **Stop**: Stop execution
- **Pause**: Pause execution
- **Resume**: Resume execution

### 📊 **Logging & Monitoring**
- Real-time status logging in the dialog
- Centralized logging to main window
- Connection status monitoring
- API test results

## How to Use

### 1. Open the Dialog
- Click the **"G-Code Test"** button in the main HavenCNC Server window
- The dialog will automatically attempt to connect to the Centroid API

### 2. Connection Status
Watch the status log for connection messages:
- ✓ Green checkmarks indicate successful operations
- ✗ Red X marks indicate errors
- ⚠ Warning symbols indicate issues that may need attention

### 3. Edit G-Code
- Use the built-in editor to modify G-code
- Default sample includes basic movement commands:
  ```gcode
  G00 X0 Y0 Z1
  G01 Z-0.1 F100
  G01 X10 Y10 F500
  G01 X0 Y10
  G01 X0 Y0
  G00 Z1
  M30
  ```

### 4. Test Execution
- Click **"Run G-Code"** to start execution
- Monitor progress in the status log
- Use **Stop**, **Pause**, or **Resume** as needed

## Technical Implementation

### Threading
- **Background Connection**: Centroid API initialization runs on a background thread to prevent UI lockup
- **Async Execution**: G-code execution runs asynchronously to keep the UI responsive
- **Thread-Safe Logging**: All logging operations are thread-safe

### Centroid API Integration
The dialog tests several API capabilities:
- **Parameter Access**: Tests reading machine parameters
- **System Information**: Retrieves CNC system type and version
- **Connection Validation**: Verifies API connectivity before operations

### Logging Architecture
- **Local Logging**: Status messages in the dialog's status box
- **Main Window Logging**: Important events logged to main application
- **Timestamped Messages**: All log entries include timestamps

## Error Handling

### Connection Issues
- **Timeout Protection**: 10-second timeout for API connection
- **Graceful Degradation**: Dialog remains functional even if API connection fails
- **User Feedback**: Clear error messages for connection problems

### Execution Safety
- **Button State Management**: Run button disabled during execution to prevent conflicts
- **Exception Handling**: All operations wrapped in try-catch blocks
- **Resource Cleanup**: Proper disposal of resources on dialog close

## Current Limitations

### Simulation Mode
⚠️ **Important**: The current implementation runs in simulation mode. Real G-code execution requires:
1. Implementation of actual CentroidAPI execution methods
2. Proper CNC12 system running and accessible
3. Safety considerations for machine movement

### API Method Placeholders
The following methods need real implementation:
```csharp
// Load G-code file
_cncPipe.LoadGCodeFile(filePath);

// Program execution
_cncPipe.program.Start();
_cncPipe.program.Stop();
_cncPipe.program.Pause();
_cncPipe.program.Resume();

// MDI (Manual Data Input)
_cncPipe.mdi.SendCommand(gcodeLine);
```

## Development Notes

### Extending Functionality
To add real G-code execution:

1. **Research CentroidAPI Documentation**: Identify the correct methods for:
   - Loading G-code files
   - Starting/stopping program execution
   - MDI command sending

2. **Replace Simulation Code**: Update methods in `GCodeTestDialog.cs`:
   - `LoadGCodeFileAsync()`
   - `RunGCodeAsync()`
   - Button click handlers

3. **Add Safety Features**:
   - Emergency stop functionality
   - Position monitoring
   - Feed rate control
   - Coordinate system management

### Safety Considerations
⚠️ **Warning**: When implementing real execution:
- Always test with safe G-code first
- Implement emergency stop functionality
- Validate machine limits and safety systems
- Consider implementing dry-run mode
- Add position and status monitoring

## Troubleshooting

### "Centroid API not connected"
1. Ensure CNC12 software is running
2. Check that CentroidAPI.dll is available
3. Verify no firewall blocking communication
4. Check system permissions

### UI Lockup Issues
- All operations now run on background threads
- If lockup still occurs, check for synchronous operations in the API calls
- Monitor the main window log for detailed error information

### File Access Issues
- Ensure write permissions to temp directory
- Check that CNC12 programs directory exists
- Verify file path lengths are reasonable

## Future Enhancements

### Planned Features
- [ ] Real-time position display
- [ ] Feed rate override controls
- [ ] Dry-run simulation mode
- [ ] G-code syntax highlighting
- [ ] Program progress indicator
- [ ] Tool path visualization
- [ ] Machine status monitoring

### API Extensions
- [ ] MDI command interface
- [ ] Parameter monitoring
- [ ] I/O status display
- [ ] Alarm and error handling
- [ ] Coordinate system display