using System;

namespace HavenCNCServer.Models
{
    /// <summary>
    /// System status information
    /// </summary>
    public class SystemStatus
    {
        /// <summary>
        /// Current server date and time
        /// </summary>
        public DateTime CurrentDateTime { get; set; }

        /// <summary>
        /// Whether the CNC pipe connection is active
        /// </summary>
        public bool IsCNCConnected { get; set; }

        /// <summary>
        /// Additional status message
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }
}