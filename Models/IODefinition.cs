namespace HavenCNCServer.Models;

/// <summary>
/// CNC System types supported by Centroid
/// </summary>
public enum SystemType
{
    /// <summary>
    /// Unknown or unrecognized system type
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Acorn CNC controller (8 base I/O)
    /// </summary>
    Acorn = 1,

    /// <summary>
    /// Acorn Six CNC controller (16 base I/O)
    /// </summary>
    AcornSix = 2,

    /// <summary>
    /// Hickory CNC controller (32 base I/O)
    /// </summary>
    Hickory = 3
}

/// <summary>
/// Board configuration information including type and I/O capabilities
/// </summary>
public class BoardConfiguration
{
    /// <summary>
    /// Board type: "Acorn" or "Acorn6"
    /// </summary>
    public string BoardType { get; set; } = "Acorn";

    /// <summary>
    /// Whether an extension board is installed
    /// </summary>
    public bool HasExtensionBoard { get; set; }

    /// <summary>
    /// Maximum number of available input ports
    /// </summary>
    public int MaxInputs { get; set; }

    /// <summary>
    /// Maximum number of available output ports
    /// </summary>
    public int MaxOutputs { get; set; }
}

/// <summary>
/// System information including board type and I/O capabilities
/// </summary>
public class SystemInfo
{
    /// <summary>
    /// System type
    /// </summary>
    public SystemType SystemType { get; set; } = SystemType.Unknown;

    /// <summary>
    /// Total number of inputs available
    /// </summary>
    public int TotalInputs { get; set; }

    /// <summary>
    /// Total number of outputs available
    /// </summary>
    public int TotalOutputs { get; set; }

    /// <summary>
    /// Number of base inputs (built-in to controller)
    /// </summary>
    public int BaseInputs { get; set; }

    /// <summary>
    /// Number of base outputs (built-in to controller)
    /// </summary>
    public int BaseOutputs { get; set; }

    /// <summary>
    /// Number of expansion inputs
    /// </summary>
    public int ExpansionInputs { get; set; }

    /// <summary>
    /// Number of expansion outputs
    /// </summary>
    public int ExpansionOutputs { get; set; }

    /// <summary>
    /// Number of expansion boards connected
    /// </summary>
    public int ExpansionBoardCount { get; set; }

    /// <summary>
    /// Unlock version string from CNC12
    /// </summary>
    public string UnlockVersion { get; set; } = string.Empty;
}

/// <summary>
/// Represents an input or output definition from the PLC source file
/// </summary>
public class IODefinition
{
    /// <summary>
    /// The symbolic name of the I/O (e.g., "EStopOk", "SpinFWD")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The I/O number (e.g., 1, 2, 65, etc.)
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// The type of I/O: "INPUT" or "OUTPUT"
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The raw definition line from the PLC source
    /// </summary>
    public string RawDefinition { get; set; } = string.Empty;
}

/// <summary>
/// Represents a defined I/O with its current state
/// </summary>
public class DefinedIO
{
    /// <summary>
    /// The I/O number (e.g., 1, 2, 65, etc.)
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// The symbolic name of the I/O (e.g., "EStopOk", "SpinFWD")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Current state of the I/O (true = active/on, false = inactive/off)
    /// </summary>
    public bool State { get; set; }
}

/// <summary>
/// Response containing all I/O definitions from the PLC source file
/// </summary>
public class IODefinitionsResponse
{
    /// <summary>
    /// List of all input definitions
    /// </summary>
    public List<IODefinition> Inputs { get; set; } = new();

    /// <summary>
    /// List of all output definitions
    /// </summary>
    public List<IODefinition> Outputs { get; set; } = new();

    /// <summary>
    /// The file path that was parsed
    /// </summary>
    public string SourceFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the definitions were parsed
    /// </summary>
    public DateTime ParsedAt { get; set; } = DateTime.UtcNow;
}
