using System;

namespace HavenCNCServer.Centroid.Events
{
    /// <summary>
    /// Base interface for all Centroid CNC events
    /// </summary>
    public interface ICentroidEvent
    {
        /// <summary>
        /// Timestamp when the event occurred
        /// </summary>
        DateTime Timestamp { get; set; }

        /// <summary>
        /// Message associated with the event
        /// </summary>
        string Message { get; set; }

        /// <summary>
        /// Type of message/event
        /// </summary>
        string MessageType { get; set; }
    }
}