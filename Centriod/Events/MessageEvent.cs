using System;

namespace HavenCNCServer.Centriod.Events
{
    /// <summary>
    /// Event containing a CNC message with error code and classification
    /// </summary>
    public class MessageEvent : ICentroidEvent 
    {
        /// <summary>
        /// Timestamp when the message event occurred
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Numeric error or message code from the CNC system
        /// </summary>
        public int EventCode { get; set; }
        
        /// <summary>
        /// Message text content
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Classified type of the message event
        /// </summary>
        public MessageEventType EventType { get; set; }
    }
}