using System;

namespace HavenCNCServer.Centriod.Events
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
    }
}