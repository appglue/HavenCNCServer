using Microsoft.AspNetCore.Mvc;
using HavenCNCServer.Models;
using HavenCNCServer.Services;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Controllers
{
    /// <summary>
    /// Machine Configuration Management Controller
    /// Handles multi-machine configuration with MongoDB synchronization
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MachineConfigurationController : ControllerBase
    {
        private readonly MongoDbService _mongoService;
        private readonly string _dataDirectory;
        private readonly string _localSettingsPath;
        private readonly string _archiveDirectory;
        private readonly string _defaultPlcDirectory;
        private static readonly string[] ConfigFiles = new[]
        {
            "plcSystem.json",
            "plcSystemDefault.json",
            "configuration.json",
            "machine.json",
            "machineState.json",
            "fixtures.json",
            "userActionData.json"
        };

        public MachineConfigurationController()
        {
            var mongoSettings = SettingsManager.Settings.MongoDB;
            _mongoService = new MongoDbService(mongoSettings);

            _dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "data");
            _localSettingsPath = Path.Combine(_dataDirectory, "localMachineSettings.json");
            _archiveDirectory = Path.Combine(_dataDirectory, "machineArchives");
            _defaultPlcDirectory = Path.Combine(_dataDirectory, "defaultPlcVersions");

            Directory.CreateDirectory(_dataDirectory);
            Directory.CreateDirectory(_archiveDirectory);
            Directory.CreateDirectory(_defaultPlcDirectory);

            LogInfo("MachineConfigurationController initialized", "MachineConfig");

            // Perform initial sync check on startup
            _ = Task.Run(async () => await CheckAndPerformInitialSync());
        }

        /// <summary>
        /// Check if initial sync is needed and perform it
        /// </summary>
        private async Task CheckAndPerformInitialSync()
        {
            try
            {
                // Wait a bit for services to fully initialize
                await Task.Delay(2000);

                var localSettings = LoadLocalSettings();
                var machineName = localSettings.CurrentMachineName;

                // Skip if no machine name set
                if (string.IsNullOrEmpty(machineName))
                {
                    LogInfo("Machine name not set, skipping initial sync check", "MachineConfig");
                    return;
                }

                // Skip if sync disabled
                if (!localSettings.SyncEnabled)
                {
                    LogInfo("Sync disabled, skipping initial sync check", "MachineConfig");
                    return;
                }

                // Skip if MongoDB is offline
                if (!_mongoService.IsConnected)
                {
                    LogWarning("MongoDB offline, skipping initial sync check", "MachineConfig");
                    return;
                }

                LogInfo($"🔍 Checking if initial sync needed for machine '{machineName}'...", "MachineConfig");

                // Check if any configuration files exist in MongoDB for this machine
                var hasMongoFiles = false;
                foreach (var fileName in ConfigFiles)
                {
                    var mongoDoc = await _mongoService.GetMachineConfigurationAsync(machineName, fileName);
                    if (mongoDoc != null)
                    {
                        hasMongoFiles = true;
                        break;
                    }
                }

                if (!hasMongoFiles)
                {
                    // No files in MongoDB, check if we have local files to upload
                    var hasLocalFiles = false;
                    foreach (var fileName in ConfigFiles)
                    {
                        var localFilePath = Path.Combine(_dataDirectory, fileName);
                        if (System.IO.File.Exists(localFilePath))
                        {
                            hasLocalFiles = true;
                            break;
                        }
                    }

                    if (hasLocalFiles)
                    {
                        LogInfo($"📤 No MongoDB files found but local files exist. Performing initial sync...", "MachineConfig");
                        await SaveAllLocalFilesToMongoAsync(machineName);

                        localSettings.LastSyncTime = DateTime.UtcNow;
                        SaveLocalSettings(localSettings);

                        LogSuccess($"✓ Initial sync completed for machine '{machineName}'", "MachineConfig");
                    }
                    else
                    {
                        LogInfo("No local files found to sync", "MachineConfig");
                    }
                }
                else
                {
                    LogInfo("MongoDB files already exist, no initial sync needed", "MachineConfig");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error during initial sync check: {ex.Message}", "MachineConfig");
            }
        }

        #region Local Settings Management

        /// <summary>
        /// Load local machine settings from file
        /// </summary>
        private LocalMachineSettings LoadLocalSettings()
        {
            try
            {
                if (System.IO.File.Exists(_localSettingsPath))
                {
                    var json = System.IO.File.ReadAllText(_localSettingsPath);
                    return JsonSerializer.Deserialize<LocalMachineSettings>(json) ?? new LocalMachineSettings();
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to load local machine settings: {ex.Message}", "MachineConfig");
            }

            // Return default with empty machine name (not set yet)
            return new LocalMachineSettings
            {
                CurrentMachineName = string.Empty,
                SyncEnabled = true
            };
        }

        /// <summary>
        /// Save local machine settings to file
        /// </summary>
        private void SaveLocalSettings(LocalMachineSettings settings)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(_localSettingsPath, json);
                LogInfo($"Local machine settings saved: {settings.CurrentMachineName}", "MachineConfig");
            }
            catch (Exception ex)
            {
                LogError($"Failed to save local machine settings: {ex.Message}", "MachineConfig");
                throw;
            }
        }

        #endregion

        #region Machine Management

        /// <summary>
        /// Get all available machine names from MongoDB
        /// </summary>
        /// <returns>Array of machine names</returns>
        [HttpGet("GetMachineNames")]
        [ProducesResponseType(typeof(string[]), 200)]
        public async Task<ActionResult<string[]>> GetMachineNames()
        {
            try
            {
                LogInfo("GetMachineNames request", "MachineConfig");

                var machineNames = await _mongoService.GetAllMachineNamesAsync();

                // Add current local machine if not in list and machine name is set
                var localSettings = LoadLocalSettings();
                if (!string.IsNullOrEmpty(localSettings.CurrentMachineName) &&
                    !machineNames.Contains(localSettings.CurrentMachineName))
                {
                    machineNames.Add(localSettings.CurrentMachineName);
                }

                machineNames = machineNames.Distinct().OrderBy(x => x).ToList();

                LogSuccess($"✓ Retrieved {machineNames.Count} machine names", "MachineConfig");
                return Ok(machineNames.ToArray());
            }
            catch (Exception ex)
            {
                LogError($"Failed to get machine names: {ex.Message}", "MachineConfig");
                return StatusCode(500, new { message = $"Failed to get machine names: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get currently active machine name
        /// </summary>
        /// <returns>Current machine name</returns>
        [HttpGet("GetCurrentMachine")]
        [ProducesResponseType(typeof(string), 200)]
        public ActionResult<string> GetCurrentMachine()
        {
            try
            {
                var settings = LoadLocalSettings();
                LogInfo($"Current machine: {settings.CurrentMachineName}", "MachineConfig");
                return Ok(settings.CurrentMachineName);
            }
            catch (Exception ex)
            {
                LogError($"Failed to get current machine: {ex.Message}", "MachineConfig");
                return StatusCode(500, new { message = $"Failed to get current machine: {ex.Message}" });
            }
        }

        /// <summary>
        /// Set current machine (archives old local data and loads new machine data)
        /// </summary>
        /// <param name="request">Machine name to switch to</param>
        /// <returns>Success response</returns>
        [HttpPost("SetCurrentMachine")]
        [ProducesResponseType(200)]
        public async Task<ActionResult> SetCurrentMachine([FromBody] SetCurrentMachineRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.MachineName))
                {
                    return BadRequest(new { message = "Machine name is required" });
                }

                LogInfo($"🔄 SetCurrentMachine request: '{request.MachineName}'", "MachineConfig");

                var localSettings = LoadLocalSettings();
                var oldMachineName = localSettings.CurrentMachineName;
                var isFirstTimeSetup = string.IsNullOrEmpty(oldMachineName);

                // Step 1: Archive current local files to old machine name in MongoDB (only if machine name was already set)
                if (!string.IsNullOrEmpty(oldMachineName))
                {
                    LogInfo($"Step 1: Archiving current machine '{oldMachineName}' to MongoDB", "MachineConfig");
                    await SaveAllLocalFilesToMongoAsync(oldMachineName);

                    // Also create local archive
                    ArchiveLocalFiles(oldMachineName);
                }
                else
                {
                    LogInfo("Step 1: First time setup - no previous machine to archive", "MachineConfig");
                }

                // Step 2: Update local settings
                localSettings.CurrentMachineName = request.MachineName;
                localSettings.LastSyncTime = DateTime.UtcNow;
                SaveLocalSettings(localSettings);

                // Step 3: Load new machine data from MongoDB to local files (if available)
                LogInfo($"Step 2: Loading machine '{request.MachineName}' from MongoDB", "MachineConfig");
                var loadedFromMongo = await LoadAllFilesFromMongoAsync(request.MachineName);

                if (isFirstTimeSetup && loadedFromMongo)
                {
                    LogSuccess($"✓ First time setup: Loaded existing configuration for '{request.MachineName}' from MongoDB", "MachineConfig");
                }
                else if (isFirstTimeSetup && !loadedFromMongo)
                {
                    LogInfo($"First time setup: No existing configuration in MongoDB for '{request.MachineName}', uploading current local files", "MachineConfig");
                    // Upload current local files to this new machine name
                    await SaveAllLocalFilesToMongoAsync(request.MachineName);
                }
                else if (!isFirstTimeSetup && loadedFromMongo)
                {
                    LogSuccess($"✓ Switched to existing machine '{request.MachineName}' from MongoDB", "MachineConfig");
                }
                else if (!isFirstTimeSetup && !loadedFromMongo)
                {
                    // Switching machines: new machine doesn't exist in MongoDB
                    // Copy from old machine in MongoDB (not from local files)
                    LogInfo($"New machine '{request.MachineName}' not found in MongoDB, copying from '{oldMachineName}'", "MachineConfig");

                    var copySuccess = await _mongoService.CopyMachineConfigurationAsync(
                        oldMachineName,
                        request.MachineName,
                        $"Copied from {oldMachineName} on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}"
                    );

                    if (copySuccess)
                    {
                        LogSuccess($"✓ Copied configuration from '{oldMachineName}' to '{request.MachineName}' in MongoDB", "MachineConfig");
                        // Now load the copied files from MongoDB to local
                        await LoadAllFilesFromMongoAsync(request.MachineName);
                    }
                    else
                    {
                        LogWarning($"Failed to copy from '{oldMachineName}' in MongoDB (may be offline), keeping current local files", "MachineConfig");
                    }
                }

                LogSuccess($"✓ Switched to machine '{request.MachineName}'", "MachineConfig");
                return Ok(new { message = $"Successfully switched to machine '{request.MachineName}'" });
            }
            catch (Exception ex)
            {
                LogError($"Failed to set current machine: {ex.Message}", "MachineConfig");
                return StatusCode(500, new { message = $"Failed to set current machine: {ex.Message}" });
            }
        }

        /// <summary>
        /// Copy machine configuration to a new machine name
        /// </summary>
        /// <param name="request">Copy request with source and new machine names</param>
        /// <returns>Success response</returns>
        [HttpPost("CopyMachineConfiguration")]
        [ProducesResponseType(200)]
        public async Task<ActionResult> CopyMachineConfiguration([FromBody] CopyMachineConfigurationRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.SourceMachineName))
                {
                    return BadRequest(new { message = "Source machine name is required" });
                }

                if (string.IsNullOrWhiteSpace(request.NewMachineName))
                {
                    return BadRequest(new { message = "New machine name is required" });
                }

                LogInfo($"📋 CopyMachineConfiguration: '{request.SourceMachineName}' -> '{request.NewMachineName}'", "MachineConfig");

                var success = await _mongoService.CopyMachineConfigurationAsync(
                    request.SourceMachineName,
                    request.NewMachineName,
                    request.Description
                );

                if (success)
                {
                    LogSuccess($"✓ Copied machine configuration from '{request.SourceMachineName}' to '{request.NewMachineName}'", "MachineConfig");
                    return Ok(new { message = "Machine configuration copied successfully" });
                }
                else
                {
                    return StatusCode(500, new { message = "Failed to copy machine configuration" });
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to copy machine configuration: {ex.Message}", "MachineConfig");
                return StatusCode(500, new { message = $"Failed to copy machine configuration: {ex.Message}" });
            }
        }

        /// <summary>
        /// Sync local files to MongoDB (upload newer local files)
        /// </summary>
        /// <returns>Sync status with counts</returns>
        [HttpPost("SyncLocalToMongo")]
        [ProducesResponseType(200)]
        public async Task<ActionResult> SyncLocalToMongo()
        {
            try
            {
                var localSettings = LoadLocalSettings();
                var machineName = localSettings.CurrentMachineName;

                if (string.IsNullOrEmpty(machineName))
                {
                    return BadRequest(new { message = "Machine name not set. Please set machine name first." });
                }

                if (!_mongoService.IsConnected)
                {
                    return StatusCode(503, new { message = "MongoDB is offline. Cannot sync." });
                }

                LogInfo($"🔄 Syncing local files to MongoDB for machine '{machineName}'...", "MachineConfig");

                var uploadedCount = 0;
                var skippedCount = 0;
                var errorCount = 0;

                foreach (var fileName in ConfigFiles)
                {
                    var localFilePath = Path.Combine(_dataDirectory, fileName);

                    if (!System.IO.File.Exists(localFilePath))
                    {
                        skippedCount++;
                        continue;
                    }

                    try
                    {
                        var localContent = System.IO.File.ReadAllText(localFilePath);
                        var localTimestamp = System.IO.File.GetLastWriteTimeUtc(localFilePath);

                        // Check if MongoDB version exists and is older
                        var mongoDoc = await _mongoService.GetMachineConfigurationAsync(machineName, fileName);

                        if (mongoDoc == null)
                        {
                            // File doesn't exist in MongoDB, upload it
                            LogInfo($"  📤 {fileName} - new file, uploading...", "MachineConfig");
                            var success = await _mongoService.SaveMachineConfigurationAsync(machineName, fileName, localContent);
                            if (success)
                            {
                                uploadedCount++;
                                LogInfo($"    ✓ Uploaded", "MachineConfig");
                            }
                            else
                            {
                                errorCount++;
                                LogWarning($"    ✗ Failed to upload", "MachineConfig");
                            }
                        }
                        else if (localTimestamp > mongoDoc.Timestamp)
                        {
                            // Local file is newer, upload it
                            LogInfo($"  📤 {fileName} - local newer ({localTimestamp:yyyy-MM-dd HH:mm:ss} > {mongoDoc.Timestamp:yyyy-MM-dd HH:mm:ss})", "MachineConfig");
                            var success = await _mongoService.SaveMachineConfigurationAsync(machineName, fileName, localContent);
                            if (success)
                            {
                                uploadedCount++;
                                LogInfo($"    ✓ Uploaded", "MachineConfig");
                            }
                            else
                            {
                                errorCount++;
                                LogWarning($"    ✗ Failed to upload", "MachineConfig");
                            }
                        }
                        else
                        {
                            // MongoDB version is same or newer, skip
                            skippedCount++;
                            LogInfo($"  ⊘ {fileName} - MongoDB version is current", "MachineConfig");
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        LogError($"  ✗ Error syncing {fileName}: {ex.Message}", "MachineConfig");
                    }
                }

                if (uploadedCount > 0)
                {
                    localSettings.LastSyncTime = DateTime.UtcNow;
                    SaveLocalSettings(localSettings);
                }

                var message = $"Sync complete: {uploadedCount} uploaded, {skippedCount} skipped, {errorCount} errors";
                LogSuccess($"✓ {message}", "MachineConfig");

                return Ok(new
                {
                    message,
                    uploaded = uploadedCount,
                    skipped = skippedCount,
                    errors = errorCount,
                    lastSyncTime = localSettings.LastSyncTime
                });
            }
            catch (Exception ex)
            {
                LogError($"Failed to sync local files to MongoDB: {ex.Message}", "MachineConfig");
                return StatusCode(500, new { message = $"Failed to sync: {ex.Message}" });
            }
        }

        #endregion

        #region Configuration File Operations

        /// <summary>
        /// Get configuration file (checks local vs MongoDB timestamp and returns latest)
        /// </summary>
        /// <param name="fileName">Configuration file name</param>
        /// <returns>Configuration file content</returns>
        [HttpGet("GetConfiguration/{fileName}")]
        [ProducesResponseType(typeof(string), 200)]
        public async Task<ActionResult<string>> GetConfiguration(string fileName)
        {
            try
            {
                if (!ConfigFiles.Contains(fileName))
                {
                    return BadRequest(new { message = $"Invalid configuration file: {fileName}" });
                }

                LogInfo($"📖 GetConfiguration request: '{fileName}'", "MachineConfig");

                var localSettings = LoadLocalSettings();
                var machineName = localSettings.CurrentMachineName;

                // Get local file timestamp and content
                var localFilePath = Path.Combine(_dataDirectory, fileName);
                DateTime? localTimestamp = null;
                string? localContent = null;

                if (System.IO.File.Exists(localFilePath))
                {
                    localTimestamp = System.IO.File.GetLastWriteTimeUtc(localFilePath);
                    localContent = System.IO.File.ReadAllText(localFilePath);
                }

                // If machine name is not set, only return local file (no MongoDB sync)
                if (string.IsNullOrEmpty(machineName))
                {
                    if (localContent != null)
                    {
                        LogInfo($"Machine name not set - returning local file only: {fileName}", "MachineConfig");
                        return Ok(localContent);
                    }
                    else
                    {
                        LogWarning($"Machine name not set and configuration file not found: {fileName}", "MachineConfig");
                        return NotFound(new { message = $"Configuration file not found: {fileName}" });
                    }
                }

                // Check MongoDB status (only if machine name is set)
                MachineConfigurationDocument? mongoDoc = null;
                if (_mongoService.IsConnected)
                {
                    mongoDoc = await _mongoService.GetMachineConfigurationAsync(machineName, fileName);
                }
                else
                {
                    LogInfo("MongoDB offline - using local file only", "MachineConfig");
                }

                // Compare timestamps and return latest
                if (mongoDoc != null && localTimestamp != null)
                {
                    if (mongoDoc.Timestamp > localTimestamp)
                    {
                        // MongoDB is newer - update local file and return MongoDB content
                        System.IO.File.WriteAllText(localFilePath, mongoDoc.JsonData);
                        LogInfo($"MongoDB version is newer, updated local file: {fileName}", "MachineConfig");
                        return Ok(mongoDoc.JsonData);
                    }
                    else if (localTimestamp > mongoDoc.Timestamp)
                    {
                        // Local is newer - update MongoDB and return local content
                        await _mongoService.SaveMachineConfigurationAsync(machineName, fileName, localContent!);
                        LogInfo($"Local version is newer, updated MongoDB: {fileName}", "MachineConfig");
                        return Ok(localContent);
                    }
                    else
                    {
                        // Same timestamp - return local
                        return Ok(localContent);
                    }
                }
                else if (mongoDoc != null)
                {
                    // Only MongoDB has it - save locally and return
                    System.IO.File.WriteAllText(localFilePath, mongoDoc.JsonData);
                    LogInfo($"Loaded from MongoDB only: {fileName}", "MachineConfig");
                    return Ok(mongoDoc.JsonData);
                }
                else if (localContent != null)
                {
                    // Only local has it - try to save to MongoDB if online
                    if (_mongoService.IsConnected)
                    {
                        var syncSuccess = await _mongoService.SaveMachineConfigurationAsync(machineName, fileName, localContent);
                        if (syncSuccess)
                        {
                            LogInfo($"Synced local file to MongoDB: {fileName}", "MachineConfig");
                        }
                    }
                    return Ok(localContent);
                }
                else
                {
                    // Neither has it
                    LogWarning($"Configuration file not found: {fileName}", "MachineConfig");
                    return NotFound(new { message = $"Configuration file not found: {fileName}" });
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to get configuration: {ex.Message}", "MachineConfig");
                return StatusCode(500, new { message = $"Failed to get configuration: {ex.Message}" });
            }
        }

        /// <summary>
        /// Save configuration file (saves both locally and to MongoDB)
        /// </summary>
        /// <param name="fileName">Configuration file name</param>
        /// <param name="content">File content</param>
        /// <returns>Success response</returns>
        [HttpPost("SaveConfiguration/{fileName}")]
        [ProducesResponseType(200)]
        public async Task<ActionResult> SaveConfiguration(string fileName, [FromBody] string content)
        {
            try
            {
                if (!ConfigFiles.Contains(fileName))
                {
                    return BadRequest(new { message = $"Invalid configuration file: {fileName}" });
                }

                if (string.IsNullOrEmpty(content))
                {
                    return BadRequest(new { message = "Content is required" });
                }

                LogInfo($"💾 SaveConfiguration request: '{fileName}' ({content.Length} chars)", "MachineConfig");

                var localSettings = LoadLocalSettings();
                var machineName = localSettings.CurrentMachineName;

                // Save to local file
                var localFilePath = Path.Combine(_dataDirectory, fileName);

                // Create backup before saving
                BackupService.CreateBackup(localFilePath);

                System.IO.File.WriteAllText(localFilePath, content);
                LogInfo($"Saved locally: {fileName}", "MachineConfig");

                // Save to MongoDB only if machine name is set
                var mongoSuccess = false;
                if (!string.IsNullOrEmpty(machineName))
                {
                    mongoSuccess = await _mongoService.SaveMachineConfigurationAsync(machineName, fileName, content);

                    if (mongoSuccess)
                    {
                        localSettings.LastSyncTime = DateTime.UtcNow;
                        SaveLocalSettings(localSettings);
                    }
                }
                else
                {
                    LogInfo("Machine name not set - skipping MongoDB sync", "MachineConfig");
                }

                // If saving plcSystemDefault.json and no default PLC versions exist, store this as the first default
                if (fileName == "plcSystemDefault.json" && _mongoService.IsConnected)
                {
                    try
                    {
                        var existingVersions = await _mongoService.ListDefaultPlcVersionsAsync();
                        if (existingVersions.Count == 0)
                        {
                            LogInfo("No default PLC versions found - storing current plcSystemDefault.json as first default version", "MachineConfig");
                            var storeSuccess = await _mongoService.StoreDefaultPlcAsync(
                                versionName: "Initial Default",
                                jsonData: content,
                                description: "Automatically created from first plcSystemDefault.json save",
                                markAsLatest: true,
                                createdBy: "System"
                            );
                            if (storeSuccess)
                            {
                                LogSuccess("✓ Automatically stored first default PLC version", "MachineConfig");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogWarning($"Could not auto-store default PLC to MongoDB (offline): {ex.Message}", "MachineConfig");
                        // Continue - local file is already saved
                    }
                }

                LogSuccess($"✓ Saved configuration: {fileName}", "MachineConfig");
                return Ok(new { message = "Configuration saved successfully", syncedToMongoDB = mongoSuccess });
            }
            catch (Exception ex)
            {
                LogError($"Failed to save configuration: {ex.Message}", "MachineConfig");
                return StatusCode(500, new { message = $"Failed to save configuration: {ex.Message}" });
            }
        }

        #endregion

        #region Default PLC Management

        /// <summary>
        /// Store current system default PLC as a versioned default
        /// </summary>
        /// <param name="request">Store request with version name and data</param>
        /// <returns>Success response</returns>
        [HttpPost("StoreAsDefaultPLC")]
        [ProducesResponseType(200)]
        public async Task<ActionResult> StoreAsDefaultPLC([FromBody] StoreDefaultPlcRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.VersionName))
                {
                    return BadRequest(new { message = "Version name is required" });
                }

                if (string.IsNullOrEmpty(request.JsonData))
                {
                    return BadRequest(new { message = "JSON data is required" });
                }

                LogInfo($"💾 StoreAsDefaultPLC request: '{request.VersionName}'", "MachineConfig");

                // Check if this is the first default PLC version (check both MongoDB and local)
                var existingVersions = await _mongoService.ListDefaultPlcVersionsAsync();
                var localVersions = GetLocalDefaultPlcVersions();
                var isFirstVersion = existingVersions.Count == 0 && localVersions.Count == 0;

                // If this is the first version, always mark it as latest regardless of request
                var markAsLatest = isFirstVersion || request.MarkAsLatest;

                if (isFirstVersion)
                {
                    LogInfo("First default PLC version - automatically marking as latest", "MachineConfig");
                }

                // Save to local file system first
                var localSuccess = SaveDefaultPlcLocally(request.VersionName, request.JsonData, request.Description, markAsLatest, request.CreatedBy);

                // Try to save to MongoDB if connected
                var mongoSuccess = false;
                if (_mongoService.IsConnected)
                {
                    mongoSuccess = await _mongoService.StoreDefaultPlcAsync(
                        request.VersionName,
                        request.JsonData,
                        request.Description,
                        markAsLatest,
                        request.CreatedBy
                    );
                }
                else
                {
                    LogInfo("MongoDB offline - default PLC stored locally only", "MachineConfig");
                }

                if (localSuccess)
                {
                    LogSuccess($"✓ Stored default PLC version: '{request.VersionName}' (MongoDB: {mongoSuccess})", "MachineConfig");
                    return Ok(new { message = "Default PLC version stored successfully", syncedToMongoDB = mongoSuccess });
                }
                else
                {
                    return StatusCode(500, new { message = "Failed to store default PLC version" });
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to store default PLC: {ex.Message}", "MachineConfig");
                return StatusCode(500, new { message = $"Failed to store default PLC: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get default PLC (latest version or specific version)
        /// </summary>
        /// <param name="versionName">Optional version name (if null, returns latest)</param>
        /// <returns>Default PLC data</returns>
        [HttpGet("GetDefaultPLC")]
        [ProducesResponseType(typeof(string), 200)]
        public async Task<ActionResult<string>> GetDefaultPLC([FromQuery] string? versionName = null)
        {
            try
            {
                LogInfo($"📖 GetDefaultPLC request: version='{versionName ?? "latest"}'", "MachineConfig");

                DefaultPlcVersionDocument? result = null;

                // Try MongoDB first if connected
                if (_mongoService.IsConnected)
                {
                    if (string.IsNullOrWhiteSpace(versionName))
                    {
                        result = await _mongoService.GetLatestDefaultPlcAsync();
                    }
                    else
                    {
                        result = await _mongoService.GetDefaultPlcByVersionAsync(versionName);
                    }
                }

                // Fallback to local storage if MongoDB didn't return a result
                if (result == null)
                {
                    if (_mongoService.IsConnected)
                    {
                        LogInfo("Default PLC not found in MongoDB, checking local storage", "MachineConfig");
                    }
                    else
                    {
                        LogInfo("MongoDB offline - using local default PLC storage", "MachineConfig");
                    }

                    result = GetDefaultPlcLocally(versionName);
                }

                if (result != null)
                {
                    LogSuccess($"✓ Retrieved default PLC version: '{result.VersionName}'", "MachineConfig");
                    return Ok(result.JsonData);
                }
                else
                {
                    LogWarning("Default PLC not found", "MachineConfig");
                    return NotFound(new { message = "Default PLC not found" });
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to get default PLC: {ex.Message}", "MachineConfig");
                return StatusCode(500, new { message = $"Failed to get default PLC: {ex.Message}" });
            }
        }

        /// <summary>
        /// List all default PLC versions
        /// </summary>
        /// <returns>Array of default PLC version information</returns>
        [HttpGet("ListDefaultPLCVersions")]
        [ProducesResponseType(typeof(DefaultPlcVersionInfo[]), 200)]
        public async Task<ActionResult<DefaultPlcVersionInfo[]>> ListDefaultPLCVersions()
        {
            try
            {
                LogInfo("📋 ListDefaultPLCVersions request", "MachineConfig");

                var versions = new List<DefaultPlcVersionInfo>();

                // Get from MongoDB if connected
                if (_mongoService.IsConnected)
                {
                    versions = await _mongoService.ListDefaultPlcVersionsAsync();
                }

                // Merge with local versions (avoid duplicates)
                var localVersions = GetLocalDefaultPlcVersions();
                foreach (var localVersion in localVersions)
                {
                    if (!versions.Any(v => v.VersionName == localVersion.VersionName))
                    {
                        versions.Add(localVersion);
                    }
                }

                // Sort by timestamp descending
                versions = versions.OrderByDescending(v => v.Timestamp).ToList();

                LogSuccess($"✓ Retrieved {versions.Count} default PLC versions", "MachineConfig");
                return Ok(versions.ToArray());
            }
            catch (Exception ex)
            {
                LogError($"Failed to list default PLC versions: {ex.Message}", "MachineConfig");
                return StatusCode(500, new { message = $"Failed to list default PLC versions: {ex.Message}" });
            }
        }

        #endregion

        #region Sync Status

        /// <summary>
        /// Get current sync status
        /// </summary>
        /// <returns>Sync status information</returns>
        [HttpGet("GetSyncStatus")]
        [ProducesResponseType(typeof(SyncStatusResponse), 200)]
        public ActionResult<SyncStatusResponse> GetSyncStatus()
        {
            try
            {
                var localSettings = LoadLocalSettings();

                var response = new SyncStatusResponse
                {
                    MongoDbOnline = _mongoService.IsConnected,
                    CurrentMachineName = localSettings.CurrentMachineName,
                    LastSyncTime = localSettings.LastSyncTime,
                    PendingChanges = 0, // Could be enhanced to track pending changes
                    Message = _mongoService.IsConnected
                        ? "Connected to MongoDB"
                        : "MongoDB offline - working with local files only"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                LogError($"Failed to get sync status: {ex.Message}", "MachineConfig");
                return StatusCode(500, new { message = $"Failed to get sync status: {ex.Message}" });
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Save all local configuration files to MongoDB for a specific machine
        /// </summary>
        private async Task SaveAllLocalFilesToMongoAsync(string machineName)
        {
            LogInfo($"Uploading all local config files to MongoDB for machine '{machineName}'...", "MachineConfig");
            var successCount = 0;
            var failCount = 0;

            foreach (var fileName in ConfigFiles)
            {
                var filePath = Path.Combine(_dataDirectory, fileName);
                if (System.IO.File.Exists(filePath))
                {
                    try
                    {
                        var content = System.IO.File.ReadAllText(filePath);
                        var success = await _mongoService.SaveMachineConfigurationAsync(machineName, fileName, content);

                        if (success)
                        {
                            successCount++;
                            LogInfo($"  ✓ Uploaded {fileName}", "MachineConfig");
                        }
                        else
                        {
                            failCount++;
                            LogWarning($"  ✗ Failed to upload {fileName} (MongoDB offline or error)", "MachineConfig");
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        LogError($"  ✗ Error uploading {fileName}: {ex.Message}", "MachineConfig");
                    }
                }
                else
                {
                    LogInfo($"  ⊘ Skipped {fileName} (not found locally)", "MachineConfig");
                }
            }

            if (successCount > 0)
            {
                LogSuccess($"✓ Uploaded {successCount} files to MongoDB for '{machineName}'", "MachineConfig");
            }

            if (failCount > 0)
            {
                LogWarning($"⚠ {failCount} files failed to upload to MongoDB", "MachineConfig");
            }

            if (successCount == 0 && failCount == 0)
            {
                LogWarning("No files were uploaded to MongoDB", "MachineConfig");
            }
        }

        /// <summary>
        /// Load all configuration files from MongoDB for a specific machine
        /// </summary>
        /// <returns>True if any files were loaded from MongoDB, false otherwise</returns>
        private async Task<bool> LoadAllFilesFromMongoAsync(string machineName)
        {
            var loadedCount = 0;
            foreach (var fileName in ConfigFiles)
            {
                var mongoDoc = await _mongoService.GetMachineConfigurationAsync(machineName, fileName);
                if (mongoDoc != null)
                {
                    var filePath = Path.Combine(_dataDirectory, fileName);
                    System.IO.File.WriteAllText(filePath, mongoDoc.JsonData);
                    LogInfo($"Loaded {fileName} from MongoDB", "MachineConfig");
                    loadedCount++;
                }
            }
            return loadedCount > 0;
        }

        /// <summary>
        /// Archive current local files before switching machines
        /// </summary>
        private void ArchiveLocalFiles(string machineName)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var archivePath = Path.Combine(_archiveDirectory, $"{machineName}_{timestamp}");
                Directory.CreateDirectory(archivePath);

                foreach (var fileName in ConfigFiles)
                {
                    var sourcePath = Path.Combine(_dataDirectory, fileName);
                    if (System.IO.File.Exists(sourcePath))
                    {
                        var destPath = Path.Combine(archivePath, fileName);
                        System.IO.File.Copy(sourcePath, destPath, true);
                    }
                }

                LogInfo($"Archived local files for machine '{machineName}' to {archivePath}", "MachineConfig");
            }
            catch (Exception ex)
            {
                LogError($"Failed to archive local files: {ex.Message}", "MachineConfig");
            }
        }

        /// <summary>
        /// Save default PLC version to local file system
        /// </summary>
        private bool SaveDefaultPlcLocally(string versionName, string jsonData, string? description, bool markAsLatest, string? createdBy)
        {
            try
            {
                var document = new DefaultPlcVersionDocument
                {
                    Id = Guid.NewGuid().ToString(),
                    VersionName = versionName,
                    Timestamp = DateTime.UtcNow,
                    JsonData = jsonData,
                    Description = description,
                    IsLatest = true, // Always latest since there's only one
                    CreatedBy = createdBy
                };

                // Always save to single fixed file - overwrites previous version
                var filePath = Path.Combine(_defaultPlcDirectory, "systemDefault.json");
                var json = JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(filePath, json);

                LogInfo($"Saved default PLC version '{versionName}' locally", "MachineConfig");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to save default PLC locally: {ex.Message}", "MachineConfig");
                return false;
            }
        }

        /// <summary>
        /// Get default PLC version from local file system
        /// </summary>
        private DefaultPlcVersionDocument? GetDefaultPlcLocally(string? versionName)
        {
            try
            {
                var filePath = Path.Combine(_defaultPlcDirectory, "systemDefault.json");

                if (!System.IO.File.Exists(filePath))
                {
                    return null;
                }

                var json = System.IO.File.ReadAllText(filePath);
                var doc = JsonSerializer.Deserialize<DefaultPlcVersionDocument>(json);

                // If version name specified, verify it matches
                if (!string.IsNullOrWhiteSpace(versionName) && doc != null)
                {
                    if (doc.VersionName != versionName)
                    {
                        return null; // Requested version doesn't match stored version
                    }
                }

                return doc;
            }
            catch (Exception ex)
            {
                LogError($"Failed to get default PLC locally: {ex.Message}", "MachineConfig");
                return null;
            }
        }

        /// <summary>
        /// Get all local default PLC versions
        /// </summary>
        private List<DefaultPlcVersionInfo> GetLocalDefaultPlcVersions()
        {
            try
            {
                var versions = new List<DefaultPlcVersionInfo>();
                var filePath = Path.Combine(_defaultPlcDirectory, "systemDefault.json");

                if (System.IO.File.Exists(filePath))
                {
                    var json = System.IO.File.ReadAllText(filePath);
                    var doc = JsonSerializer.Deserialize<DefaultPlcVersionDocument>(json);
                    if (doc != null)
                    {
                        versions.Add(new DefaultPlcVersionInfo
                        {
                            Id = doc.Id!,
                            VersionName = doc.VersionName,
                            Timestamp = doc.Timestamp,
                            Description = doc.Description,
                            IsLatest = doc.IsLatest,
                            CreatedBy = doc.CreatedBy
                        });
                    }
                }

                return versions;
            }
            catch (Exception ex)
            {
                LogError($"Failed to list local default PLC versions: {ex.Message}", "MachineConfig");
                return new List<DefaultPlcVersionInfo>();
            }
        }

        #endregion
    }
}
