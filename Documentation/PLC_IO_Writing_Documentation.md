# PLC Input/Output Writing Documentation

## Overview
This document describes how the Centroid Wizard application writes input and output definitions to PLC files, including the inversion logic system. The wizard uses a region-based approach to manage sections of the PLC file that it can modify while preserving the rest of the file content.

## Wizard Regions in PLC Files

### Region Structure
The wizard manages specific sections of the PLC file using "wizard regions" that are marked with special comments:

```plc
;------------------------------------------------------------------------------
;                        INPUT DEFINITIONS
;               Closed = 1 (green)  Open = 0 (red)
;------------------------------------------------------------------------------

; DO NOT MODIFY
; #wizardregion Inputs
EStopOk    IS INP8
; #endregion
```

### Region Types
The system supports multiple region types defined in the `WizardRegion.RegionName` enum:
- **Inputs** - Standard input definitions (INP1-INP8, etc.)
- **Outputs** - Standard output definitions (OUT1-OUT8, etc.)
- **UsbInput** - USB panel input definitions
- **UsbOutput** - USB panel output definitions
- **MemoryBits** - Memory bit definitions
- **Configuration** - Configuration settings
- **Drive** - Drive-specific settings
- **Date** - Timestamp information

## Input/Output Definition Writing Process

### 1. Data Collection and Building
The writing process begins with the `Plc.Build.Manager` class, which orchestrates the creation of all definition lines:

```csharp
public class Manager
{
    private readonly List<LineRegion> buildRegions;
    
    public Manager(Definitions definitions, Configuration.Manager configuration)
    {
        buildRegions = new List<LineRegion>()
        {
            new Lines.Input(definitions),
            new Lines.Output(definitions),
            new Lines.MemoryBit(definitions),
            new Lines.Configuration(definitions),
            new Lines.Drive(configuration),
            new Lines.UsbInput(definitions),
            new Lines.UsbOutput(definitions)
        };
    }
}
```

### 2. Input Definition Line Construction
The `Lines.Input` class handles standard input definitions:

```csharp
public override void Build()
{
    var inputs = new List<Definition>(definitions.Selected.Where(x => x.Function is Function.Input && !(x.Function is Function.UsbInput)));
    
    // Sort based on IONumber
    inputs.Sort((a, b) => a.IONumber.CompareTo(b.IONumber));
    
    Content = new List<string>(Manager.ConstructDefinitionLines(inputs, DefineLine));
}
```

**Key Points:**
- Filters for Input functions (excluding USB inputs)
- Sorts by IO number for consistent ordering
- Uses "IS INP" as the definition line prefix

### 3. Output Definition Line Construction
The `Lines.Output` class handles standard output definitions:

```csharp
public override void Build()
{
    var outputs = new List<Definition>(definitions.Selected.Where(x => x.Function is Function.Output));
    
    // Sort based on IONumber
    outputs.Sort((a, b) => a.IONumber.CompareTo(b.IONumber));
    
    Content = new List<string>(Manager.ConstructDefinitionLines(outputs, DefineLine));
}
```

**Key Points:**
- Filters for Output functions
- Sorts by IO number for consistent ordering
- Uses "IS OUT" as the definition line prefix

### 4. Line Construction Details
The `LineConstructor` class formats individual definition lines:

```csharp
public string ConstructDefinitionLine(string defType, int number)
{
    var stringBuilder = new StringBuilder(definition.Function.Name);
    
    AddSpaces(stringBuilder);
    AddExtraSpaces(stringBuilder, longestName - definition.Function.Name.Length);
    
    stringBuilder.Append(defType + number);
    
    return stringBuilder.ToString();
}
```

**Output Format:**
```
FunctionName    IS INP1
LongerName      IS INP2
VeryLongName    IS INP3
```

**Formatting Rules:**
- 4 spaces minimum between function name and definition
- Additional spaces added to align all definitions
- Alignment based on the longest function name in the group

## Input Inversion System

### Inversion State Management
Each input definition has an inversion state controlled by the `Definition.State` property:

```csharp
public enum InputType
{
    NormallyOpen,     // Red indicator - Input is inverted
    NormallyClosed    // Green indicator - Input is not inverted
}

public class Definition
{
    public InputType State { get; set; } = InputType.NormallyClosed;
}
```

### Visual Representation
The UI uses color coding to indicate inversion state:
- **Green Circle**: NormallyClosed (not inverted) - Input reads 1 when closed
- **Red Circle**: NormallyOpen (inverted) - Input reads 1 when open
- **Black Circle**: Logic determined by touch device menus

### Parameter Storage System
Input inversion states are stored in CNC12 parameters 911-915:

