using System.Text.Json;
using HavenCNCServer.Models;

namespace HavenCNCServer.Services
{
    /// <summary>
    /// Manages application settings with JSON file persistence
    /// </summary>
    public static class SettingsManager
    {
        private static readonly string _settingsFilePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "settings.json");

        private static AppSettings? _settings;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets the current application settings, loading from file if necessary
        /// </summary>
        public static AppSettings Settings
        {
            get
            {
                if (_settings == null)
                {
                    LoadSettings();
                }
                return _settings!;
            }
        }

        /// <summary>
        /// Load settings from JSON file or create default settings
        /// </summary>
        public static void LoadSettings()
        {
            lock (_lock)
            {
                try
                {
                    Console.WriteLine($"Loading settings from: {_settingsFilePath}");

                    if (File.Exists(_settingsFilePath))
                    {
                        Console.WriteLine($"Settings file exists, reading...");
                        string json = File.ReadAllText(_settingsFilePath);
                        _settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            WriteIndented = true
                        });

                        Console.WriteLine($"Loaded CNC12 path from settings: {_settings?.Cnc?.Cnc12Path ?? "NULL"}");

                        // Validate and fix any missing or invalid settings
                        ValidateSettings();
                    }
                    else
                    {
                        Console.WriteLine($"Settings file does not exist, creating defaults...");
                        // Create default settings
                        _settings = new AppSettings();
                        ValidateSettings();
                        SaveSettings(); // Save default settings to file
                        Console.WriteLine($"Created default settings with CNC12 path: {_settings.Cnc.Cnc12Path}");
                    }
                }
                catch (Exception ex)
                {
                    // If settings file is corrupted, create new default settings
                    Console.WriteLine($"Error loading settings: {ex.Message}. Using default settings.");
                    _settings = new AppSettings();
                    ValidateSettings();

                    try
                    {
                        SaveSettings(); // Try to save corrected settings
                    }
                    catch
                    {
                        // If we can't save, at least we have working defaults
                    }
                }
            }
        }

        /// <summary>
        /// Save current settings to JSON file
        /// </summary>
        public static void SaveSettings()
        {
            lock (_lock)
            {
                try
                {
                    if (_settings == null)
                        return;

                    // Ensure directory exists
                    string directory = Path.GetDirectoryName(_settingsFilePath)!;
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    string json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    File.WriteAllText(_settingsFilePath, json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving settings: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// Validate and fix settings values
        /// </summary>
        private static void ValidateSettings()
        {
            if (_settings == null)
                return;

            // Ensure temp directory exists
            if (!Directory.Exists(_settings.Files.TempFilesDirectory))
            {
                try
                {
                    Directory.CreateDirectory(_settings.Files.TempFilesDirectory);
                }
                catch
                {
                    // Fall back to system temp if custom directory fails
                    _settings.Files.TempFilesDirectory = Path.GetTempPath();
                }
            }

            // Ensure log directory exists (resolve relative to data directory)
            if (!Path.IsPathRooted(_settings.Logging.LogDirectory))
            {
                var dataDir = _settings.Files.DataDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), "data");
                _settings.Logging.LogDirectory = Path.Combine(dataDir, _settings.Logging.LogDirectory);
            }

            if (!Directory.Exists(_settings.Logging.LogDirectory))
            {
                try
                {
                    Directory.CreateDirectory(_settings.Logging.LogDirectory);
                }
                catch
                {
                    // Fall back to temp directory for logs if custom directory fails
                    _settings.Logging.LogDirectory = Path.Combine(Path.GetTempPath(), "HavenCNCServer", "Logs");
                    Directory.CreateDirectory(_settings.Logging.LogDirectory);
                }
            }

            // Validate CNC programs directory if specified
            if (!string.IsNullOrEmpty(_settings.Files.CncProgramsDirectory) &&
                !Directory.Exists(_settings.Files.CncProgramsDirectory))
            {
                // Reset to null to trigger auto-detection
                _settings.Files.CncProgramsDirectory = null;
            }

            // Validate numeric settings
            if (_settings.Files.MaxTempFiles < 1)
                _settings.Files.MaxTempFiles = 50;

            if (_settings.Logging.MaxLogFileSizeMB < 1)
                _settings.Logging.MaxLogFileSizeMB = 10;

            if (_settings.Logging.MaxLogFiles < 1)
                _settings.Logging.MaxLogFiles = 5;

            if (_settings.Cnc.ConnectionTimeoutMs < 1000)
                _settings.Cnc.ConnectionTimeoutMs = 10000;

            if (_settings.Cnc.ConnectionRetries < 1)
                _settings.Cnc.ConnectionRetries = 3;

            if (_settings.Cnc.RetryDelayMs < 100)
                _settings.Cnc.RetryDelayMs = 1000;

            // Validate log level
            var validLogLevels = new[] { "Debug", "Info", "Warning", "Error" };
            if (!validLogLevels.Contains(_settings.Logging.LogLevel))
                _settings.Logging.LogLevel = "Info";
        }

        /// <summary>
        /// Get the CNC programs directory, with auto-detection if not specified
        /// </summary>
        public static string GetCncProgramsDirectory()
        {
            if (!string.IsNullOrEmpty(Settings.Files.CncProgramsDirectory) &&
                Directory.Exists(Settings.Files.CncProgramsDirectory))
            {
                return Settings.Files.CncProgramsDirectory;
            }

            // Auto-detect CNC12 programs directory
            string documentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CNC12", "Programs");
            if (Directory.Exists(documentsPath))
            {
                return documentsPath;
            }

            string defaultPath = @"C:\CNC12\Programs";
            if (Directory.Exists(defaultPath))
            {
                return defaultPath;
            }

            // Fall back to temp directory
            return Settings.Files.TempFilesDirectory;
        }

        /// <summary>
        /// Reset settings to defaults
        /// </summary>
        public static void ResetToDefaults()
        {
            lock (_lock)
            {
                _settings = new AppSettings();
                ValidateSettings();
                SaveSettings();
            }
        }

        /// <summary>
        /// Get the settings file path
        /// </summary>
        public static string GetSettingsFilePath() => _settingsFilePath;
    }
}