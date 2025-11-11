using System;

namespace HavenCNCServer.Centroid.Events
{
    /// <summary>
    /// Event fired when a job is completed
    /// </summary>
    public class JobCompletedEvent : ICentroidEvent, ISignalRSerializable
    {
        /// <summary>
        /// Timestamp when the job completed
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Message associated with the job completion event
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Type of message/event
        /// </summary>
        public string MessageType { get; set; } = "JobCompletedEvent";

        /// <summary>
        /// Unique identifier for the job
        /// </summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        /// Whether the job completed successfully
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if job failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Duration of job execution
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Total lines executed
        /// </summary>
        public int LinesExecuted { get; set; }

        /// <summary>
        /// File path of the G-code file that was executed
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// Serialize the job completed event for SignalR transmission
        /// </summary>
        /// <returns>The event itself for complete data transmission</returns>
        public object ToSignalRData()
        {
            return this;
        }
    }
}