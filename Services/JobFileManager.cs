using HavenCNCServer.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HavenCNCServer.Services
{
    /// <summary>
    /// Manages job file storage in local filesystem
    /// Pattern follows ConfigurationFileManager
    /// </summary>
    public class JobFileManager
    {
        private readonly ILogger<JobFileManager>? _logger;
        private readonly string _baseDirectory;
        private const string JobsSubdirectory = "jobs";

        public JobFileManager(ILogger<JobFileManager>? logger)
        {
            _logger = logger;
            _baseDirectory = Path.Combine(@"C:\havencncdata", JobsSubdirectory);

            // Ensure directory exists
            if (!Directory.Exists(_baseDirectory))
            {
                Directory.CreateDirectory(_baseDirectory);
                _logger?.LogInformation("Created jobs directory: {Directory}", _baseDirectory);
            }
        }

        /// <summary>
        /// Get the file path for a job
        /// </summary>
        public string GetFilePath(string jobId)
        {
            return Path.Combine(_baseDirectory, $"{jobId}.json");
        }

        /// <summary>
        /// Get the version file path for a job
        /// </summary>
        public string GetVersionFilePath(string jobId)
        {
            return Path.Combine(_baseDirectory, $"{jobId}.json.version");
        }

        /// <summary>
        /// Check if job exists locally
        /// </summary>
        public bool Exists(string jobId)
        {
            return File.Exists(GetFilePath(jobId));
        }

        /// <summary>
        /// Read job data from local file
        /// </summary>
        public async Task<string?> ReadAsync(string jobId)
        {
            try
            {
                var filePath = GetFilePath(jobId);
                if (!File.Exists(filePath))
                {
                    _logger?.LogDebug("Job file not found: {JobId}", jobId);
                    return null;
                }

                var data = await File.ReadAllTextAsync(filePath);
                _logger?.LogDebug("Read job from local file: {JobId}, {Size} bytes", jobId, data.Length);
                return data;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error reading job file: {JobId}", jobId);
                return null;
            }
        }

        /// <summary>
        /// Write job data to local file
        /// </summary>
        public async Task<bool> WriteAsync(string jobId, string data)
        {
            try
            {
                var filePath = GetFilePath(jobId);
                await File.WriteAllTextAsync(filePath, data);
                _logger?.LogInformation("Wrote job to local file: {JobId}, {Size} bytes", jobId, data.Length);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error writing job file: {JobId}", jobId);
                return false;
            }
        }

        /// <summary>
        /// Delete job file and version file
        /// </summary>
        public async Task<bool> DeleteAsync(string jobId)
        {
            try
            {
                var filePath = GetFilePath(jobId);
                var versionPath = GetVersionFilePath(jobId);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                if (File.Exists(versionPath))
                {
                    File.Delete(versionPath);
                }

                _logger?.LogInformation("Deleted job files: {JobId}", jobId);
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting job files: {JobId}", jobId);
                return false;
            }
        }

        /// <summary>
        /// Get current version number
        /// </summary>
        public async Task<long> GetVersionAsync(string jobId)
        {
            try
            {
                var versionPath = GetVersionFilePath(jobId);
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
                _logger?.LogWarning(ex, "Error reading version file for job: {JobId}", jobId);
                return 0;
            }
        }

        /// <summary>
        /// Write version number
        /// </summary>
        public async Task<bool> WriteVersionAsync(string jobId, long version)
        {
            try
            {
                var versionPath = GetVersionFilePath(jobId);
                var versionObj = new VersionFile { Version = version };
                var versionText = JsonSerializer.Serialize(versionObj);
                await File.WriteAllTextAsync(versionPath, versionText);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error writing version file for job: {JobId}", jobId);
                return false;
            }
        }

        /// <summary>
        /// Increment and write version
        /// </summary>
        public async Task<long> IncrementAndWriteAsync(string jobId, string data)
        {
            var currentVersion = await GetVersionAsync(jobId);
            var newVersion = currentVersion + 1;

            await WriteAsync(jobId, data);
            await WriteVersionAsync(jobId, newVersion);

            return newVersion;
        }

        /// <summary>
        /// Get all job IDs in local storage
        /// </summary>
        public string[] GetAllJobIds()
        {
            try
            {
                var files = Directory.GetFiles(_baseDirectory, "*.json");
                var jobIds = new System.Collections.Generic.List<string>();

                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    // Skip version files
                    if (fileName.EndsWith(".json.version", StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Remove .json extension to get job ID
                    var jobId = Path.GetFileNameWithoutExtension(file);
                    jobIds.Add(jobId);
                }

                return jobIds.ToArray();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error listing job IDs");
                return Array.Empty<string>();
            }
        }

        private class VersionFile
        {
            public long Version { get; set; }
        }
    }
}
