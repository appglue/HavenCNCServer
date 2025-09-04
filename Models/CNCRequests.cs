using System.Collections.Generic;

namespace HavenCNCServer.Models
{
    /// <summary>
    /// Reque    /// <summary>
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
}ing to a specific point
    /// </summary>
    public class MoveToRequest
    {
        /// <summary>
        /// Target point to move to
        /// </summary>
        public CNCPoint Point { get; set; } = new CNCPoint();

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
        /// Maximum distance to move (optional)
        /// </summary>
        public double? MaxDistance { get; set; }

        /// <summary>
        /// Movement speed (optional)
        /// </summary>
        public double? Speed { get; set; }
    }

    /// <summary>
    /// Request model for setting fixture
    /// </summary>
    public class SetFixtureRequest
    {
        /// <summary>
        /// Fixture number
        /// </summary>
        public string FixtureNumber { get; set; } = string.Empty;

        /// <summary>
        /// Point to set the fixture to
        /// </summary>
        public CNCPoint Point { get; set; } = new CNCPoint();
    }

    /// <summary>
    /// Request model for loading G-code
    /// </summary>
    public class LoadGCodeRequest
    {
        /// <summary>
        /// G-code lines to load
        /// </summary>
        public List<string> GCode { get; set; } = new List<string>();
    }

    /// <summary>
    /// Request model for setting output state
    /// </summary>
    public class SetOutputRequest
    {
        /// <summary>
        /// Output number
        /// </summary>
        public int OutputNumber { get; set; }

        /// <summary>
        /// Desired state (true = on, false = off)
        /// </summary>
        public bool State { get; set; }
    }

    /// <summary>
    /// Request model for input/output overrides (testing only)
    /// </summary>
    public class OverrideIORequest
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
}
