using System;
using System.IO;
using System.Text.Json;
using HavenCNCServer.Models;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Services
{
    /// <summary>
    /// Simple configuration file manager - handles version tracking and file I/O
    /// </summary>
    public class ConfigurationFileManager
    {
        private readonly string _dataDirectory;

        public ConfigurationFileManager(string dataDirectory)
        {
            _dataDirectory = dataDirectory;

            if (!Directory.Exists(_dataDirectory))
            {
                Directory.CreateDirectory(_dataDirectory);
            }
        }

        /// <summary>
        /// Get version number for a file (0 if file or version doesn't exist)
        /// </summary>
        public long GetVersion(string fileName)
        {
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var versionPath = Path.Combine(_dataDirectory, $"{fileNameWithoutExt}.version.json");

            if (!File.Exists(versionPath))
                return 0;

            try
            {
                var json = File.ReadAllText(versionPath);
                var versionData = JsonSerializer.Deserialize<FileVersion>(json);
                return versionData?.Version ?? 0;
            }
            catch (Exception ex)
            {
                LogError($"Failed to read version for {fileName}: {ex.Message}", "ConfigFileManager");
                return 0;
            }
        }

        /// <summary>
        /// Read data from file
        /// </summary>
        public string? ReadData(string fileName)
        {
            var dataPath = Path.Combine(_dataDirectory, fileName);

            if (!File.Exists(dataPath))
                return null;

            try
            {
                return File.ReadAllText(dataPath);
            }
            catch (Exception ex)
            {
                LogError($"Failed to read {fileName}: {ex.Message}", "ConfigFileManager");
                return null;
            }
        }

        /// <summary>
        /// Write data to file and update version
        /// </summary>
        public void WriteData(string fileName, string data, long version)
        {
            var dataPath = Path.Combine(_dataDirectory, fileName);
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var versionPath = Path.Combine(_dataDirectory, $"{fileNameWithoutExt}.version.json");

            try
            {
                // Write data file
                File.WriteAllText(dataPath, data);

                // Write version file
                var versionData = new FileVersion { Version = version };
                var versionJson = JsonSerializer.Serialize(versionData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(versionPath, versionJson);

                LogInfo($"Wrote {fileName} v{version}", "ConfigFileManager");
            }
            catch (Exception ex)
            {
                LogError($"Failed to write {fileName}: {ex.Message}", "ConfigFileManager");
                throw;
            }
        }

        /// <summary>
        /// Increment version and write data
        /// </summary>
        public long IncrementAndWrite(string fileName, string data)
        {
            var currentVersion = GetVersion(fileName);
            var newVersion = currentVersion + 1;
            WriteData(fileName, data, newVersion);
            return newVersion;
        }

        /// <summary>
        /// Check if file exists
        /// </summary>
        public bool Exists(string fileName)
        {
            var dataPath = Path.Combine(_dataDirectory, fileName);
            return File.Exists(dataPath);
        }
    }

    /// <summary>
    /// Simple version tracking
    /// </summary>
    public class FileVersion
    {
        public long Version { get; set; }
    }
}
