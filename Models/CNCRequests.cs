using System.Collections.Generic;

namespace HavenCNCServer.Models
{
    /// <summary>
    /// Request model for moving to a specific point
    /// </summary>
    public class MoveToRequest
    {
        /// <summary>
        /// Target point to move to
        /// </summary>
        public MachinePoint Point { get; set; } = new MachinePoint();

        /// <summary>
        /// XY movement speed (optional)
        /// </summary>
        public double? XYSpeed { get; set; }

        /// <summary>
        /// Z movement speed (optional)
        /// </summary>
        public double? ZSpeed { get; set; }
    }

    /// <summary>
    /// Request model for moving until an IO event occurs
    /// </summary>
    public class MoveToUntilRequest : MoveToRequest
    {
        /// <summary>
        /// IO event to wait for
        /// </summary>
        public IOEvent Event { get; set; }
    }

    /// <summary>
    /// Request model for directional movement until an event
    /// </summary>
    public class MoveDirectionUntilRequest
    {
        /// <summary>
        /// Direction to move
        /// </summary>
        public MoveDirection Direction { get; set; }

        /// <summary>
        /// IO event to wait for
        /// </summary>
        public IOEvent Event { get; set; }

        /// <summary>
        /// Movement speed (optional)
        /// </summary>
        public double? Speed { get; set; }
    }

    /// <summary>
    /// Request model for setting fixture to a point
    /// </summary>
    public class SetFixtureRequest
    {
        /// <summary>
        /// Fixture number (e.g., "G54", "G55", etc.)
        /// </summary>
        public string FixtureNumber { get; set; } = string.Empty;

        /// <summary>
        /// Point to set the fixture to
        /// </summary>
        public MachinePoint Point { get; set; } = new MachinePoint();
    }

    /// <summary>
    /// Request model for program execution
    /// </summary>
    public class ProgramExecuteRequest
    {
        /// <summary>
        /// Program name or identifier
        /// </summary>
        public string ProgramName { get; set; } = string.Empty;

        /// <summary>
        /// Optional starting line number
        /// </summary>
        public int? StartLine { get; set; }
    }

    /// <summary>
    /// Request model for setting IO override
    /// </summary>
    public class IOOverrideRequest
    {
        /// <summary>
        /// Input/Output number
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// Override value
        /// </summary>
        public bool Value { get; set; }
    }

    /// <summary>
    /// Request model for configuration data operations
    /// </summary>
    public class ConfigurationDataRequest
    {
        /// <summary>
        /// Name of the data item
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Content/value of the data item
        /// </summary>
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for saving checkpoints
    /// </summary>
    public class CheckpointSaveRequest
    {
        /// <summary>
        /// Name of the checkpoint
        /// </summary>
        public string CheckpointName { get; set; } = string.Empty;

        /// <summary>
        /// Array of data items to save in the checkpoint
        /// </summary>
        public IEnumerable<ConfigurationDataRequest> Data { get; set; } = new List<ConfigurationDataRequest>();
    }

    /// <summary>
    /// Request model for setting output values
    /// </summary>
    public class SetOutputRequest
    {
        /// <summary>
        /// Output number
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// Output value
        /// </summary>
        public bool Value { get; set; }
    }

    /// <summary>
    /// Request model for loading G-code
    /// </summary>
    public class LoadGCodeRequest
    {
        /// <summary>
        /// G-code content
        /// </summary>
        public string GCode { get; set; } = string.Empty;

        /// <summary>
        /// Optional program name
        /// </summary>
        public string? ProgramName { get; set; }
    }

    /// <summary>
    /// Request model for IO override operations
    /// </summary>
    public class OverrideIORequest
    {
        /// <summary>
        /// IO number
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// Override value
        /// </summary>
        public bool Value { get; set; }
    }
}
