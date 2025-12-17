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
            _dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "data");
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

                var filePath = Path.Combine(_dataDirectory, name);
                Services.LoggingService.LogDebug($"Looking for file at: {filePath}", "Config");

                if (!System.IO.File.Exists(filePath))
                {
                    Services.LoggingService.LogWarning($"❌ Data '{name}' not found at: {filePath}", "Config");
                    Services.LoggingService.LogDebug($"File.Exists returned false for: {filePath}", "Config");
                    return null; // Return null instead of throwing error
                }

                Services.LoggingService.LogDebug($"File exists, reading content from: {filePath}", "Config");
                var content = System.IO.File.ReadAllText(filePath);
                Services.LoggingService.LogSuccess($"✓ GetData '{name}' returned {content.Length} characters from {filePath}", "Config");
                return content;
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
        public void SetData([FromBody] ConfigurationDataRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    Services.LoggingService.LogError("SetData request with empty name", "Config");
                    throw new ArgumentException("Data name cannot be empty");
                }

                Services.LoggingService.LogInfo($"💾 SetData request: '{request.Name}' ({(request.Content?.Length ?? 0)} chars)", "Config");

                var filePath = Path.Combine(_dataDirectory, request.Name);

                // Create backup before saving
                Services.BackupService.CreateBackup(filePath);

                System.IO.File.WriteAllText(filePath, request.Content ?? string.Empty);

                Services.LoggingService.LogSuccess($"✓ SetData '{request.Name}' saved to: {filePath}", "Config");
            }
            catch (Exception ex)
            {
                Services.LoggingService.LogError($"Failed to save data '{request.Name}': {ex.Message}", "Config");
                throw new InvalidOperationException($"Failed to save data '{request.Name}': {ex.Message}", ex);
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
                Services.LoggingService.LogInfo($"📋 ListData request - checking directory: {_dataDirectory}", "Config");

                var files = Directory.GetFiles(_dataDirectory, "*.json")
                    .Select(f => Path.GetFileName(f))  // Get full filename with .json extension
                    .Where(name => !string.IsNullOrEmpty(name))
                    .OrderBy(name => name)
                    .ToArray();

                Services.LoggingService.LogInfo($"📋 ListData returning {files.Length} files: [{string.Join(", ", files)}]", "Config");

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
                LoggingService.Log($"Configuration contains: {config.Axes?.Count ?? 0} axes, Spindle: {config.Spindle != null}, Probe: {config.Probe != null}, PWM: {config.PWMOutputs?.Count ?? 0}, ATC: {config.ATC != null}");

                var result = CentroidConfigUtil.ConfigureCompleteMachine(
                    config.Axes ?? new List<AxisConfiguration>(),
                    config.Spindle!,
                    config.Probe,
                    config.PWMOutputs,
                    config.ATC
                );

                if (result)
                {
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
        /// Get travel limits for all configured axes
        /// </summary>
        /// <returns>Travel limits for each axis with plus and minus limits</returns>
        [HttpGet("GetTravelLimits")]
        public TravelLimitsResponse GetTravelLimits()
        {
            try
            {
                var response = new TravelLimitsResponse();
                var axisLimits = new List<AxisTravelLimits>();

                // Get number of axes configured (typically check parameters or iterate through common axes)
                // For now, we'll check axes 1-8 and include those that have valid data
                for (int axisNumber = 1; axisNumber <= 8; axisNumber++)
                {
                    if (CNCUtils.GetAxisTravelLimits(axisNumber, out double plusLimit, out double minusLimit))
                    {
                        // Get the axis label (X, Y, Z, A, etc.)
                        string axisLabel = CNCUtils.GetAxisLabel(axisNumber);

                        // Only include axes that have a valid label (configured axes)
                        if (!string.IsNullOrEmpty(axisLabel))
                        {
                            axisLimits.Add(new AxisTravelLimits
                            {
                                AxisNumber = axisNumber,
                                AxisLabel = axisLabel,
                                PlusLimit = plusLimit,
                                MinusLimit = minusLimit
                            });
                        }
                    }
                }

                response.Axes = axisLimits;
                response.Message = $"Retrieved travel limits for {axisLimits.Count} configured axes";

                return response;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get travel limits: {ex.Message}", ex);
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
        /// Configure ATC settings
        /// </summary>
        /// <param name="config">ATC configuration</param>
        /// <returns>Configuration result</returns>
        [HttpPost("ConfigureATC")]
        public bool ConfigureATC([FromBody] ATCConfiguration config)
        {
            try
            {
                return CentroidConfigUtil.ConfigureATC(config);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to configure ATC: {ex.Message}", ex);
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

        /// <summary>
        /// Validate ATC configuration for issues
        /// </summary>
        /// <param name="config">ATC configuration to validate</param>
        /// <returns>Validation results</returns>
        [HttpPost("ValidateATCConfiguration")]
        public ATCValidationResult ValidateATCConfiguration([FromBody] ATCConfiguration config)
        {
            try
            {
                var issues = CentroidConfigUtil.ValidateATCConfiguration(config);

                return new ATCValidationResult
                {
                    Valid = issues.Count == 0,
                    Issues = issues.ToArray(),
                    Type = config.Type.ToString()
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to validate ATC configuration: {ex.Message}", ex);
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

                // Validate frequency range (100kHz to 400kHz)
                if (request.Frequency < 100000 || request.Frequency > 400000)
                {
                    throw new ArgumentOutOfRangeException(nameof(request.Frequency),
                        "Step frequency must be between 100,000 and 400,000 Hz");
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
