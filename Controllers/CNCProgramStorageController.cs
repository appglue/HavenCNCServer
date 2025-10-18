using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using HavenCNCServer.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using IOFile = System.IO.File;

namespace HavenCNCServer.Controllers
{
    /// <summary>
    /// Storage index entry for quick lookups
    /// </summary>
    public class StorageIndexEntry
    {
        /// <summary>
        /// Storage name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last updated timestamp
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Type of storage (Flow or MDI)
        /// </summary>
        public StorageType StorageType { get; set; }

        /// <summary>
        /// Whether this storage is exposed as an action
        /// </summary>
        public bool ExposeAsAction { get; set; }

        /// <summary>
        /// Action name if exposed as action
        /// </summary>
        public string ActionName { get; set; } = string.Empty;
    }
    /// <summary>
    /// CNC Program Storage - Handles storage, retrieval, and management of CNC programs and MDI commands
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CNCProgramStorageController : ControllerBase
    {
        private static readonly string StorageDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HavenCNCServer",
            "ProgramStorage"
        );

        private static readonly string IndexFilePath = Path.Combine(StorageDirectory, "_index.json");

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Constructor for CNC Program Storage Controller
        /// </summary>
        public CNCProgramStorageController()
        {
            // Ensure storage directory exists
            if (!Directory.Exists(StorageDirectory))
            {
                Directory.CreateDirectory(StorageDirectory);
            }

            // Initialize index if it doesn't exist
            if (!IOFile.Exists(IndexFilePath))
            {
                SaveIndex(new List<StorageIndexEntry>());
            }
        }

        /// <summary>
        /// Load the storage index
        /// </summary>
        private List<StorageIndexEntry> LoadIndex()
        {
            try
            {
                if (!IOFile.Exists(IndexFilePath))
                    return new List<StorageIndexEntry>();

                var json = IOFile.ReadAllText(IndexFilePath);
                return JsonSerializer.Deserialize<List<StorageIndexEntry>>(json, JsonOptions) ?? new List<StorageIndexEntry>();
            }
            catch
            {
                return new List<StorageIndexEntry>();
            }
        }

        /// <summary>
        /// Save the storage index
        /// </summary>
        private void SaveIndex(List<StorageIndexEntry> index)
        {
            var json = JsonSerializer.Serialize(index, JsonOptions);
            IOFile.WriteAllText(IndexFilePath, json);
        }

        /// <summary>
        /// Update index entry for a storage item
        /// </summary>
        private void UpdateIndexEntry(ProgramStorage storage)
        {
            var index = LoadIndex();
            var existing = index.FirstOrDefault(e => e.Name == storage.Name);

            if (existing != null)
            {
                existing.UpdatedAt = storage.UpdatedAt;
                existing.StorageType = storage.StorageType;
                existing.ExposeAsAction = storage.ExposeAsAction;
                existing.ActionName = storage.ActionName;
            }
            else
            {
                index.Add(new StorageIndexEntry
                {
                    Name = storage.Name,
                    CreatedAt = storage.CreatedAt,
                    UpdatedAt = storage.UpdatedAt,
                    StorageType = storage.StorageType,
                    ExposeAsAction = storage.ExposeAsAction,
                    ActionName = storage.ActionName
                });
            }

            SaveIndex(index);
        }

        /// <summary>
        /// Remove index entry for a storage item
        /// </summary>
        private void RemoveIndexEntry(string name)
        {
            var index = LoadIndex();
            index.RemoveAll(e => e.Name == name);
            SaveIndex(index);
        }

