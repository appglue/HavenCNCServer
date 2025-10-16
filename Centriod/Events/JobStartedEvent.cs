using System;

namespace HavenCNCServer.Centriod.Events
{
    /// <summary>
    /// Event fired when a job is started with G-code
    /// </summary>
    public class JobStartedEvent : ICentroidEvent, ISignalRSerializable
    {
        /// <summary>
        /// Timestamp when the job started
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Message associated with the job start event
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Unique identifier for the job
        /// </summary>
        public string JobId { get; set; } = string.Empty;
        
        /// <summary>
        /// G-code lines for the job
        /// </summary>
        public string[] GCodeLines { get; set; } = Array.Empty<string>();
        
        /// <summary>
        /// Total number of lines in the job
        /// </summary>
        public int TotalLines { get; set; }
        
        /// <summary>
        /// Whether this is a step run job
        /// </summary>
        public bool IsStepRunMode { get; set; }
        
        /// <summary>
        /// File path if job was loaded from file
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// Serialize the job started event for SignalR transmission
        /// </summary>
        /// <returns>The event itself for complete data transmission</returns>
        public object ToSignalRData()
        {
            return this;
        }
    }
}