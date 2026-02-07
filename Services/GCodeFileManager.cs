using HavenCNCServer.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace HavenCNCServer.Services
{
    /// <summary>
    /// Manages G-code file storage
    /// Managed files: stored by ID in C:\havencncdata\gcode\{id}.nc
    /// External files: scanned from user-specified directories (read-only)
    /// </summary>
    public class GCodeFileManager
    {
        private readonly ILogger<GCodeFileManager>? _logger;
        private readonly string _managedDirectory;
        private const string GCodeSubdirectory = "gcode";

        public GCodeFileManager(ILogger<GCodeFileManager>? logger)
        {
            _logger = logger;
            _managedDirectory = Path.Combine(@"C:\havencncdata", GCodeSubdirectory);

            // Ensure directory exists
            if (!Directory.Exists(_managedDirectory))
            {
                Directory.CreateDirectory(_managedDirectory);
                _logger?.LogInformation("Created gcode directory: {Directory}", _managedDirectory);
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
                    _logger?.LogDebug("Managed G-code file not found: {FileId}", fileId);
                    return null;
                }

                var data = await File.ReadAllTextAsync(filePath);
                _logger?.LogDebug("Read managed G-code file: {FileId}, {Size} bytes", fileId, data.Length);
                return data;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error reading managed G-code file: {FileId}", fileId);
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
                _logger?.LogInformation("Wrote managed G-code file: {FileId}, {Size} bytes", fileId, data.Length);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error writing managed G-code file: {FileId}", fileId);
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

                _logger?.LogInformation("Deleted managed G-code files: {FileId}", fileId);
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting managed G-code files: {FileId}", fileId);
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
                _logger?.LogWarning(ex, "Error reading version file for G-code: {FileId}", fileId);
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
                _logger?.LogError(ex, "Error writing version file for G-code: {FileId}", fileId);
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
        /// Scan external directory for G-code files (.nc, .gcode)
        /// Returns metadata for list operations
        /// </summary>
        public GCodeFileMetadata[] ScanExternalDirectory(string directory)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    _logger?.LogWarning("External directory not found: {Directory}", directory);
                    return Array.Empty<GCodeFileMetadata>();
                }

                var extensions = new[] { ".nc", ".gcode", ".NGC", ".GCODE" };
                var files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories)
                    .Where(f => extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                    .ToArray();

                var metadata = new List<GCodeFileMetadata>();
                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        metadata.Add(new GCodeFileMetadata
                        {
                            FileId = null,  // External files don't have IDs
                            Name = fileInfo.Name,
                            Directory = directory,
                            LastModified = fileInfo.LastWriteTime,
                            Size = fileInfo.Length,
                            IsManaged = false
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Error reading file info: {File}", file);
                    }
                }

                return metadata.ToArray();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error scanning external directory: {Directory}", directory);
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
                        _logger?.LogWarning(ex, "Error reading managed file info: {File}", file);
                    }
                }

                return metadata.ToArray();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error listing managed G-code files");
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
                    _logger?.LogDebug("External G-code file not found: {Path}", filePath);
                    return null;
                }

                var data = await File.ReadAllTextAsync(filePath);
                _logger?.LogDebug("Read external G-code file: {Path}, {Size} bytes", filePath, data.Length);
                return data;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error reading external G-code file: {Directory}/{FileName}", directory, fileName);
                return null;
            }
        }

        private class VersionFile
        {
            public long Version { get; set; }
        }
    }
}
