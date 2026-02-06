using System;

namespace HavenCNCServer.Centroid.Events
{
    /// <summary>
    /// Event containing server status information (sent every 2 seconds as heartbeat)
    /// </summary>
    public class ServerStatusEvent : ICentroidEvent
    {
        /// <summary>
        /// Timestamp when the status was generated
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Whether the CNC is connected
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// Whether the API is restricted
        /// </summary>
        public bool IsApiRestricted { get; set; }

        /// <summary>
        /// Whether a job is currently running
        /// </summary>
        public bool IsJobRunning { get; set; }

        /// <summary>
        /// Whether the machine requires reset (SV_STOP is active)
        /// </summary>
        public bool RequiresReset { get; set; }

        /// <summary>
        /// Whether the machine is homed
        /// </summary>
        public bool? IsHomed { get; set; }

        /// <summary>
        /// Type of message/event
        /// </summary>
        public string MessageType { get; set; } = "ServerStatus";

        /// <summary>
        /// Message content for ICentroidEvent interface
        /// </summary>
        public string Message { get; set; } = "Server Status Update";
    }
}
