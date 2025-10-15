using System;

namespace HavenCNCServer.Centriod.Events
{
    /// <summary>
    /// Stored message with timestamp for message history
    /// </summary>
    public class StoredMessage
    {
        /// <summary>
        /// Timestamp when the message was stored
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// The CNC event that was stored
        /// </summary>
        public ICentroidEvent Event { get; set; } = null!;
        
        /// <summary>
        /// Type of communication that generated this message
        /// </summary>
        public string CommunicationType { get; set; } = string.Empty;
        
        /// <summary>
        /// Timestamp in milliseconds since Unix epoch for time-based filtering
        /// </summary>
        public long TimestampMs => ((DateTimeOffset)Timestamp).ToUnixTimeMilliseconds();

        /// <summary>
        /// Creates a new stored message with the current timestamp
        /// </summary>
        /// <param name="centroidEvent">The CNC event to store</param>
        /// <param name="commType">The communication type</param>
        public StoredMessage(ICentroidEvent centroidEvent, string commType)
        {
            Timestamp = DateTime.Now;
            Event = centroidEvent;
            CommunicationType = commType;
        }
    }
}