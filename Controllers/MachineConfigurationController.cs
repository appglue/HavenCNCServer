using Microsoft.AspNetCore.Mvc;
using HavenCNCServer.Services;
using HavenCNCServer.Models;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Controllers
{
    [ApiController]
    [Route("api/configuration")]
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

            foreach (var fileName in ConfigurationFiles.SyncedFiles)
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
        /// Set current machine name
        /// </summary>
        [HttpPost("set-machine")]
        public async Task<IActionResult> SetMachine([FromBody] SetCurrentMachineRequest request)
        {
            try
            {
                var settingsPath = Path.Combine(DataDirectory, "machineDataStorageSettings.json");
                var settings = new LocalMachineSettings
                {
                    CurrentMachineName = request.MachineName,
                    LastSyncTime = System.DateTime.UtcNow
                };
                System.IO.File.WriteAllText(settingsPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));

                // Re-sync with new machine
                await SyncWithMongoDb();

                return Ok(new { message = $"Machine set to {request.MachineName}" });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, ex.Message);
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
