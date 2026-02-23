using HavenCNCServer.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Services
{
    /// <summary>
    /// Manages CAM project file storage in local filesystem
    /// Pattern follows JobFileManager
    /// </summary>
    public class CamFileManager
    {
        private readonly string _baseDirectory;
        private const string CamSubdirectory = "cam";

        public CamFileManager()
        {
            _baseDirectory = Path.Combine(@"C:\havencncdata", CamSubdirectory);

            // Ensure directory exists
            if (!Directory.Exists(_baseDirectory))
            {
                Directory.CreateDirectory(_baseDirectory);
                Log($"Created CAM directory: {_baseDirectory}", LogLevel.Info, "CamFileManager");
            }
        }

        /// <summary>
        /// Get the file path for a CAM project
        /// </summary>
        public string GetFilePath(string projectId)
        {
            return Path.Combine(_baseDirectory, $"{projectId}.json");
        }

        /// <summary>
        /// Get the version file path for a CAM project
        /// </summary>
        public string GetVersionFilePath(string projectId)
        {
            return Path.Combine(_baseDirectory, $"{projectId}.json.version");
        }

        /// <summary>
        /// Check if CAM project exists locally
        /// </summary>
        public bool Exists(string projectId)
        {
            return File.Exists(GetFilePath(projectId));
        }

        /// <summary>
        /// Read CAM project data from local file
        /// </summary>
        public async Task<string?> ReadAsync(string projectId)
        {
            try
            {
                var filePath = GetFilePath(projectId);
                if (!File.Exists(filePath))
                {
                    Log($"CAM project file not found: {projectId}", LogLevel.Debug, "CamFileManager");
                    return null;
                }

                var data = await File.ReadAllTextAsync(filePath);
                Log($"Read CAM project from local file: {projectId}, {data.Length} bytes", LogLevel.Debug, "CamFileManager");
                return data;
            }
            catch (Exception ex)
            {
                Log($"Error reading CAM project file {projectId}: {ex.Message}", LogLevel.Error, "CamFileManager");
                return null;
            }
        }

        /// <summary>
        /// Write CAM project data to local file
        /// </summary>
        public async Task<bool> WriteAsync(string projectId, string data)
        {
            try
            {
                var filePath = GetFilePath(projectId);
                await File.WriteAllTextAsync(filePath, data);
                Log($"Wrote CAM project to local file: {projectId}, {data.Length} bytes", LogLevel.Info, "CamFileManager");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Error writing CAM project file {projectId}: {ex.Message}", LogLevel.Error, "CamFileManager");
                return false;
            }
        }

        /// <summary>
        /// Delete CAM project file and version file
        /// </summary>
        public async Task<bool> DeleteAsync(string projectId)
        {
            try
            {
                var filePath = GetFilePath(projectId);
                var versionPath = GetVersionFilePath(projectId);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                if (File.Exists(versionPath))
                {
                    File.Delete(versionPath);
                }

                Log($"Deleted CAM project files: {projectId}", LogLevel.Info, "CamFileManager");
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Log($"Error deleting CAM project files {projectId}: {ex.Message}", LogLevel.Error, "CamFileManager");
                return false;
            }
        }

        /// <summary>
        /// Get current version number
        /// </summary>
        public async Task<long> GetVersionAsync(string projectId)
        {
            try
            {
                var versionPath = GetVersionFilePath(projectId);
                if (!File.Exists(versionPath))
                {
                    return 0;
                }

                var versionText = await File.ReadAllTextAsync(versionPath);
                var versionObj = JsonSerializer.Deserialize<VersionFile>(versionText);
                return versionObj?.Version ?? 0;
            }
            catch (Exception ex)
            {
                Log($"Error reading version file for CAM project {projectId}: {ex.Message}", LogLevel.Warning, "CamFileManager");
                return 0;
            }
        }

        /// <summary>
        /// Write version number
        /// </summary>
        public async Task<bool> WriteVersionAsync(string projectId, long version)
        {
            try
            {
                var versionPath = GetVersionFilePath(projectId);
                var versionObj = new VersionFile { Version = version };
                var versionText = JsonSerializer.Serialize(versionObj);
                await File.WriteAllTextAsync(versionPath, versionText);
                return true;
            }
            catch (Exception ex)
            {
                Log($"Error writing version file for CAM project {projectId}: {ex.Message}", LogLevel.Error, "CamFileManager");
                return false;
            }
        }

        /// <summary>
        /// Increment and write version
        /// </summary>
        public async Task<long> IncrementAndWriteAsync(string projectId, string data)
        {
            var currentVersion = await GetVersionAsync(projectId);
            var newVersion = currentVersion + 1;

            await WriteAsync(projectId, data);
            await WriteVersionAsync(projectId, newVersion);

            return newVersion;
        }

        /// <summary>
        /// Get all CAM project IDs in local storage
        /// </summary>
        public string[] GetAllProjectIds()
        {
            try
            {
                var files = Directory.GetFiles(_baseDirectory, "*.json");
                var projectIds = new System.Collections.Generic.List<string>();

                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    // Skip version files
                    if (fileName.EndsWith(".json.version", StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Remove .json extension to get project ID
                    var projectId = Path.GetFileNameWithoutExtension(file);
                    projectIds.Add(projectId);
                }

                return projectIds.ToArray();
            }
            catch (Exception ex)
            {
                Log($"Error listing CAM project IDs: {ex.Message}", LogLevel.Error, "CamFileManager");
                return Array.Empty<string>();
            }
        }

        private class VersionFile
        {
            public long Version { get; set; }
        }
    }
}
