using System.Text.Json.Serialization;

namespace HavenCNCServer.Models
{
    /// <summary>
    /// Application settings configuration
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// File and directory settings
        /// </summary>
        public FileSettings Files { get; set; } = new();

        /// <summary>
        /// Logging configuration settings
        /// </summary>
        public LoggingSettings Logging { get; set; } = new();

        /// <summary>
        /// CNC and API settings
        /// </summary>
        public CncSettings Cnc { get; set; } = new();
    }

    /// <summary>
    /// File and directory path settings
    /// </summary>
    public class FileSettings
    {
        /// <summary>
        /// Directory for temporary G-code files
        /// Default: ./temp
        /// </summary>
        public string TempFilesDirectory { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "temp");

        /// <summary>
        /// Directory for CNC12 program files
        /// Default: ./cncfiles
        /// </summary>
        public string? CncProgramsDirectory { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "cncfiles");

        /// <summary>
        /// Default file extension for G-code files
        /// </summary>
        public string DefaultGCodeExtension { get; set; } = ".nc";

        /// <summary>
        /// Maximum number of temporary files to keep
        /// </summary>
        public int MaxTempFiles { get; set; } = 50;
    }

    /// <summary>
    /// Logging configuration settings
    /// </summary>
    public class LoggingSettings
    {
        /// <summary>
        /// Enable debug logging
        /// </summary>
        public bool EnableDebugLogging { get; set; } = false;

        /// <summary>
        /// Enable file logging
        /// </summary>
        public bool EnableFileLogging { get; set; } = true;

        /// <summary>
        /// Log file directory
        /// Default: %APPDATA%\HavenCNCServer\Logs
        /// </summary>
        public string LogDirectory { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "HavenCNCServer", 
            "Logs");

        /// <summary>
        /// Maximum log file size in MB
        /// </summary>
        public int MaxLogFileSizeMB { get; set; } = 10;

        /// <summary>
        /// Number of log files to retain
        /// </summary>
        public int MaxLogFiles { get; set; } = 5;

        /// <summary>
        /// Log level (Debug, Info, Warning, Error)
        /// </summary>
        public string LogLevel { get; set; } = "Info";
    }

    /// <summary>
    /// CNC and API configuration settings
    /// </summary>
    public class CncSettings
    {
        /// <summary>
        /// CentroidAPI connection timeout in milliseconds
        /// </summary>
        public int ConnectionTimeoutMs { get; set; } = 10000;

        /// <summary>
        /// Number of connection retry attempts
        /// </summary>
        public int ConnectionRetries { get; set; } = 3;

        /// <summary>
        /// Delay between retry attempts in milliseconds
        /// </summary>
        public int RetryDelayMs { get; set; } = 1000;

        /// <summary>
        /// Enable automatic API connection on startup
        /// </summary>
        public bool AutoConnectOnStartup { get; set; } = false;
    }
}