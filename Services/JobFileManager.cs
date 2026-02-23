using HavenCNCServer.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Services
{
    /// <summary>
    /// Manages job file storage in local filesystem
    /// Pattern follows ConfigurationFileManager
    /// </summary>
    public class JobFileManager
    {
        private readonly string _baseDirectory;
        private const string JobsSubdirectory = "jobs";

        public JobFileManager()
        {
            _baseDirectory = Path.Combine(@"C:\havencncdata", JobsSubdirectory);

            // Ensure directory exists
            if (!Directory.Exists(_baseDirectory))
            {
                Directory.CreateDirectory(_baseDirectory);
                Log($"Created jobs directory: {_baseDirectory}", LogLevel.Info, "JobFileManager");
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
        /// Returns true if the version file exists for this job.
        /// Jobs without a version file are treated as version 0 but should be healed.
        /// </summary>
        public bool HasVersionFile(string jobId)
        {
            return File.Exists(GetVersionFilePath(jobId));
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
                    Log($"Job file not found: {jobId}", LogLevel.Debug, "JobFileManager");
                    return null;
                }

                var data = await File.ReadAllTextAsync(filePath);
                return data;
            }
            catch (Exception ex)
            {
                Log($"Error reading job file {jobId}: {ex.Message}", LogLevel.Error, "JobFileManager");
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
                Log($"Wrote job to local file: {jobId}, {data.Length} bytes", LogLevel.Info, "JobFileManager");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Error writing job file {jobId}: {ex.Message}", LogLevel.Error, "JobFileManager");
                return false;
            }
        }

        /// <summary>
        /// Get the tombstone file path for a job (marks it as pending deletion from MongoDB)
        /// </summary>
        public string GetTombstonePath(string jobId)
        {
            return Path.Combine(_baseDirectory, $"{jobId}.deleted");
        }

        /// <summary>
        /// Returns true if this job has a pending delete tombstone
        /// </summary>
        public bool IsTombstoned(string jobId)
        {
            return File.Exists(GetTombstonePath(jobId));
        }

        /// <summary>
        /// Returns all job IDs that have a pending tombstone (need deleting from MongoDB)
        /// </summary>
        public string[] GetPendingTombstones()
        {
            try
            {
                var files = Directory.GetFiles(_baseDirectory, "*.deleted");
                var ids = new System.Collections.Generic.List<string>();
                foreach (var f in files)
                    ids.Add(Path.GetFileNameWithoutExtension(f)); // strips .deleted
                return ids.ToArray();
            }
            catch (Exception ex)
            {
                Log($"Error listing tombstones: {ex.Message}", LogLevel.Error, "JobFileManager");
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Removes the tombstone file once MongoDB deletion has been confirmed
        /// </summary>
        public void RemoveTombstone(string jobId)
        {
            try
            {
                var path = GetTombstonePath(jobId);
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                Log($"Error removing tombstone for {jobId}: {ex.Message}", LogLevel.Warning, "JobFileManager");
            }
        }

        /// <summary>
        /// Delete job file and version file, leaving a tombstone so the sync can
        /// propagate the deletion to MongoDB when it next connects.
        /// </summary>
        public async Task<bool> DeleteAsync(string jobId)
        {
            try
            {
                var filePath = GetFilePath(jobId);
                var versionPath = GetVersionFilePath(jobId);
                var tombstonePath = GetTombstonePath(jobId);

                bool existed = File.Exists(filePath) || File.Exists(versionPath);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Log($"Deleted job file: {jobId}.json", LogLevel.Info, "JobFileManager");
                }

                if (File.Exists(versionPath))
                {
                    File.Delete(versionPath);
                    Log($"Deleted version file: {jobId}.json.version", LogLevel.Info, "JobFileManager");
                }

                // Write tombstone so the sync loop can delete from MongoDB later
                await File.WriteAllTextAsync(tombstonePath, DateTime.UtcNow.ToString("O"));
                Log($"Tombstone written for job: {jobId}", LogLevel.Info, "JobFileManager");

                if (!existed)
                    Log($"Job {jobId} was not found locally — tombstone still written to propagate delete to MongoDB", LogLevel.Warning, "JobFileManager");

                // Return true as long as we could write the tombstone (deletion intent recorded)
                return true;
            }
            catch (Exception ex)
            {
                Log($"Error deleting job files {jobId}: {ex.Message}", LogLevel.Error, "JobFileManager");
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
                Log($"Error reading version file for job {jobId}: {ex.Message}", LogLevel.Warning, "JobFileManager");
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
                Log($"Error writing version file for job {jobId}: {ex.Message}", LogLevel.Error, "JobFileManager");
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

                    // Skip tombstoned jobs
                    if (IsTombstoned(jobId))
                        continue;

                    jobIds.Add(jobId);
                }

                return jobIds.ToArray();
            }
            catch (Exception ex)
            {
                Log($"Error listing job IDs: {ex.Message}", LogLevel.Error, "JobFileManager");
                return Array.Empty<string>();
            }
        }

        private class VersionFile
        {
            public long Version { get; set; }
        }
    }
}
