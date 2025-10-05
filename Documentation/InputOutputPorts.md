# CentroidAPI I/O Board Detection and Numbering

This documentation covers how to detect I/O expansion boards and calculate the exact input/output numbers available on Centroid CNC12 systems using the CentroidAPI.

## Overview

Centroid CNC systems have different I/O configurations based on the system type and installed expansion boards. Each system type has a specific base I/O count and uses different expansion board types with unique numbering schemes.

## System Types and Base I/O

### Acorn System
- **Base I/O**: 8 inputs and 8 outputs (numbers 1-8)
- **Expansion Boards**: Ether1616 boards
- **Expansion Numbering**: Starts at I/O 17

### AcornSix System  
- **Base I/O**: 16 inputs and 16 outputs (numbers 1-16)
- **Expansion Boards**: PLCEXP1616 boards
- **Expansion Numbering**: Starts at I/O 65

### Hickory System
- **Base I/O**: 32 inputs and 32 outputs (numbers 1-32)
- **Expansion Boards**: ECAT1616 boards
- **Expansion Numbering**: Starts at I/O 129

## System Detection

### Detecting System Type
```csharp
cncPipe.system.GetUnlockVersion(out CNCPipe.Sys.UnlockVersions version);

bool isAcorn = version.ToString().Contains("ACORN") && !version.ToString().Contains("ACORN_SIX");
bool isAcornSix = version.ToString().Contains("ACORN_SIX");  
bool isHickory = version.ToString().Contains("HICKORY");

Console.WriteLine($"System type detected: {version}");
```

### System-Specific Expansion Board Detection

#### Acorn Systems - Ether1616 Boards
```csharp
cncPipe.system.GetEther1616DeviceInfo(out List<CNCPipe.Sys.Ether1616Device> devices);
int ether1616Count = devices?.Count ?? 0;

Console.WriteLine($"Found {ether1616Count} Ether1616 expansion boards");

// Each device has properties like:
foreach (var device in devices)
{
    Console.WriteLine($"Device {device.DeviceNumber}: IP {device.IP}");
    // Note: Wizard code shows StartingIONumber = 32 + (DeviceNumber * 16) 
    // But the standard algorithm uses 17 + (board * 16)
}
```

#### AcornSix Systems - PLCEXP1616 Boards  
```csharp
cncPipe.system.GetPLCEXP1616NumberofDevices(out int plcExpCount);

Console.WriteLine($"Found {plcExpCount} PLCEXP1616 expansion boards");
```

#### Hickory Systems - ECAT1616 Boards
```csharp
cncPipe.system.GetECAT1616NumberOfDevices(out int ecatCount);

Console.WriteLine($"Found {ecatCount} ECAT1616 expansion boards");
```

## I/O Number Calculation Algorithms

### Acorn System I/O Calculation
```csharp
public static int[] GetAvailableInputsAcorn(CNCPipe cncPipe)
{
    var availableInputs = new List<int>();
    
    // Base I/O available on all Acorn systems (inputs 1-8)
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
    // 1 Ether1616 board:  [1, 2, 3, 4, 5, 6, 7, 8, 17, 18, 19, ..., 32]
    // 2 Ether1616 boards: [1, 2, 3, 4, 5, 6, 7, 8, 17, 18, 19, ..., 32, 33, 34, ..., 48]
}
```

### AcornSix System I/O Calculation
```csharp
public static int[] GetAvailableInputsAcornSix(CNCPipe cncPipe)
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

### Hickory System I/O Calculation
```csharp
public static int[] GetAvailableInputsHickory(CNCPipe cncPipe)
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

## Universal I/O Detection Implementation

### Complete System-Agnostic Implementation
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

### I/O Availability Checking
```csharp
public static bool IsInputAvailable(CNCPipe cncPipe, int inputNumber)
{
    int[] availableInputs = GetAllAvailableInputs(cncPipe);
    return Array.IndexOf(availableInputs, inputNumber) >= 0;
}

public static bool IsOutputAvailable(CNCPipe cncPipe, int outputNumber)
{
    int[] availableOutputs = GetAllAvailableOutputs(cncPipe);
    return Array.IndexOf(availableOutputs, outputNumber) >= 0;
}
```

## Board Information Class

### Comprehensive Board Information
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
        return $"{SystemType}: {TotalInputs} inputs ({BaseInputs} base + {ExpansionInputs} expansion), " +
               $"{TotalOutputs} outputs ({BaseOutputs} base + {ExpansionOutputs} expansion)";
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

## Key I/O Numbering Rules

### Base I/O Numbering
- All systems start at I/O number 1
- **Acorn**: 1-8 (8 total)
- **AcornSix**: 1-16 (16 total)  
- **Hickory**: 1-32 (32 total)

### Expansion Starting Points
Each system has a specific starting point for expansion I/O:
- **Acorn**: Expansion starts at 17
- **AcornSix**: Expansion starts at 65
- **Hickory**: Expansion starts at 129

### Expansion Board Capacity
All expansion boards provide 16 I/O each:
- **Ether1616**: 16 inputs + 16 outputs
- **PLCEXP1616**: 16 inputs + 16 outputs
- **ECAT1616**: 16 inputs + 16 outputs

### Sequential Board Numbering
Multiple expansion boards are numbered sequentially:
- **Board 0**: startIO + (0 * 16) = startIO to startIO + 15
- **Board 1**: startIO + (1 * 16) = startIO + 16 to startIO + 31
- **Board 2**: startIO + (2 * 16) = startIO + 32 to startIO + 47

### Input and Output Symmetry
- Input and output numbering follows identical patterns
- Each expansion board adds the same count to both inputs and outputs
- Available inputs and outputs use the same numbering schemes

## Practical Examples

### Complete System Detection Example
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

### Real-World Configuration Examples

#### Acorn with 2 Ether1616 Boards
```
Base I/O: [1, 2, 3, 4, 5, 6, 7, 8]
Board 0:  [17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32]
Board 1:  [33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48]
Total:    56 inputs, 56 outputs
```

#### AcornSix with 1 PLCEXP1616 Board
```
Base I/O: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16]
Board 0:  [65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80]
Total:    32 inputs, 32 outputs
```

#### Hickory with 1 ECAT1616 Board
```
Base I/O: [1, 2, 3, ..., 32]
Board 0:  [129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144]
Total:    48 inputs, 48 outputs
```

## Notes and Considerations

### Ether1616 Device Information
The `CNCPipe.Sys.Ether1616Device` class contains:
- `DeviceNumber`: Device identifier
- `IP`: IP address of the device

The Centroid Wizard code shows a different calculation for Ether1616 starting I/O numbers:
```csharp
StartingIONumber = 32 + (Convert.ToInt32(device.DeviceNumber) * 16)
```
This suggests there may be variations in I/O numbering depending on implementation context.

### Error Handling
System detection methods do not return error codes like parameter methods. They use void returns with out parameters. Always check for null device lists when working with Ether1616 devices.

### Performance
I/O detection involves multiple API calls and should not be called frequently. Consider caching results when possible.

---

*This documentation covers I/O detection and numbering for CentroidAPI. For general CentroidAPI usage, see the main CentroidAPI documentation.*