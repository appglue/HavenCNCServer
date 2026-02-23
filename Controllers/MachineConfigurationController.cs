using Microsoft.AspNetCore.Mvc;
using HavenCNCServer.Services;
using HavenCNCServer.Models;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MachineConfigurationController : ControllerBase
    {
        private static readonly string DataDirectory = @"C:\havencncdata";
        private static ConfigurationFileManager? _fileManager;
        private static MongoDbService? _mongoService;

        public static async Task InitializeAsync()
        {
            LogInfo("🔄 MachineConfigurationController.InitializeAsync CALLED", "MachineConfig");

            try
            {
                LogInfo("Creating ConfigurationFileManager...", "MachineConfig");
                _fileManager = new ConfigurationFileManager(DataDirectory);
                LogInfo("✓ ConfigurationFileManager created", "MachineConfig");

                // Load MongoDB settings using SettingsManager
                var mongoSettings = SettingsManager.Settings.MongoDB;
                if (mongoSettings != null)
                {
                    LogInfo($"MongoDB settings loaded: Enabled={mongoSettings.Enabled}", "MachineConfig");
                    _mongoService = new MongoDbService(mongoSettings);
                }
                else
                {
                    LogWarning("MongoDB settings not found in configuration", "MachineConfig");
                }

                // Perform initial sync on startup
                LogInfo("Starting initial sync...", "MachineConfig");
                await SyncWithMongoDb();

                LogSuccess("✓ MachineConfigurationController initialized", "MachineConfig");
            }
            catch (System.Exception ex)
            {
                LogError($"MachineConfigurationController initialization failed: {ex.Message}", "MachineConfig");
                LogError($"Stack trace: {ex.StackTrace}", "MachineConfig");
            }
        }

        private static async Task SyncWithMongoDb()
        {
            if (_fileManager == null || _mongoService == null || !_mongoService.IsConnected)
            {
                LogInfo("Skipping MongoDB sync (not connected)", "MachineConfig");
                return;
            }

            // Get machine name
            var settingsPath = Path.Combine(DataDirectory, "machineDataStorageSettings.json");
            if (!System.IO.File.Exists(settingsPath))
            {
                LogWarning("No machine name configured", "MachineConfig");
                return;
            }

            var localSettings = JsonSerializer.Deserialize<LocalMachineSettings>(System.IO.File.ReadAllText(settingsPath));
            if (localSettings == null || string.IsNullOrEmpty(localSettings.CurrentMachineName))
            {
                LogWarning("Machine name not set", "MachineConfig");
                return;
            }

            var machineName = localSettings.CurrentMachineName;
            LogInfo($"Syncing files for machine '{machineName}'", "MachineConfig");

            // Build unique list of files from both local and MongoDB
            var filesToSync = new System.Collections.Generic.HashSet<string>(ConfigurationFiles.SyncedFiles);

            // Add all local .json files (excluding .version files and machineDataStorageSettings)
            if (System.IO.Directory.Exists(DataDirectory))
            {
                var localFiles = System.IO.Directory.GetFiles(DataDirectory, "*.json")
                    .Select(f => System.IO.Path.GetFileName(f))
                    .Where(f => !f.EndsWith(".version.json") && f != "machineDataStorageSettings.json");

                foreach (var file in localFiles)
                {
                    filesToSync.Add(file);
                }
            }

            // Add all files from MongoDB for this machine
            var mongoFiles = await _mongoService.GetFileNamesForMachineAsync(machineName);
            foreach (var file in mongoFiles)
            {
                filesToSync.Add(file);
            }

            LogInfo($"Found {filesToSync.Count} unique files to sync", "MachineConfig");

            foreach (var fileName in filesToSync.OrderBy(f => f))
            {
                try
                {
                    var localVersion = _fileManager.GetVersion(fileName);
                    var mongoDoc = await _mongoService.LoadAsync(machineName, fileName);
                    var mongoVersion = mongoDoc?.Version ?? 0;

                    LogInfo($"  📁 {fileName}: Local v{localVersion} | MongoDB v{mongoVersion}", "MachineConfig");

                    if (mongoVersion > localVersion)
                    {
                        // MongoDB is newer - download
                        _fileManager.WriteData(fileName, mongoDoc!.Data, mongoDoc.Version);
                        LogSuccess($"    ✓ Downloaded {fileName} v{mongoVersion}", "MachineConfig");
                    }
                    else if (localVersion > mongoVersion && localVersion > 0)
                    {
                        // Local is newer - upload
                        var data = _fileManager.ReadData(fileName);
                        if (data != null)
                        {
                            await _mongoService.SaveAsync(machineName, fileName, data, localVersion);
                            LogSuccess($"    ✓ Uploaded {fileName} v{localVersion}", "MachineConfig");
                        }
                    }
                    else
                    {
                        LogInfo($"    ⏭️ Skipped {fileName} (versions match or both missing)", "MachineConfig");
                    }
                }
                catch (System.Exception ex)
                {
                    LogError($"  ✗ Failed to sync {fileName}: {ex.Message}", "MachineConfig");
                }
            }

            LogSuccess($"✓ Sync complete for machine '{machineName}'", "MachineConfig");
        }

        /// <summary>
        /// Get configuration file (MongoDB first, then local fallback)
        /// </summary>
        [HttpGet("{fileName}")]
        public async Task<IActionResult> GetConfiguration(string fileName)
        {
            LogInfo($"📥 GetConfiguration request for: {fileName}", "MachineConfig");

            if (_fileManager == null)
            {
                LogError("FileManager not initialized", "MachineConfig");
                return StatusCode(500, "Service not initialized");
            }

            try
            {
                // Try MongoDB first if connected
                if (_mongoService != null && _mongoService.IsConnected)
                {
                    var settingsPath = Path.Combine(DataDirectory, "machineDataStorageSettings.json");
                    if (System.IO.File.Exists(settingsPath))
                    {
                        var localSettings = JsonSerializer.Deserialize<LocalMachineSettings>(System.IO.File.ReadAllText(settingsPath));
                        if (localSettings != null && !string.IsNullOrEmpty(localSettings.CurrentMachineName))
                        {
                            LogInfo($"  Attempting to load {fileName} from MongoDB for machine '{localSettings.CurrentMachineName}'...", "MachineConfig");
                            var mongoDoc = await _mongoService.LoadAsync(localSettings.CurrentMachineName, fileName);

                            if (mongoDoc != null)
                            {
                                LogSuccess($"  ✓ Loaded {fileName} v{mongoDoc.Version} from MongoDB ({mongoDoc.Data.Length} bytes)", "MachineConfig");
                                return Ok(mongoDoc.Data);
                            }
                            else
                            {
                                LogWarning($"  {fileName} not found in MongoDB, trying local...", "MachineConfig");
                            }
                        }
                    }
                }
                else
                {
                    LogInfo($"  MongoDB not connected, reading from local files...", "MachineConfig");
                }

                // Fallback to local file
                var localVersion = _fileManager.GetVersion(fileName);
                var data = _fileManager.ReadData(fileName);

                if (data == null)
                {
                    LogWarning($"  ✗ {fileName} not found locally either", "MachineConfig");
                    return NotFound(new { message = $"{fileName} not found" });
                }

                LogSuccess($"  ✓ Loaded {fileName} v{localVersion} from local file ({data.Length} bytes)", "MachineConfig");
                return Ok(data);
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to get {fileName}: {ex.Message}", "MachineConfig");
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Save configuration file (auto-increments version)
        /// </summary>
        [HttpPost("{fileName}")]
        public async Task<IActionResult> SaveConfiguration(string fileName, [FromBody] string data)
        {
            if (_fileManager == null)
                return StatusCode(500, "Service not initialized");

            try
            {
                // Increment version and save locally
                var newVersion = _fileManager.IncrementAndWrite(fileName, data);

                // Upload to MongoDB if connected
                if (_mongoService != null && _mongoService.IsConnected)
                {
                    var settingsPath = Path.Combine(DataDirectory, "machineDataStorageSettings.json");
                    if (System.IO.File.Exists(settingsPath))
                    {
                        var localSettings = JsonSerializer.Deserialize<LocalMachineSettings>(System.IO.File.ReadAllText(settingsPath));
                        if (localSettings != null && !string.IsNullOrEmpty(localSettings.CurrentMachineName))
                        {
                            await _mongoService.SaveAsync(localSettings.CurrentMachineName, fileName, data, newVersion);
                        }
                    }
                }

                return Ok(new { version = newVersion, message = "Saved successfully" });
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to save {fileName}: {ex.Message}", "MachineConfig");
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Set current machine name.
        /// - New machine: seeds MongoDB with current local config under the new name, then syncs.
        /// - Existing machine: archives current data to zip, seeds new-machine config in MongoDB,
        ///   wipes local jobs/gcode, then syncs down from the new machine.
        /// </summary>
        [HttpPost("set-machine")]
        [HttpPost("SetCurrentMachine")]
        public async Task<IActionResult> SetCurrentMachine([FromBody] SetCurrentMachineRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.MachineName))
                    return BadRequest(new { message = "Machine name is required" });

                LogInfo($"💾 SetCurrentMachine request: '{request.MachineName}'", "MachineConfig");

                // ── Read current machine name ──────────────────────────────────────────
                var settingsPath = Path.Combine(DataDirectory, "machineDataStorageSettings.json");
                var currentSettings = System.IO.File.Exists(settingsPath)
                    ? JsonSerializer.Deserialize<LocalMachineSettings>(System.IO.File.ReadAllText(settingsPath))
                    : null;
                var oldMachineName = currentSettings?.CurrentMachineName ?? string.Empty;

                // ── Determine if the target machine already exists in MongoDB ──────────
                bool isExistingMachine = false;
                if (_mongoService != null && _mongoService.IsConnected && !string.IsNullOrEmpty(request.MachineName))
                {
                    var knownMachines = await _mongoService.GetMachineNamesAsync();
                    isExistingMachine = knownMachines.Contains(request.MachineName, System.StringComparer.OrdinalIgnoreCase);
                }

                LogInfo($"Target machine '{request.MachineName}' is {(isExistingMachine ? "EXISTING" : "NEW")}", "MachineConfig");

                if (isExistingMachine)
                {
                    // ── Step 1: Archive current data directory to a zip ────────────────
                    var timestamp = System.DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                    var safeName = string.IsNullOrEmpty(oldMachineName) ? "unknown" : oldMachineName;
                    var zipName = $"backup_{safeName}_{timestamp}.zip";
                    var zipPath = Path.Combine(DataDirectory, zipName);

                    LogInfo($"Creating archive: {zipName}", "MachineConfig");
                    try
                    {
                        // Resolve the logs directory so we can skip it (log files are locked)
                        var logDir = Services.SettingsManager.Settings?.Logging?.LogDirectory ?? string.Empty;
                        if (!string.IsNullOrEmpty(logDir))
                            logDir = Path.GetFullPath(logDir);

                        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
                        foreach (var file in System.IO.Directory.GetFiles(DataDirectory, "*", SearchOption.AllDirectories))
                        {
                            // Skip existing zip files to avoid nesting archives
                            if (file.EndsWith(".zip", System.StringComparison.OrdinalIgnoreCase)) continue;
                            // Skip log files — they are locked by the running process
                            if (!string.IsNullOrEmpty(logDir) && Path.GetFullPath(file).StartsWith(logDir, System.StringComparison.OrdinalIgnoreCase)) continue;
                            var relativePath = Path.GetRelativePath(DataDirectory, file);
                            zip.CreateEntryFromFile(file, relativePath, CompressionLevel.Optimal);
                        }
                        LogSuccess($"✓ Archive created: {zipName}", "MachineConfig");
                    }
                    catch (System.Exception ex)
                    {
                        LogError($"Archive creation failed: {ex.Message}", "MachineConfig");
                        return StatusCode(500, new { message = $"Archive failed: {ex.Message}" });
                    }

                    // ── Step 2: Delete old backup zips (keep only the new one) ────────
                    foreach (var oldZip in System.IO.Directory.GetFiles(DataDirectory, "*.zip"))
                    {
                        if (oldZip == zipPath) continue;
                        try { System.IO.File.Delete(oldZip); } catch { /* best effort */ }
                    }

                    // ── Step 3: Delete all local config files so sync starts fresh at version 0 ──
                    // This ensures SyncWithMongoDb downloads the existing machine's config from
                    // MongoDB rather than comparing against stale old-machine versions.
                    LogInfo("Clearing local config files to force fresh download...", "MachineConfig");
                    foreach (var file in System.IO.Directory.GetFiles(DataDirectory, "*.json"))
                    {
                        var fname = Path.GetFileName(file);
                        if (fname == "machineDataStorageSettings.json") continue;
                        try { System.IO.File.Delete(file); } catch { /* best effort */ }
                    }
                    LogSuccess("✓ Local config files cleared", "MachineConfig");

                    // ── Step 4: Wipe local jobs and gcode ─────────────────────────────
                    WipeDirectory(Path.Combine(DataDirectory, "jobs"));
                    WipeDirectory(Path.Combine(DataDirectory, "gcode"));
                    LogSuccess("✓ Local jobs and gcode cleared", "MachineConfig");
                }
                else
                {
                    // New machine: seed all current local config to MongoDB under the new name
                    if (_mongoService != null && _mongoService.IsConnected && _fileManager != null)
                    {
                        LogInfo($"Seeding config for new machine '{request.MachineName}' in MongoDB...", "MachineConfig");
                        var configFiles = System.IO.Directory.GetFiles(DataDirectory, "*.json")
                            .Select(f => Path.GetFileName(f))
                            .Where(f => !f.EndsWith(".version.json", System.StringComparison.OrdinalIgnoreCase)
                                     && f != "machineDataStorageSettings.json");

                        foreach (var fileName in configFiles)
                        {
                            try
                            {
                                var data = _fileManager.ReadData(fileName);
                                if (data == null) continue;
                                var version = _fileManager.GetVersion(fileName);
                                await _mongoService.SaveAsync(request.MachineName, fileName, data, version);
                                LogInfo($"  ✓ Seeded {fileName}", "MachineConfig");
                            }
                            catch (System.Exception ex)
                            {
                                LogWarning($"  ✗ Could not seed {fileName}: {ex.Message}", "MachineConfig");
                            }
                        }
                    }
                }

                // ── Write new machine name to settings ────────────────────────────────
                var newSettings = new LocalMachineSettings
                {
                    CurrentMachineName = request.MachineName,
                    LastSyncTime = System.DateTime.UtcNow
                };
                System.IO.File.WriteAllText(settingsPath, JsonSerializer.Serialize(newSettings,
                    new JsonSerializerOptions { WriteIndented = true }));

                // ── Sync machine config files for new machine name ────────────────────
                await SyncWithMongoDb();

                // ── Reinitialize job storage for the new machine ──────────────────────
                await JobStorageController.ReinitializeAsync();

                LogSuccess($"✓ Machine switched to '{request.MachineName}'", "MachineConfig");
                return Ok(new { message = $"Machine set to {request.MachineName}", isExistingMachine });
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to set current machine: {ex.Message}", "MachineConfig");
                return StatusCode(500, new { message = $"Failed to set current machine: {ex.Message}" });
            }
        }

        /// <summary>
        /// Delete all files in a directory without removing the directory itself.
        /// </summary>
        private static void WipeDirectory(string directory)
        {
            if (!System.IO.Directory.Exists(directory)) return;
            foreach (var file in System.IO.Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
            {
                try { System.IO.File.Delete(file); }
                catch (System.Exception ex) { LogWarning($"Could not delete {file}: {ex.Message}", "MachineConfig"); }
            }
        }

        /// <summary>
        /// Get list of available machine names from MongoDB
        /// </summary>
        /// <returns>Array of machine names</returns>
        [HttpGet("GetMachineNames")]
        [ProducesResponseType(typeof(string[]), 200)]
        public async Task<ActionResult<string[]>> GetMachineNames()
        {
            try
            {
                LogInfo("GetMachineNames request", "MachineConfig");

                if (_mongoService == null || !_mongoService.IsConnected)
                {
                    LogWarning("MongoDB not connected, returning empty list", "MachineConfig");
                    return Ok(new string[0]);
                }

                var machines = await _mongoService.GetMachineNamesAsync();

                // Add current local machine if not in list
                var settingsPath = Path.Combine(DataDirectory, "machineDataStorageSettings.json");
                if (System.IO.File.Exists(settingsPath))
                {
                    var localSettings = JsonSerializer.Deserialize<LocalMachineSettings>(System.IO.File.ReadAllText(settingsPath));
                    if (localSettings != null && !string.IsNullOrEmpty(localSettings.CurrentMachineName) &&
                        !machines.Contains(localSettings.CurrentMachineName))
                    {
                        machines.Add(localSettings.CurrentMachineName);
                    }
                }

                machines = machines.Distinct().OrderBy(x => x).ToList();
                LogSuccess($"✓ Retrieved {machines.Count} machine names", "MachineConfig");
                return Ok(machines.ToArray());
            }
            catch (System.Exception ex)
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
                var settingsPath = Path.Combine(DataDirectory, "machineDataStorageSettings.json");
                if (!System.IO.File.Exists(settingsPath))
                {
                    return Ok(string.Empty);
                }

                var settings = JsonSerializer.Deserialize<LocalMachineSettings>(System.IO.File.ReadAllText(settingsPath));
                var machineName = settings?.CurrentMachineName ?? string.Empty;
                LogInfo($"Current machine: {machineName}", "MachineConfig");
                return Ok(machineName);
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to get current machine: {ex.Message}", "MachineConfig");
                return StatusCode(500, new { message = $"Failed to get current machine: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get default PLC (latest version)
        /// </summary>
        /// <returns>Default PLC data</returns>
        [HttpGet("GetDefaultPLC")]
        [ProducesResponseType(typeof(string), 200)]
        public async Task<ActionResult<string>> GetDefaultPLC()
        {
            try
            {
                LogInfo("📖 GetDefaultPLC request", "MachineConfig");

                // Try MongoDB first if connected
                if (_mongoService != null && _mongoService.IsConnected)
                {
                    var result = await _mongoService.GetLatestDefaultPlcAsync();
                    if (result != null)
                    {
                        LogSuccess($"✓ Retrieved default PLC version: '{result.VersionName}'", "MachineConfig");
                        return Ok(result.JsonData);
                    }
                }

                // Fallback to plcSystemDefault.json
                LogInfo("Using plcSystemDefault.json as default PLC", "MachineConfig");
                if (_fileManager != null)
                {
                    var data = _fileManager.ReadData("plcSystemDefault.json");
                    if (data != null)
                    {
                        return Ok(data);
                    }
                }

                LogWarning("Default PLC not found", "MachineConfig");
                return NotFound(new { message = "Default PLC not found" });
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to get default PLC: {ex.Message}", "MachineConfig");
                return StatusCode(500, new { message = $"Failed to get default PLC: {ex.Message}" });
            }
        }

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

                if (_mongoService == null || !_mongoService.IsConnected)
                {
                    return StatusCode(500, new { message = "MongoDB not connected" });
                }

                var success = await _mongoService.SaveDefaultPlcAsync(
                    request.VersionName,
                    request.JsonData,
                    request.MarkAsLatest,
                    request.Description,
                    request.CreatedBy
                );

                if (success)
                {
                    LogSuccess($"✓ Stored default PLC version: '{request.VersionName}'", "MachineConfig");
                    return Ok(new { message = $"Default PLC version '{request.VersionName}' stored successfully" });
                }
                else
                {
                    return StatusCode(500, new { message = "Failed to store default PLC version" });
                }
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to store default PLC: {ex.Message}", "MachineConfig");
                return StatusCode(500, new { message = $"Failed to store default PLC: {ex.Message}" });
            }
        }

        /// <summary>
        /// Delete all MongoDB data for a machine (configs, jobs, G-code files).
        /// Does NOT touch local files — only removes the machine from the cloud.
        /// If deleting the currently active machine, provide switchToMachine to switch first.
        /// </summary>
        [HttpDelete("DeleteMachine/{machineName}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteMachine(string machineName, [FromQuery] string? switchToMachine = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(machineName))
                    return BadRequest(new { message = "Machine name is required" });

                if (_mongoService == null || !_mongoService.IsConnected)
                    return StatusCode(500, new { message = "MongoDB not connected" });

                // Check if we are deleting the currently active machine
                var settingsPath = Path.Combine(DataDirectory, "machineDataStorageSettings.json");
                var current = System.IO.File.Exists(settingsPath)
                    ? JsonSerializer.Deserialize<LocalMachineSettings>(System.IO.File.ReadAllText(settingsPath))
                    : null;
                bool isDeletingActive = string.Equals(current?.CurrentMachineName, machineName, System.StringComparison.OrdinalIgnoreCase);

                if (isDeletingActive)
                {
                    if (string.IsNullOrWhiteSpace(switchToMachine))
                        return BadRequest(new { message = $"'{machineName}' is the active machine. Provide switchToMachine to switch before deleting." });

                    if (string.Equals(switchToMachine, machineName, System.StringComparison.OrdinalIgnoreCase))
                        return BadRequest(new { message = "switchToMachine must be a different machine than the one being deleted." });

                    LogInfo($"Switching from '{machineName}' to '{switchToMachine}' before delete...", "MachineConfig");
                    var switchResult = await SetCurrentMachine(new SetCurrentMachineRequest { MachineName = switchToMachine });
                    if (switchResult is not OkObjectResult)
                        return StatusCode(500, new { message = $"Failed to switch to '{switchToMachine}' before deleting '{machineName}'." });

                    LogSuccess($"✓ Switched to '{switchToMachine}'", "MachineConfig");
                }

                LogInfo($"🗑️ Deleting all MongoDB data for machine '{machineName}'", "MachineConfig");

                var (configs, jobs, gcode, camProjects) = await _mongoService.DeleteAllForMachineAsync(machineName);

                LogSuccess($"✓ Deleted machine '{machineName}': {configs} config(s), {jobs} job(s), {gcode} G-code file(s), {camProjects} CAM project(s)", "MachineConfig");
                return Ok(new
                {
                    message = $"Machine '{machineName}' deleted from MongoDB",
                    switchedTo = isDeletingActive ? switchToMachine : null,
                    deletedConfigs = configs,
                    deletedJobs = jobs,
                    deletedGCodeFiles = gcode
                });
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to delete machine '{machineName}': {ex.Message}", "MachineConfig");
                return StatusCode(500, new { message = $"Failed to delete machine: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get sync status
        /// </summary>
        [HttpGet("sync-status")]
        public IActionResult GetSyncStatus()
        {
            var settingsPath = Path.Combine(DataDirectory, "machineDataStorageSettings.json");
            LocalMachineSettings? settings = null;

            if (System.IO.File.Exists(settingsPath))
            {
                settings = JsonSerializer.Deserialize<LocalMachineSettings>(System.IO.File.ReadAllText(settingsPath));
            }

            return Ok(new SyncStatusResponse
            {
                MongoDbOnline = _mongoService?.IsConnected ?? false,
                CurrentMachineName = settings?.CurrentMachineName ?? "",
                LastSyncTime = settings?.LastSyncTime,
                Message = _mongoService?.IsConnected ?? false ? "Online" : "Offline"
            });
        }

        /// <summary>
        /// Manually trigger sync with MongoDB
        /// </summary>
        [HttpPost("sync")]
        public async Task<IActionResult> TriggerSync()
        {
            try
            {
                LogInfo("Manual sync triggered", "MachineConfig");
                await SyncWithMongoDb();
                return Ok(new { message = "Sync completed" });
            }
            catch (System.Exception ex)
            {
                LogError($"Sync failed: {ex.Message}", "MachineConfig");
                return StatusCode(500, ex.Message);
            }
        }
    }
}
