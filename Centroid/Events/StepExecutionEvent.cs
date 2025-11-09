using System;

namespace HavenCNCServer.Centroid.Events
{
    /// <summary>
    /// Event fired during step run execution to indicate current line
    /// </summary>
    public class StepExecutionEvent : ICentroidEvent, ISignalRSerializable
    {
        /// <summary>
        /// Timestamp when the step was executed
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Message associated with the step execution
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Type of message/event
        /// </summary>
        public string MessageType { get; set; } = "StepExecutionEvent";

        /// <summary>
        /// Unique identifier for the job
        /// </summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        /// Current line number being executed (1-based)
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// The G-code line being executed
        /// </summary>
        public string CurrentLine { get; set; } = string.Empty;

        /// <summary>
        /// Total number of lines in the job
        /// </summary>
        public int TotalLines { get; set; }

        /// <summary>
        /// Whether this is the last step in the job
        /// </summary>
        public bool IsLastStep { get; set; }

        /// <summary>
        /// Step execution status
        /// </summary>
        public StepExecutionStatus Status { get; set; }

        /// <summary>
        /// Serialize the step execution event for SignalR transmission
        /// </summary>
        /// <returns>The event itself for complete data transmission</returns>
        public object ToSignalRData()
        {
            return this;
        }
    }
}