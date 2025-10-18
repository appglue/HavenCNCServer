using System;

namespace HavenCNCServer.Models
{
    /// <summary>
    /// Storage type enumeration
    /// </summary>
    public enum StorageType
    {
        /// <summary>
        /// Flow-based program storage
        /// </summary>
        Flow,

        /// <summary>
        /// MDI (Manual Data Input) command storage
        /// </summary>
        MDI
    }

    /// <summary>
    /// Represents a stored CNC program or MDI command
    /// </summary>
    public class ProgramStorage
    {
        /// <summary>
        /// Unique name identifier for the storage item
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Last updated timestamp
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Whether this storage should be exposed as an action
        /// </summary>
        public bool ExposeAsAction { get; set; } = false;

        /// <summary>
        /// Action name for exposure (if ExposeAsAction is true)
        /// </summary>
        public string ActionName { get; set; } = string.Empty;

        /// <summary>
        /// Program or command data content
        /// </summary>
        public string Data { get; set; } = string.Empty;

        /// <summary>
        /// Type of storage (Flow or MDI)
        /// </summary>
        public StorageType StorageType { get; set; } = StorageType.Flow;
    }
}
