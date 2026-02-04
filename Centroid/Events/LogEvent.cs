using System;

namespace HavenCNCServer.Centroid.Events
{
    /// <summary>
    /// Event for log messages from the CNC system
    /// </summary>
    public class LogEvent : ICentroidEvent
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
