using HavenCNCServer.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Services
{
    /// <summary>
    /// Manages G-code file storage
    /// Managed files: stored by ID in C:\havencncdata\gcode\{id}.nc
    /// External files: scanned from user-specified directories (read-only)
    /// </summary>
    public class GCodeFileManager
    {
        private readonly string _managedDirectory;
        private const string GCodeSubdirectory = "gcode";

        public GCodeFileManager()
        {
            _managedDirectory = Path.Combine(@"C:\havencncdata", GCodeSubdirectory);

            // Ensure directory exists
            if (!Directory.Exists(_managedDirectory))
            {
                Directory.CreateDirectory(_managedDirectory);
                Log($"Created gcode directory: {_managedDirectory}", LogLevel.Info, "GCodeFileManager");
            }
        }

        /// <summary>
        /// Get the file path for a managed G-code file
        /// </summary>
        public string GetManagedFilePath(string fileId)
        {
            return Path.Combine(_managedDirectory, $"{fileId}.nc");
        }

        /// <summary>
        /// Get the version file path for a managed G-code file
        /// </summary>
        public string GetVersionFilePath(string fileId)
        {
            return Path.Combine(_managedDirectory, $"{fileId}.nc.version.json");
        }

        /// <summary>
        /// Check if managed file exists
        /// </summary>
        public bool ManagedExists(string fileId)
        {
            return File.Exists(GetManagedFilePath(fileId));
        }

        /// <summary>
        /// Read G-code data from managed file
        /// </summary>
        public async Task<string?> ReadManagedAsync(string fileId)
        {
            try
            {
                var filePath = GetManagedFilePath(fileId);
                if (!File.Exists(filePath))
                {
                    Log($"Managed G-code file not found: {fileId}", LogLevel.Debug, "GCodeFileManager");
                    return null;
                }

                var data = await File.ReadAllTextAsync(filePath);
                Log($"Read managed G-code file: {fileId}, {data.Length} bytes", LogLevel.Debug, "GCodeFileManager");
                return data;
            }
            catch (Exception ex)
            {
                Log($"Error reading managed G-code file {fileId}: {ex.Message}", LogLevel.Error, "GCodeFileManager");
                return null;
            }
        }

        /// <summary>
        /// Write G-code data to managed file
        /// </summary>
        public async Task<bool> WriteManagedAsync(string fileId, string data)
        {
            try
            {
                var filePath = GetManagedFilePath(fileId);
                await File.WriteAllTextAsync(filePath, data);
                Log($"Wrote managed G-code file: {fileId}, {data.Length} bytes", LogLevel.Info, "GCodeFileManager");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Error writing managed G-code file {fileId}: {ex.Message}", LogLevel.Error, "GCodeFileManager");
                return false;
            }
        }

        /// <summary>
        /// Delete managed G-code file and version file
        /// </summary>
        public async Task<bool> DeleteManagedAsync(string fileId)
        {
            try
            {
                var filePath = GetManagedFilePath(fileId);
                var versionPath = GetVersionFilePath(fileId);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                if (File.Exists(versionPath))
                {
                    File.Delete(versionPath);
                }

                Log($"Deleted managed G-code files: {fileId}", LogLevel.Info, "GCodeFileManager");
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Log($"Error deleting managed G-code files {fileId}: {ex.Message}", LogLevel.Error, "GCodeFileManager");
                return false;
            }
        }

        /// <summary>
        /// Get current version number
        /// </summary>
        public async Task<long> GetVersionAsync(string fileId)
        {
            try
            {
                var versionPath = GetVersionFilePath(fileId);
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
                Log($"Error reading version file for G-code {fileId}: {ex.Message}", LogLevel.Warning, "GCodeFileManager");
                return 0;
            }
        }

        /// <summary>
        /// Write version number
        /// </summary>
        public async Task<bool> WriteVersionAsync(string fileId, long version)
        {
            try
            {
                var versionPath = GetVersionFilePath(fileId);
                var versionObj = new VersionFile { Version = version };
                var versionText = JsonSerializer.Serialize(versionObj);
                await File.WriteAllTextAsync(versionPath, versionText);
                return true;
            }
            catch (Exception ex)
            {
                Log($"Error writing version file for G-code {fileId}: {ex.Message}", LogLevel.Error, "GCodeFileManager");
                return false;
            }
        }

        /// <summary>
        /// Increment and write version
        /// </summary>
        public async Task<long> IncrementAndWriteAsync(string fileId, string data)
        {
            var currentVersion = await GetVersionAsync(fileId);
            var newVersion = currentVersion + 1;

            await WriteManagedAsync(fileId, data);
            await WriteVersionAsync(fileId, newVersion);

            return newVersion;
        }

        /// <summary>
        /// Scan external directory for G-code files
        /// Returns metadata for list operations
        /// </summary>
        public GCodeFileMetadata[] ScanExternalDirectory(string directory, string[]? fileExtensions = null)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    Log($"External directory not found: {directory}", LogLevel.Warning, "GCodeFileManager");
                    return Array.Empty<GCodeFileMetadata>();
                }

                // Use provided extensions or defaults
                var extensions = fileExtensions ?? new[] { ".nc", ".txt", ".tap" };

                var files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories)
                    .Where(f => extensions.Any(ext => ext.Equals(Path.GetExtension(f), StringComparison.OrdinalIgnoreCase)))
                    .ToArray();

                var metadata = new List<GCodeFileMetadata>();
                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        var actualDirectory = fileInfo.DirectoryName ?? directory;

                        metadata.Add(new GCodeFileMetadata
                        {
                            FileId = null,  // External files don't have IDs
                            Name = fileInfo.Name,
                            Directory = actualDirectory,  // Use actual directory path, not root
                            LastModified = fileInfo.LastWriteTime,
                            Size = fileInfo.Length,
                            IsManaged = false
                        });
                    }
                    catch (Exception ex)
                    {
                        Log($"Error reading file info {file}: {ex.Message}", LogLevel.Warning, "GCodeFileManager");
                    }
                }

                return metadata.ToArray();
            }
            catch (Exception ex)
            {
                Log($"Error scanning external directory {directory}: {ex.Message}", LogLevel.Error, "GCodeFileManager");
                return Array.Empty<GCodeFileMetadata>();
            }
        }

        /// <summary>
        /// Get metadata for all managed G-code files
        /// </summary>
        public GCodeFileMetadata[] GetManagedFileMetadata()
        {
            try
            {
                var files = Directory.GetFiles(_managedDirectory, "*.nc");
                var metadata = new List<GCodeFileMetadata>();

                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        var fileName = Path.GetFileNameWithoutExtension(file);

                        metadata.Add(new GCodeFileMetadata
                        {
                            FileId = fileName,  // Managed files use ID as filename
                            Name = fileInfo.Name,
                            Directory = "managed",
                            LastModified = fileInfo.LastWriteTime,
                            Size = fileInfo.Length,
                            IsManaged = true
                        });
                    }
                    catch (Exception ex)
                    {
                        Log($"Error reading managed file info {file}: {ex.Message}", LogLevel.Warning, "GCodeFileManager");
                    }
                }

                return metadata.ToArray();
            }
            catch (Exception ex)
            {
                Log($"Error listing managed G-code files: {ex.Message}", LogLevel.Error, "GCodeFileManager");
                return Array.Empty<GCodeFileMetadata>();
            }
        }

        /// <summary>
        /// Read G-code file from external directory
        /// </summary>
        public async Task<string?> ReadExternalAsync(string directory, string fileName)
        {
            try
            {
                var filePath = Path.Combine(directory, fileName);
                if (!File.Exists(filePath))
                {
                    return null;
                }

                var data = await File.ReadAllTextAsync(filePath);
                return data;
            }
            catch (Exception ex)
            {
                Log($"Error reading external G-code file {directory}/{fileName}: {ex.Message}", LogLevel.Error, "GCodeFileManager");
                return null;
            }
        }

        private class VersionFile
        {
            public long Version { get; set; }
        }
    }
}
