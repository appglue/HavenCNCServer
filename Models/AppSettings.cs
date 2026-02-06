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

        /// <summary>
        /// MongoDB configuration settings
        /// </summary>
        public MongoDbSettings MongoDB { get; set; } = new();
    }

    /// <summary>
    /// File and directory path settings
    /// </summary>
    public class FileSettings
    {
        /// <summary>
        /// Absolute path to the data directory for all configuration files
        /// Default: C:\havencncdata
        /// </summary>
        public string? DataDirectory { get; set; } = @"C:\havencncdata";

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
        /// Log file directory (relative to data directory)
        /// Default: logs
        /// </summary>
        public string LogDirectory { get; set; } = "logs";

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
        /// CNC12 installation directory path
        /// Default: C:\cncr
        /// </summary>
        public string Cnc12Path { get; set; } = @"C:\cncr";

        /// <summary>
        /// CNC12 process name to monitor (without .exe extension)
        /// Default: cncr
        /// </summary>
        public string Cnc12ProcessName { get; set; } = "cncr";

        /// <summary>
        /// User name for the CNC machine operator
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Machine name/identifier
        /// </summary>
        public string MachineName { get; set; } = string.Empty;

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

        /// <summary>
        /// Heartbeat interval in milliseconds for SignalR status broadcasts
        /// </summary>
        public int HeartbeatIntervalMs { get; set; } = 2000;

        /// <summary>
        /// CNC server executable settings
        /// </summary>
        public CncServerSettings Server { get; set; } = new();
    }

    /// <summary>
    /// CNC server executable management settings
    /// </summary>
    public class CncServerSettings
    {
        /// <summary>
        /// Path to the CNC server executable
        /// Default: \cncr\cncr.exe
        /// </summary>
        public string ExecutablePath { get; set; } = @"\cncr\cncr.exe";

        /// <summary>
        /// Whether to auto-start the CNC server on application startup
        /// </summary>
        public bool AutoStartServer { get; set; } = true;

        /// <summary>
        /// Whether to auto-restart the CNC server if it stops unexpectedly
        /// </summary>
        public bool AutoRestartServer { get; set; } = true;

        /// <summary>
        /// Whether to stop the CNC server when the application shuts down (only if we started it)
        /// </summary>
        public bool StopServerOnShutdown { get; set; } = true;

        /// <summary>
        /// Delay in milliseconds to wait before restarting the server
        /// </summary>
        public int RestartDelayMs { get; set; } = 5000;

        /// <summary>
        /// Interval in milliseconds to check if the server is still running
        /// </summary>
        public int MonitorIntervalMs { get; set; } = 10000;

        /// <summary>
        /// Command line arguments to pass to the CNC server executable
        /// </summary>
        public string[] Arguments { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Working directory for the CNC server process
        /// </summary>
        public string? WorkingDirectory { get; set; }
    }

    /// <summary>
    /// MongoDB configuration settings
    /// </summary>
    public class MongoDbSettings
    {
        /// <summary>
        /// Enable MongoDB synchronization
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// MongoDB connection string
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Database name for machine configurations
        /// </summary>
        public string DatabaseName { get; set; } = "HavenCNCMachineData";

        /// <summary>
        /// Collection name for machine configurations
        /// </summary>
        public string MachineConfigurationsCollection { get; set; } = "machineConfigurations";

        /// <summary>
        /// Collection name for default PLC versions
        /// </summary>
        public string DefaultPlcVersionsCollection { get; set; } = "defaultPlcVersions";

        /// <summary>
        /// Connection timeout in milliseconds
        /// </summary>
        public int ConnectionTimeoutMs { get; set; } = 5000;

        /// <summary>
        /// Whether to continue working when MongoDB is offline
        /// </summary>
        public bool ContinueOnOffline { get; set; } = true;
    }
}