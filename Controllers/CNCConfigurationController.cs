using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using HavenCNCServer.Models;
using HavenCNCServer.Centroid;
using HavenCNCServer.Centroid.Data;
using HavenCNCServer.Services;

namespace HavenCNCServer.Controllers
{
    /// <summary>
    /// CNC Configuration Management - Handles configuration data storage, retrieval, and checkpoint operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CNCConfigurationController : ControllerBase
    {
        private readonly string _dataDirectory;
        private readonly string _checkpointsDirectory;

        /// <summary>
        /// Initializes a new instance of the CNCConfigurationController
        /// </summary>
        public CNCConfigurationController()
        {
            // Use centralized data directory from settings
            var dataDir = SettingsManager.Settings.Files.DataDirectory;
            if (string.IsNullOrEmpty(dataDir))
            {
                // Fallback to old behavior if not set
                dataDir = Path.Combine(Directory.GetCurrentDirectory(), "data");
                Services.LoggingService.LogWarning("DataDirectory not set in settings.json, using fallback", "Config");
            }
            _dataDirectory = dataDir;
            _checkpointsDirectory = Path.Combine(_dataDirectory, "checkpoints");

            // Ensure directories exist
            Directory.CreateDirectory(_dataDirectory);
            Directory.CreateDirectory(_checkpointsDirectory);

            Services.LoggingService.LogInfo($"CNCConfigurationController initialized. Data directory: {_dataDirectory}", "Config");
        }

        #region Data Management

        /// <summary>
        /// Get data by name
        /// </summary>
        /// <param name="name">Name of the data to retrieve</param>
        /// <returns>Data content or null if not found</returns>
        [HttpGet("GetData/{name}")]
        public string? GetData(string name)
        {
            Services.LoggingService.LogInfo($"🔵 GetData ENDPOINT HIT - name parameter: '{name}'", "Config");
            try
            {
                Services.LoggingService.LogInfo($"📖 GetData request received: '{name}'", "Config");
                Services.LoggingService.LogDebug($"Data directory: {_dataDirectory}", "Config");

                var dataPath = Path.Combine(_dataDirectory, name);
                Services.LoggingService.LogDebug($"Looking for data file at: {dataPath}", "Config");

                if (!System.IO.File.Exists(dataPath))
                {
                    Services.LoggingService.LogWarning($"❌ Data '{name}' not found at: {dataPath}", "Config");
                    Services.LoggingService.LogDebug($"File.Exists returned false for: {dataPath}", "Config");
                    return null; // Return null instead of throwing error
                }

                Services.LoggingService.LogDebug($"File exists, reading content from: {dataPath}", "Config");
                var versioned = LoadVersionedFile(name);
                if (versioned == null)
                {
                    // Fallback: read raw file if no version metadata
                    var content = System.IO.File.ReadAllText(dataPath);
                    Services.LoggingService.LogSuccess($"✓ GetData '{name}' returned {content.Length} chars (no version metadata)", "Config");
                    return content;
                }

                Services.LoggingService.LogSuccess($"✓ GetData '{name}' returned v{versioned.Metadata.Version} ({versioned.Data.Length} chars)", "Config");
                return versioned.Data;
            }
            catch (Exception ex)
            {
                Services.LoggingService.LogError($"Failed to read data '{name}': {ex.Message}", "Config");
                Services.LoggingService.LogError($"Stack trace: {ex.StackTrace}", "Config");
                throw new InvalidOperationException($"Failed to read data '{name}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Set data with specified name and content
        /// </summary>
        /// <param name="request">Data setting request</param>
        /// <returns>Success response</returns>
        [HttpPost("SetData")]
        public async Task SetData([FromBody] ConfigurationDataRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    Services.LoggingService.LogError("SetData request with empty name", "Config");
                    throw new ArgumentException("Data name cannot be empty");
                }

                Services.LoggingService.LogInfo($"💾 SetData request: '{request.Name}' ({(request.Content?.Length ?? 0)} chars)", "Config");

                var dataPath = Path.Combine(_dataDirectory, request.Name);
                var versionPath = Path.Combine(_dataDirectory, $"{request.Name}.version.json");

                // Load current version to increment it
                var currentVersioned = LoadVersionedFile(request.Name);
                var currentVersion = currentVersioned?.Metadata.Version ?? 0;

                // Create new versioned file with incremented version
                var newVersioned = VersionedConfigurationFile.Create(request.Name, request.Content ?? string.Empty, currentVersion);

                // Create backup before saving
                if (System.IO.File.Exists(dataPath))
                {
                    Services.BackupService.CreateBackup(dataPath);
                }

                // Save data file
                System.IO.File.WriteAllText(dataPath, newVersioned.Data);

                // Save version metadata file
                var versionJson = System.Text.Json.JsonSerializer.Serialize(newVersioned.Metadata, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(versionPath, versionJson);

                Services.LoggingService.LogSuccess($"✓ SetData '{request.Name}' saved v{newVersioned.Metadata.Version} to: {dataPath}", "Config");

                // Sync to MongoDB if enabled
                await SyncToMongoDbAsync(request.Name, System.Text.Json.JsonSerializer.Serialize(newVersioned));
            }
            catch (Exception ex)
            {
                Services.LoggingService.LogError($"Failed to save data '{request.Name}': {ex.Message}", "Config");
                throw new InvalidOperationException($"Failed to save data '{request.Name}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Sync configuration file to MongoDB
        /// </summary>
        private async Task SyncToMongoDbAsync(string fileName, string content)
        {
            try
            {
                var mongoSettings = SettingsManager.Settings.MongoDB;
                if (!mongoSettings.Enabled)
                {
                    return; // MongoDB disabled, skip sync
                }

                var mongoService = new Services.MongoDbService(mongoSettings);
                if (!mongoService.IsConnected)
                {
                    Services.LoggingService.LogWarning($"MongoDB offline, skipping sync for {fileName}", "MongoDB");
                    return;
                }

                // Load current machine name
                // Use ONLY the configured data directory from settings - no fallback
                var dataDirectory = SettingsManager.Settings.Files.DataDirectory;
                if (string.IsNullOrEmpty(dataDirectory))
                {
                    throw new InvalidOperationException("DataDirectory not configured in settings.json");
                }
                var localSettingsPath = Path.Combine(dataDirectory, "machineDataStorageSettings.json");
                if (!System.IO.File.Exists(localSettingsPath))
                {
                    Services.LoggingService.LogInfo("No machine name set, skipping MongoDB sync", "MongoDB");
                    return;
                }

                var localSettingsJson = System.IO.File.ReadAllText(localSettingsPath);
                var localSettings = System.Text.Json.JsonSerializer.Deserialize<Models.LocalMachineSettings>(localSettingsJson);

                if (localSettings == null || string.IsNullOrEmpty(localSettings.CurrentMachineName))
                {
                    Services.LoggingService.LogInfo("Machine name not set, skipping MongoDB sync", "MongoDB");
                    return;
                }

                // Only sync known configuration files
                if (!ConfigurationFiles.IsSyncedFile(fileName))
                {
                    return; // Not a config file we sync
                }

                var success = await mongoService.SaveMachineConfigurationAsync(
                    localSettings.CurrentMachineName,
                    fileName,
                    content
                );

                if (success)
                {
                    Services.LoggingService.LogSuccess($"✓ Synced {fileName} to MongoDB for machine '{localSettings.CurrentMachineName}'", "MongoDB");
                }
            }
            catch (Exception ex)
            {
                Services.LoggingService.LogError($"MongoDB sync error for {fileName}: {ex.Message}", "MongoDB");
                // Don't throw - MongoDB sync failure shouldn't break the save operation
            }
        }

        /// <summary>
        /// List all available data names
        /// </summary>
        /// <returns>Array of data names (full filenames with extensions)</returns>
        [HttpGet("ListData")]
        public string[] ListData()
        {
            try
            {
                var files = Directory.GetFiles(_dataDirectory, "*.json")
                    .Select(f => Path.GetFileName(f))  // Get full filename with .json extension
                    .Where(name => !string.IsNullOrEmpty(name))
                    .OrderBy(name => name)
                    .ToArray();

                return files;
            }
            catch (Exception ex)
            {
                Services.LoggingService.LogError($"Failed to list data: {ex.Message}", "Config");
                throw new InvalidOperationException($"Failed to list data: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Delete data by name
        /// </summary>
        /// <param name="name">Name of the data to delete</param>
        /// <returns>Success response</returns>
        [HttpDelete("DeleteData/{name}")]
        public void DeleteData(string name)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, name);

                if (!System.IO.File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Data '{name}' not found");
                }

                System.IO.File.Delete(filePath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to delete data '{name}': {ex.Message}", ex);
            }
        }

        #endregion

        #region Checkpoint Management

        /// <summary>
        /// Save checkpoint with multiple data items
        /// </summary>
        /// <param name="request">Checkpoint save request</param>
        /// <returns>Success response</returns>
        [HttpPost("SaveCheckpoint")]
        public void SaveCheckpoint([FromBody] CheckpointSaveRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.CheckpointName))
                {
                    throw new ArgumentException("Checkpoint name cannot be empty");
                }

                if (request.Data == null || !request.Data.Any())
                {
                    throw new ArgumentException("At least one data item is required");
                }

                var checkpointDir = Path.Combine(_checkpointsDirectory, request.CheckpointName);
                Directory.CreateDirectory(checkpointDir);

                // Save each data item to the checkpoint directory
                foreach (var dataItem in request.Data)
                {
                    if (string.IsNullOrWhiteSpace(dataItem.Name))
                        continue;

                    var filePath = Path.Combine(checkpointDir, dataItem.Name);

                    // Create backup before saving
                    Services.BackupService.CreateBackup(filePath);

                    System.IO.File.WriteAllText(filePath, dataItem.Content ?? string.Empty);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save checkpoint '{request.CheckpointName}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// List all available checkpoints
        /// </summary>
        /// <returns>Array of checkpoint names with metadata</returns>
        [HttpGet("ListCheckpoints")]
        public CheckpointInfo[] ListCheckpoints()
        {
            try
            {
                return Directory.GetDirectories(_checkpointsDirectory)
                    .Select(dir =>
                    {
                        var name = Path.GetFileName(dir);
                        var fileCount = Directory.GetFiles(dir, "*.json").Length;
                        var createdTime = Directory.GetCreationTime(dir);
                        return new CheckpointInfo
                        {
                            Name = name,
                            FileCount = fileCount,
                            Created = createdTime.ToString("yyyy-MM-dd HH:mm:ss")
                        };
                    })
                    .OrderByDescending(c => c.Created)
                    .ToArray();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to list checkpoints: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Restore checkpoint by copying all its data to the main data directory
        /// </summary>
        /// <param name="checkpointName">Name of the checkpoint to restore</param>
        /// <returns>Success response</returns>
        [HttpPost("RestoreCheckpoint/{checkpointName}")]
        public int RestoreCheckpoint(string checkpointName)
        {
            try
            {
                var checkpointDir = Path.Combine(_checkpointsDirectory, checkpointName);

                if (!Directory.Exists(checkpointDir))
                {
                    throw new DirectoryNotFoundException($"Checkpoint '{checkpointName}' not found");
                }

                var checkpointFiles = Directory.GetFiles(checkpointDir, "*.json");
                var restoredCount = 0;

                foreach (var checkpointFile in checkpointFiles)
                {
                    var fileName = Path.GetFileName(checkpointFile);
                    var targetPath = Path.Combine(_dataDirectory, fileName);

                    var content = System.IO.File.ReadAllText(checkpointFile);
                    System.IO.File.WriteAllText(targetPath, content);
                    restoredCount++;
                }

                return restoredCount;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to restore checkpoint '{checkpointName}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Delete checkpoint
        /// </summary>
        /// <param name="checkpointName">Name of the checkpoint to delete</param>
        /// <returns>Success response</returns>
        [HttpDelete("DeleteCheckpoint/{checkpointName}")]
        public void DeleteCheckpoint(string checkpointName)
        {
            try
            {
                var checkpointDir = Path.Combine(_checkpointsDirectory, checkpointName);

                if (!Directory.Exists(checkpointDir))
                {
                    throw new DirectoryNotFoundException($"Checkpoint '{checkpointName}' not found");
                }

                Directory.Delete(checkpointDir, true);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to delete checkpoint '{checkpointName}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get checkpoint contents (list of data items in checkpoint)
        /// </summary>
        /// <param name="checkpointName">Name of the checkpoint</param>
        /// <returns>Array of data items in the checkpoint</returns>
        [HttpGet("GetCheckpointContents/{checkpointName}")]
        public DataItem[] GetCheckpointContents(string checkpointName)
        {
            try
            {
                var checkpointDir = Path.Combine(_checkpointsDirectory, checkpointName);

                if (!Directory.Exists(checkpointDir))
                {
                    throw new DirectoryNotFoundException($"Checkpoint '{checkpointName}' not found");
                }

                var files = Directory.GetFiles(checkpointDir, "*.json");
                var contents = new List<DataItem>();

                foreach (var file in files)
                {
                    var name = Path.GetFileName(file);  // Keep full filename with .json
                    var content = System.IO.File.ReadAllText(file);
                    contents.Add(new DataItem { Name = name, Content = content });
                }

                return contents.ToArray();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get checkpoint contents '{checkpointName}': {ex.Message}", ex);
            }
        }

        #endregion

        #region Machine Configuration

        /// <summary>
        /// Configure complete machine setup with all systems
        /// </summary>
        /// <param name="config">Complete machine configuration</param>
        /// <returns>Configuration result</returns>
        [HttpPost("ConfigureCompleteMachine")]
        public bool ConfigureCompleteMachine([FromBody] CompleteMachineConfiguration config)
        {
            try
            {
                LoggingService.Log("=== ConfigureCompleteMachine API called ===");
                LoggingService.Log($"Configuration contains: {config.Axes?.Count ?? 0} axes, Spindle: {config.Spindle != null}, Probe: {config.Probe != null}, PWM: {config.PWMOutputs?.Count ?? 0}, GlobalSystem: {config.GlobalSystem != null}");

                // Log GlobalSystem values if present
                if (config.GlobalSystem != null)
                {
                    LoggingService.Log($"GlobalSystem StepFrequency: {config.GlobalSystem.StepFrequency}");
                    LoggingService.Log($"GlobalSystem DriveFaultDelay: {config.GlobalSystem.DriveFaultDelay}");
                    LoggingService.Log($"GlobalSystem LinearJogIncrement: {config.GlobalSystem.LinearJogIncrement}");
                    LoggingService.Log($"GlobalSystem RotaryJogIncrement: {config.GlobalSystem.RotaryJogIncrement}");
                }

                // Get configured axes
                var configuredAxes = (config.Axes ?? new List<AxisConfiguration>())
                    .Where(a => !string.IsNullOrEmpty(a.AxisType) && a.AxisType.ToLower() != "unset")
                    .ToList();

                LoggingService.Log($"Received {configuredAxes.Count} configured axes");

                // Configure the machine with the provided axes
                var result = CentroidConfigUtil.ConfigureCompleteMachine(
                    configuredAxes,
                    config.Spindle!,
                    config.Probe,
                    config.PWMOutputs,
                    atc: null,
                    touchPlate: null,
                    secondSpindle: null,
                    globalSystem: config.GlobalSystem
                );

                if (result)
                {
                    // CNC12 has 6 axis slots - set unused slots to 'N' (None) and hide from DRO
                    // This prevents unconfigured axes from showing as "U" or other default letters
                    int maxAxes = 6;
                    int configuredCount = configuredAxes.Count;

                    if (configuredCount < maxAxes)
                    {
                        LoggingService.Log($"Configuring unused axis slots {configuredCount + 1}-{maxAxes} as 'N' (None)");

                        for (int axisNum = configuredCount + 1; axisNum <= maxAxes; axisNum++)
                        {
                            try
                            {
                                var unconfiguredAxis = new AxisConfiguration
                                {
                                    AxisNumber = axisNum,
                                    AxisType = "N",  // Set label to 'N' for None/unconfigured
                                    HideFromDRO = true
                                };

                                // This will set the label to 'N' and hide from DRO
                                CentroidConfigUtil.ConfigureAxis(unconfiguredAxis);
                                LoggingService.Log($"  ✓ Axis {axisNum} set to 'N' and hidden from DRO");
                            }
                            catch (Exception ex)
                            {
                                LoggingService.Log($"  ⚠ Failed to configure axis {axisNum}: {ex.Message}", LoggingService.LogLevel.Warning);
                            }
                        }
                    }

                    LoggingService.Log("=== ConfigureCompleteMachine API completed successfully ===");
                }
                else
                {
                    LoggingService.Log("=== ConfigureCompleteMachine API failed - check detailed logs above ===", LoggingService.LogLevel.Error);
                }

                return result;
            }
            catch (Exception ex)
            {
                LoggingService.Log($"EXCEPTION in ConfigureCompleteMachine API: {ex.Message}", LoggingService.LogLevel.Error);
                LoggingService.Log($"Stack trace: {ex.StackTrace}", LoggingService.LogLevel.Error);
                throw new InvalidOperationException($"Failed to configure machine: {ex.Message}", ex);
            }
        }


        /// <summary>
        /// Configure axis settings
        /// </summary>
        /// <param name="config">Axis configuration</param>
        /// <returns>Configuration result</returns>
        [HttpPost("ConfigureAxis")]
        public bool ConfigureAxis([FromBody] AxisConfiguration config)
        {
            try
            {
                return CentroidConfigUtil.ConfigureAxis(config);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to configure axis: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Configure spindle settings
        /// </summary>
        /// <param name="config">Spindle configuration</param>
        /// <returns>Configuration result</returns>
        [HttpPost("ConfigureSpindle")]
        public bool ConfigureSpindle([FromBody] SpindleConfiguration config)
        {
            try
            {
                return CentroidConfigUtil.ConfigureSpindle(config);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to configure spindle: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Configure PWM output settings
        /// </summary>
        /// <param name="config">PWM configuration</param>
        /// <returns>Configuration result</returns>
        [HttpPost("ConfigurePWM")]
        public bool ConfigurePWM([FromBody] PWMConfiguration config)
        {
            try
            {
                return CentroidConfigUtil.ConfigurePWM(config);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to configure PWM: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Configure probe settings
        /// </summary>
        /// <param name="config">Probe configuration</param>
        /// <returns>Configuration result</returns>
        [HttpPost("ConfigureProbe")]
        public bool ConfigureProbe([FromBody] ProbeConfiguration config)
        {
            try
            {
                return CentroidConfigUtil.ConfigureProbe(config);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to configure probe: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Configure tool touch off settings
        /// </summary>
        /// <param name="config">Tool touch off configuration</param>
        /// <returns>Configuration result</returns>
        [HttpPost("ConfigureToolTouchOff")]
        public bool ConfigureToolTouchOff([FromBody] ToolTouchOffConfiguration config)
        {
            try
            {
                return CentroidConfigUtil.ConfigureToolTouchOff(config);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to configure tool touch off: {ex.Message}", ex);
            }
        }

        #endregion

        #region New Configuration Endpoints

        /// <summary>
        /// Configure touch plate system
        /// </summary>
        /// <param name="config">Touch plate configuration</param>
        /// <returns>Configuration result</returns>
        [HttpPost("ConfigureTouchPlate")]
        public bool ConfigureTouchPlate([FromBody] TouchPlateConfiguration config)
        {
            try
            {
                if (config == null)
                {
                    throw new ArgumentNullException(nameof(config), "Touch plate configuration is required");
                }

                return CentroidConfigUtil.ConfigureTouchPlate(config);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to configure touch plate: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Configure second spindle
        /// </summary>
        /// <param name="config">Second spindle configuration</param>
        /// <returns>Configuration result</returns>
        [HttpPost("ConfigureSecondSpindle")]
        public bool ConfigureSecondSpindle([FromBody] SecondSpindleConfiguration config)
        {
            try
            {
                if (config == null)
                {
                    throw new ArgumentNullException(nameof(config), "Second spindle configuration is required");
                }

                return CentroidConfigUtil.ConfigureSecondSpindle(config);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to configure second spindle: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Configure global system settings
        /// </summary>
        /// <param name="config">Global system configuration</param>
        /// <returns>Configuration result</returns>
        [HttpPost("ConfigureGlobalSystem")]
        public bool ConfigureGlobalSystem([FromBody] GlobalSystemConfiguration config)
        {
            try
            {
                if (config == null)
                {
                    throw new ArgumentNullException(nameof(config), "Global system configuration is required");
                }

                return CentroidConfigUtil.ConfigureGlobalSystem(config);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to configure global system: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get current step frequency
        /// </summary>
        /// <returns>Current step frequency value</returns>
        [HttpGet("GetStepFrequency")]
        public int GetStepFrequency()
        {
            try
            {
                return CNCUtils.GetStepFrequency();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get step frequency: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Set step frequency
        /// </summary>
        /// <param name="request">Step frequency request</param>
        /// <returns>Configuration result</returns>
        [HttpPost("SetStepFrequency")]
        public void SetStepFrequency([FromBody] StepFrequencyRequest request)
        {
            try
            {
                if (request?.Frequency == null)
                {
                    throw new ArgumentException("Step frequency is required");
                }

                // Validate frequency against allowed values
                var allowedFrequencies = new[] { 100000, 200000, 240000, 300000, 400000 };
                if (!allowedFrequencies.Contains(request.Frequency))
                {
                    throw new ArgumentException($"Invalid step frequency: {request.Frequency}. " +
                        $"Allowed values: {string.Join(", ", allowedFrequencies)}");
                }

                CNCUtils.SetStepFrequency(request.Frequency);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to set step frequency: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get system hardware information
        /// </summary>
        /// <returns>Hardware information</returns>
        [HttpGet("GetSystemHardwareInfo")]
        public SystemHardwareInfo GetSystemHardwareInfo()
        {
            try
            {
                return new SystemHardwareInfo
                {
                    AvailableInputs = CNCUtils.GetAvailableInputPorts()?.ToList() ?? new List<int>(),
                    AvailableOutputs = CNCUtils.GetAvailableOutputPorts()?.ToList() ?? new List<int>(),
                    TotalInputs = CNCUtils.GetAvailableInputPorts()?.Length ?? 0,
                    TotalOutputs = CNCUtils.GetAvailableOutputPorts()?.Length ?? 0
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get system hardware info: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get individual parameter value
        /// </summary>
        /// <param name="parameter">Parameter identifier</param>
        /// <returns>Parameter value</returns>
        [HttpGet("GetParameter/{parameter}")]
        public ParameterValue GetParameter(int parameter)
        {
            try
            {
                if (!Enum.IsDefined(typeof(CentroidParameters), parameter))
                {
                    throw new ArgumentException($"Invalid parameter number: {parameter}", nameof(parameter));
                }

                var centroidParam = (CentroidParameters)parameter;
                double value = CNCUtils.GetParameterValue(centroidParam);

                return new ParameterValue
                {
                    Parameter = parameter,
                    ParameterName = centroidParam.ToString(),
                    Value = value,
                    Message = $"Parameter {parameter} ({centroidParam}) = {value}"
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get parameter {parameter}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Set individual parameter value
        /// </summary>
        /// <param name="request">Parameter set request</param>
        /// <returns>Set result</returns>
        [HttpPost("SetParameter")]
        public ParameterValue SetParameter([FromBody] ParameterSetRequest request)
        {
            try
            {
                if (request == null)
                {
                    throw new ArgumentNullException(nameof(request), "Parameter request is required");
                }

                if (!Enum.IsDefined(typeof(CentroidParameters), request.Parameter))
                {
                    throw new ArgumentException($"Invalid parameter number: {request.Parameter}", nameof(request));
                }

                var centroidParam = (CentroidParameters)request.Parameter;
                CNCUtils.SetParameterValue(centroidParam, request.Value);

                return new ParameterValue
                {
                    Parameter = request.Parameter,
                    ParameterName = centroidParam.ToString(),
                    Value = request.Value,
                    Message = $"Parameter {request.Parameter} ({centroidParam}) set to {request.Value}"
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to set parameter {request?.Parameter}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get all available parameters
        /// </summary>
        /// <returns>List of available parameters</returns>
        [HttpGet("GetAvailableParameters")]
        public AvailableParametersResponse GetAvailableParameters()
        {
            try
            {
                var parameters = Enum.GetValues<CentroidParameters>()
                    .Select(p => new ParameterInfo
                    {
                        Number = (int)p,
                        Name = p.ToString(),
                        Description = GetParameterDescription(p)
                    })
                    .OrderBy(p => p.Number)
                    .ToArray();

                return new AvailableParametersResponse
                {
                    Message = "Available parameters retrieved successfully",
                    ParameterCount = parameters.Length,
                    Parameters = parameters
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get available parameters: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Helper method to get parameter description from XML documentation
        /// </summary>
        private string GetParameterDescription(CentroidParameters parameter)
        {
            // This could be enhanced to read XML documentation at runtime
            // For now, return a basic description
            return parameter switch
            {
                CentroidParameters.SPINDLE_COUNTS_REV_PARM => "Spindle encoder counts per revolution",
                CentroidParameters.SPINDLE_AXIS_PARM => "Spindle axis assignment",
                CentroidParameters.RIGID_TAPPING_PARM => "Rigid tapping enable",
                CentroidParameters.SPINDLE_DECEL_TIME_PARM => "Spindle deceleration time",
                CentroidParameters.LOW_GEAR_RATIO_PARM => "Low gear ratio",
                CentroidParameters.HIGH_GEAR_RATIO_PARM => "High gear ratio",
                _ => "CNC parameter"
            };
        }

        /// <summary>
        /// Load a versioned configuration file from disk (data + version metadata)
        /// </summary>
        private VersionedConfigurationFile? LoadVersionedFile(string fileName)
        {
            try
            {
                var dataPath = Path.Combine(_dataDirectory, fileName);
                var versionPath = Path.Combine(_dataDirectory, $"{fileName}.version.json");

                // Data file must exist
                if (!System.IO.File.Exists(dataPath))
                {
                    return null;
                }

                var data = System.IO.File.ReadAllText(dataPath);

                // If version file doesn't exist, return null (will trigger sync from MongoDB)
                if (!System.IO.File.Exists(versionPath))
                {
                    return null;
                }

                var versionJson = System.IO.File.ReadAllText(versionPath);
                var metadata = System.Text.Json.JsonSerializer.Deserialize<ConfigurationMetadata>(versionJson);

                if (metadata == null)
                {
                    return null;
                }

                return new VersionedConfigurationFile
                {
                    Metadata = metadata,
                    Data = data
                };
            }
            catch (Exception ex)
            {
                Services.LoggingService.LogError($"Failed to load versioned file {fileName}: {ex.Message}", "Config");
                return null;
            }
        }

        #endregion
    }
}

/// <summary>
/// Request model for setting step frequency
/// </summary>
public class StepFrequencyRequest
{
    /// <summary>
    /// Step frequency in Hz (100,000 - 400,000)
    /// </summary>
    public int Frequency { get; set; }
}

/// <summary>
/// Request model for setting individual parameters
/// </summary>
public class ParameterSetRequest
{
    /// <summary>
    /// Parameter number
    /// </summary>
    public int Parameter { get; set; }

    /// <summary>
    /// Parameter value
    /// </summary>
    public double Value { get; set; }
}

/// <summary>
/// Checkpoint information
/// </summary>
public class CheckpointInfo
{
    /// <summary>
    /// Checkpoint name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Number of files in checkpoint
    /// </summary>
    public int FileCount { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public string Created { get; set; } = string.Empty;
}

/// <summary>
/// Data item information
/// </summary>
public class DataItem
{
    /// <summary>
    /// Data item name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Data item content
    /// </summary>
    public string Content { get; set; } = string.Empty;
}