```csharp
public void UpdatePlcInputInversion(IEnumerable<Plc.Definition> inputs)
{
    const int ioBankSize = 16;
    
    foreach (var input in inputs)
    {
        int paramNum = Convert.ToInt32(CNC12Parameters.INPUT_INVERSION_START_PARM + (input.IONumber - 1) / ioBankSize);
        int bit = (input.IONumber - 1) & (ioBankSize - 1);
        int value = Convert.ToInt32(CNCUtils.GetParameterValue(paramNum));
        
        bool isInverted = CNCUtils.IsBitSet(value, bit);
        input.State = isInverted ? InputType.NormallyOpen : InputType.NormallyClosed;
    }
}
```

**Parameter Mapping:**
- **Parameter 911**: Inputs 1-16 (bits 0-15)
- **Parameter 912**: Inputs 17-32 (bits 0-15)
- **Parameter 913**: Inputs 33-48 (bits 0-15)
- **Parameter 914**: Inputs 49-64 (bits 0-15)
- **Parameter 915**: Inputs 65-80 (bits 0-15)

**Bit Encoding:**
- Bit = 0: Input is NormallyClosed (not inverted)
- Bit = 1: Input is NormallyOpen (inverted)

### USB Input Inversion
USB inputs use a different inversion system:

```csharp
public void UpdateUsbInputInversion(IEnumerable<Plc.Definition> usbInputs)
{
    MainWindow.skin.state.GetUsbInputInversions(out string inversionBits);
    MainWindow.mainWindow.inversionBits = inversionBits.ToCharArray();
    
    foreach (var usbInput in usbInputs)
    {
        if (MainWindow.mainWindow.inversionBits[usbInput.IONumber - 1] == '0')
        {
            isInverted = true;  // NormallyClosed
        }
        else
        {
            isInverted = false; // NormallyOpen
        }
    }
}
```

**Key Differences:**
- Uses `GetUsbInputInversions()` API call instead of parameters
- Returns a string of '0' and '1' characters
- '0' = NormallyClosed (inverted), '1' = NormallyOpen (not inverted)

## File Writing Process

### 1. Region Identification
The `WizardRegion` class locates existing regions in the PLC file:

```csharp
public WizardRegion(RegionName name, List<string> file)
{
    string starttoken = @"\#wizardregion " + Name;
    string endtoken = @"\#endregion";
    
    ContentBegin = FindContentStartLine(file, starttoken);
    ContentEnd = FindContentStartLine(file, ContentBegin, endtoken);
}
```

### 2. Content Replacement
The `Writer` class replaces region content:

```csharp
private void WriteRegions()
{
    foreach (WizardRegion.RegionName name in Enum.GetValues(typeof(WizardRegion.RegionName)))
    {
        WizardRegion region = Parser.FindWizardRegion(name, file);
        
        if (name != WizardRegion.RegionName.Date)
        {
            RemoveOldDefinitions(region);
            
            var regionContent = new List<string>();
            if (builder.TryGetBuildRegionContent(name, regionContent))
            {
                file.InsertRange(region.ContentBegin, regionContent);
            }
        }
    }
}
```

**Process Steps:**
1. Find the wizard region markers in the file
2. Remove existing content between the markers
3. Insert new generated content
4. Preserve all other file content

### 3. Region Auto-Creation
If wizard regions don't exist, the system can automatically add them:

```csharp
public static bool UpdateWizardRegions(List<string> file, WizardRegion.RegionName regionName)
{
    // Search for existing INP/OUT definitions
    var inputToken = new List<string>() { @"IS\s+\bINP1", @"IS\s+\bINP8\b" };
    var outputToken = new List<string>() { @"IS\s+\bOUT1", @"IS\s+\bOUT8\b" };
    
    // Add wizard region markers around existing definitions
    if (tokens.TryGetValue(regionName, out value))
    {
        startline = WizardRegion.FindContentStartLine(file, value[0]);
        endline = WizardRegion.FindContentStartLine(file, value[1]);
        
        if (startline != -1 && endline != -1)
        {
            AddWizardRegions(file, regionName, startline, endline);
        }
    }
}
```

## Error Handling and Validation

### Missing Regions
If a required wizard region is missing:
```csharp
if (ContentBegin == -1 || ContentEnd == -1)
{
    throw new ArgumentOutOfRangeException(Messages.MissingWizardRegion + $" {name}!");
}
```

### File I/O Errors
```csharp
private void TryWriteOutSourceFile()
{
    try
    {
        FileOps.TryWriteToFile(file, MainWindow.rootPath + @"\" + fileName);
    }
    catch (Exception e)
    {
        MessageUtils.ShowErrorMessage(Messages.PLCWriteError + "\n\n" + e.Message);
    }
}
```

## Summary
The PLC I/O writing system provides a robust framework for managing input and output definitions while preserving existing PLC file content. The inversion system allows for flexible signal logic configuration through both parameter-based storage for standard inputs and API-based management for USB inputs. The wizard region approach ensures that only specific sections of the PLC file are modified, maintaining the integrity of hand-written PLC code outside the managed regions.