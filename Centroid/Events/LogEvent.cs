using System;

namespace HavenCNCServer.Centroid.Events
{
    /// <summary>
    /// Event for log messages from the CNC system
    /// </summary>
    public class LogEvent : ICentroidEvent, ISignalRSerializable
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; } = string.Empty;
        public string MessageType { get; set; } = "LOG";
        public LogLevel Level { get; set; }
        public string Source { get; set; } = string.Empty;

        public LogEvent()
        {
            Timestamp = DateTime.Now;
        }

        public LogEvent(string message, LogLevel level = LogLevel.Info, string source = "CNC")
        {
            Timestamp = DateTime.Now;
            Message = message;
            Level = level;
            Source = source;
        }

        /// <summary>
        /// Serialize this event for SignalR transmission
        /// </summary>
        public object ToSignalRData()
        {
            return new
            {
                MessageType = "LOG",
                Timestamp = Timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Message,
                Level = Level.ToString(),
                Source
            };
        }
    }

    /// <summary>
    /// Log level for log events
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }
}