        /// <summary>
        /// Get the file path for a storage item
        /// </summary>
        private static string GetStorageFilePath(string name)
        {
            var safeFileName = string.Concat(name.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(StorageDirectory, $"{safeFileName}.json");
        }

        /// <summary>
        /// Get all storages (internal helper)
        /// </summary>
        private List<ProgramStorage> GetAllStorages()
        {
            var storages = new List<ProgramStorage>();
            var files = Directory.GetFiles(StorageDirectory, "*.json");

            foreach (var file in files)
            {
                try
                {
                    var json = IOFile.ReadAllText(file);
                    var storage = JsonSerializer.Deserialize<ProgramStorage>(json, JsonOptions);

                    if (storage != null)
                        storages.Add(storage);
                }
                catch
                {
                    // Skip invalid files
                    continue;
                }
            }

            return storages;
        }

        /// <summary>
        /// Get all MDI storage names
        /// </summary>
        [HttpGet("MDI/Names")]
        public List<string> GetMDIStorageNames()
        {
            var index = LoadIndex();
            return index
                .Where(e => e.StorageType == StorageType.MDI)
                .Select(e => e.Name)
                .OrderBy(n => n)
                .ToList();
        }

        /// <summary>
        /// Get all Flow storage names
        /// </summary>
        [HttpGet("Flow/Names")]
        public List<string> GetFlowStorageNames()
        {
            var index = LoadIndex();
            return index
                .Where(e => e.StorageType == StorageType.Flow)
                .Select(e => e.Name)
                .OrderBy(n => n)
                .ToList();
        }

        /// <summary>
        /// Get all storage names (MDI and Flow)
        /// </summary>
        [HttpGet("All/Names")]
        public List<string> GetAllStorageNames()
        {
            var index = LoadIndex();
            return index
                .Select(e => e.Name)
                .OrderBy(n => n)
                .ToList();
        }

        /// <summary>
        /// Get storage data by name
        /// </summary>
        /// <param name="name">Storage name</param>
        [HttpGet("{name}")]
        public ProgramStorage? GetStorageData(string name)
        {
            var filePath = GetStorageFilePath(name);

            if (!IOFile.Exists(filePath))
                return null;

            try
            {
                var json = IOFile.ReadAllText(filePath);
                return JsonSerializer.Deserialize<ProgramStorage>(json, JsonOptions);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to read storage '{name}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Save storage data (create or update)
        /// </summary>
        /// <param name="storage">Storage data to save</param>
        [HttpPost("Save")]
        public void SaveStorageData([FromBody][Required] ProgramStorage storage)
        {
            if (string.IsNullOrWhiteSpace(storage.Name))
                throw new ArgumentException("Storage name is required", nameof(storage));

            storage.UpdatedAt = DateTime.Now;

            var filePath = GetStorageFilePath(storage.Name);

            try
            {
                var json = JsonSerializer.Serialize(storage, JsonOptions);
                IOFile.WriteAllText(filePath, json);

                // Update index
                UpdateIndexEntry(storage);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save storage '{storage.Name}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Delete storage by name
        /// </summary>
        /// <param name="name">Storage name to delete</param>
        [HttpDelete("{name}")]
        public void DeleteStorageData(string name)
        {
            var filePath = GetStorageFilePath(name);

            if (IOFile.Exists(filePath))
            {
                try
                {
                    IOFile.Delete(filePath);

                    // Update index
                    RemoveIndexEntry(name);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to delete storage '{name}': {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Get all storages exposed as actions
        /// </summary>
        [HttpGet("Actions")]
        public List<ProgramStorage> GetActionStorages()
        {
            var index = LoadIndex();
            var actionEntries = index
                .Where(e => e.ExposeAsAction)
                .OrderBy(e => e.ActionName)
                .ToList();

            var result = new List<ProgramStorage>();
            foreach (var entry in actionEntries)
            {
                var storage = GetStorageData(entry.Name);
                if (storage != null)
                    result.Add(storage);
            }

            return result;
        }

        /// <summary>
        /// Get all action storage names
        /// </summary>
        [HttpGet("Actions/Names")]
        public List<string> GetActionStorageNames()
        {
            var index = LoadIndex();
            return index
                .Where(e => e.ExposeAsAction)
                .Select(e => e.ActionName)
                .OrderBy(n => n)
                .ToList();
        }

    }
}
