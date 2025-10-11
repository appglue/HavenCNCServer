using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using HavenCNCServer.Models;
using HavenCNCServer.CentriodAPI;

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
        }

        #region Data Management

        /// <summary>
        /// Get data by name
        /// </summary>
        /// <param name="name">Name of the data to retrieve</param>
        /// <returns>Data content</returns>
        [HttpGet("GetData/{name}")]
        public string GetData(string name)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, $"{name}.json");

                if (!System.IO.File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Data '{name}' not found");
                }

                return System.IO.File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
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
                    throw new ArgumentException("Data name cannot be empty");
                }

                var filePath = Path.Combine(_dataDirectory, $"{request.Name}.json");
                System.IO.File.WriteAllText(filePath, request.Content ?? string.Empty);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save data '{request.Name}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// List all available data names
        /// </summary>
        /// <returns>Array of data names</returns>
        [HttpGet("ListData")]
        public string[] ListData()
        {
            try
            {
                return Directory.GetFiles(_dataDirectory, "*.json")
                    .Select(f => Path.GetFileNameWithoutExtension(f))
                    .Where(name => !string.IsNullOrEmpty(name))
                    .OrderBy(name => name)
                    .ToArray();
            }
            catch (Exception ex)
            {
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
                var filePath = Path.Combine(_dataDirectory, $"{name}.json");

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

                    var filePath = Path.Combine(checkpointDir, $"{dataItem.Name}.json");
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
                    var name = Path.GetFileNameWithoutExtension(file);
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
                return CentroidConfigUtil.ConfigureCompleteMachine(
                    config.Inputs ?? new List<CentroidConfigUtil.IOFunction>(),
                    config.Outputs ?? new List<CentroidConfigUtil.IOFunction>(),
                    config.Axes ?? new List<CentroidConfigUtil.AxisConfiguration>(),
                    config.Spindle,
                    config.Probe,
                    config.PWMOutputs,
                    config.ATC
                );
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to configure machine: {ex.Message}", ex);
            }
        }
    }



        /// <summary>
        /// Configure only inputs and outputs in PLC file
        /// </summary>
        /// <param name="config">I/O configuration</param>
        /// <returns>Configuration result</returns>
        [HttpPost("ConfigureInputsOutputs")]
public bool ConfigureInputsOutputs([FromBody] IOConfiguration config)
{
    try
    {
        return CentroidConfigUtil.ConfigureInputsOutputs(
            config.Inputs ?? new List<CentroidConfigUtil.IOFunction>(),
            config.Outputs ?? new List<CentroidConfigUtil.IOFunction>()
        );
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException($"Failed to configure I/O: {ex.Message}", ex);
    }
}
       

        /// <summary>
        /// Configure axis settings
        /// </summary>
        /// <param name="config">Axis configuration</param>
        /// <returns>Configuration result</returns>
        [HttpPost("ConfigureAxis")]
public bool ConfigureAxis([FromBody] CentroidConfigUtil.AxisConfiguration config)
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
public bool ConfigureSpindle([FromBody] CentroidConfigUtil.SpindleConfiguration config)
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
[ProducesResponseType(200)]
[ProducesResponseType(400)]
public async Task<IActionResult> ConfigurePWM([FromBody] CentroidConfigUtil.PWMConfiguration config)
{
    try
    {
        await Task.Delay(1); // For async pattern

        var result = CentroidConfigUtil.ConfigurePWM(config);

        if (result)
        {
            return Ok(new
            {
                success = true,
                message = $"PWM output {config.OutputNumber} configured successfully"
            });
        }
        else
        {
            return BadRequest(new
            {
                success = false,
                message = $"Failed to configure PWM output {config.OutputNumber}"
            });
        }
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            success = false,
            message = $"Failed to configure PWM: {ex.Message}"
        });
    }
}

/// <summary>
/// Configure ATC settings
/// </summary>
/// <param name="config">ATC configuration</param>
/// <returns>Configuration result</returns>
[HttpPost("ConfigureATC")]
[ProducesResponseType(200)]
[ProducesResponseType(400)]
public async Task<IActionResult> ConfigureATC([FromBody] CentroidConfigUtil.ATCConfiguration config)
{
    try
    {
        await Task.Delay(1); // For async pattern

        var result = CentroidConfigUtil.ConfigureATC(config);

        if (result)
        {
            return Ok(new
            {
                success = true,
                message = $"ATC configured successfully (Type: {config.Type})"
            });
        }
        else
        {
            return BadRequest(new
            {
                success = false,
                message = "Failed to configure ATC"
            });
        }
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            success = false,
            message = $"Failed to configure ATC: {ex.Message}"
        });
    }
}

/// <summary>
/// Configure probe settings
/// </summary>
/// <param name="config">Probe configuration</param>
/// <returns>Configuration result</returns>
[HttpPost("ConfigureProbe")]
[ProducesResponseType(200)]
[ProducesResponseType(400)]
public async Task<IActionResult> ConfigureProbe([FromBody] CentroidConfigUtil.ProbeConfiguration config)
{
    try
    {
        await Task.Delay(1); // For async pattern

        var result = CentroidConfigUtil.ConfigureProbe(config);

        if (result)
        {
            return Ok(new
            {
                success = true,
                message = "Probe configured successfully"
            });
        }
        else
        {
            return BadRequest(new
            {
                success = false,
                message = "Failed to configure probe"
            });
        }
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            success = false,
            message = $"Failed to configure probe: {ex.Message}"
        });
    }
}

/// <summary>
/// Configure tool touch off settings
/// </summary>
/// <param name="config">Tool touch off configuration</param>
/// <returns>Configuration result</returns>
[HttpPost("ConfigureToolTouchOff")]
[ProducesResponseType(200)]
[ProducesResponseType(400)]
public async Task<IActionResult> ConfigureToolTouchOff([FromBody] CentroidConfigUtil.ToolTouchOffConfiguration config)
{
    try
    {
        await Task.Delay(1); // For async pattern

        var result = CentroidConfigUtil.ConfigureToolTouchOff(config);

        if (result)
        {
            return Ok(new
            {
                success = true,
                message = "Tool touch off configured successfully"
            });
        }
        else
        {
            return BadRequest(new
            {
                success = false,
                message = "Failed to configure tool touch off"
            });
        }
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            success = false,
            message = $"Failed to configure tool touch off: {ex.Message}"
        });
    }
}

/// <summary>
/// Validate I/O configuration for conflicts and issues
/// </summary>
/// <param name="config">I/O configuration to validate</param>
/// <returns>Validation results</returns>
[HttpPost("ValidateIOConfiguration")]
[ProducesResponseType(200)]
public async Task<IActionResult> ValidateIOConfiguration([FromBody] IOConfiguration config)
{
    try
    {
        await Task.Delay(1); // For async pattern

        var issues = CentroidConfigUtil.ValidateIOConfiguration(
            config.Inputs ?? new List<CentroidConfigUtil.IOFunction>(),
            config.Outputs ?? new List<CentroidConfigUtil.IOFunction>()
        );

        return Ok(new
        {
            valid = issues.Count == 0,
            issues = issues.ToArray(),
            inputCount = config.Inputs?.Count ?? 0,
            outputCount = config.Outputs?.Count ?? 0
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            message = $"Failed to validate I/O configuration: {ex.Message}"
        });
    }
}

/// <summary>
/// Validate ATC configuration for issues
/// </summary>
/// <param name="config">ATC configuration to validate</param>
/// <returns>Validation results</returns>
[HttpPost("ValidateATCConfiguration")]
[ProducesResponseType(200)]
public async Task<IActionResult> ValidateATCConfiguration([FromBody] CentroidConfigUtil.ATCConfiguration config)
{
    try
    {
        await Task.Delay(1); // For async pattern

        var issues = CentroidConfigUtil.ValidateATCConfiguration(config);

        return Ok(new
        {
            valid = issues.Count == 0,
            issues = issues.ToArray(),
            type = config.Type.ToString()
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            message = $"Failed to validate ATC configuration: {ex.Message}"
        });
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
[ProducesResponseType(200)]
[ProducesResponseType(400)]
[ProducesResponseType(500)]
public IActionResult ConfigureTouchPlate([FromBody] CentroidConfigUtil.TouchPlateConfiguration config)
{
    try
    {
        if (config == null)
        {
            return BadRequest(new { message = "Touch plate configuration is required" });
        }

        bool success = CentroidConfigUtil.ConfigureTouchPlate(config);

        if (success)
        {
            return Ok(new
            {
                message = "Touch plate configured successfully",
                config = config
            });
        }
        else
        {
            return StatusCode(500, new { message = "Failed to configure touch plate" });
        }
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            message = $"Failed to configure touch plate: {ex.Message}"
        });
    }
}

/// <summary>
/// Configure second spindle
/// </summary>
/// <param name="config">Second spindle configuration</param>
/// <returns>Configuration result</returns>
[HttpPost("ConfigureSecondSpindle")]
[ProducesResponseType(200)]
[ProducesResponseType(400)]
[ProducesResponseType(500)]
public IActionResult ConfigureSecondSpindle([FromBody] CentroidConfigUtil.SecondSpindleConfiguration config)
{
    try
    {
        if (config == null)
        {
            return BadRequest(new { message = "Second spindle configuration is required" });
        }

        bool success = CentroidConfigUtil.ConfigureSecondSpindle(config);

        if (success)
        {
            return Ok(new
            {
                message = "Second spindle configured successfully",
                config = config
            });
        }
        else
        {
            return StatusCode(500, new { message = "Failed to configure second spindle" });
        }
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            message = $"Failed to configure second spindle: {ex.Message}"
        });
    }
}

/// <summary>
/// Configure global system settings
/// </summary>
/// <param name="config">Global system configuration</param>
/// <returns>Configuration result</returns>
[HttpPost("ConfigureGlobalSystem")]
[ProducesResponseType(200)]
[ProducesResponseType(400)]
[ProducesResponseType(500)]
public IActionResult ConfigureGlobalSystem([FromBody] CentroidConfigUtil.GlobalSystemConfiguration config)
{
    try
    {
        if (config == null)
        {
            return BadRequest(new { message = "Global system configuration is required" });
        }

        bool success = CentroidConfigUtil.ConfigureGlobalSystem(config);

        if (success)
        {
            return Ok(new
            {
                message = "Global system configured successfully",
                config = config
            });
        }
        else
        {
            return StatusCode(500, new { message = "Failed to configure global system" });
        }
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            message = $"Failed to configure global system: {ex.Message}"
        });
    }
}

/// <summary>
/// Get current step frequency
/// </summary>
/// <returns>Current step frequency value</returns>
[HttpGet("GetStepFrequency")]
[ProducesResponseType(200)]
[ProducesResponseType(500)]
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
[ProducesResponseType(200)]
[ProducesResponseType(400)]
[ProducesResponseType(500)]
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
public CentroidConfigUtil.SystemHardwareInfo GetSystemHardwareInfo()
{
    try
    {
        return new CentroidConfigUtil.SystemHardwareInfo
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
[ProducesResponseType(200)]
[ProducesResponseType(400)]
[ProducesResponseType(500)]
public IActionResult GetParameter(int parameter)
{
    try
    {
        if (!Enum.IsDefined(typeof(CentroidParameters), parameter))
        {
            return BadRequest(new
            {
                message = "Invalid parameter number",
                parameter = parameter
            });
        }

        var centroidParam = (CentroidParameters)parameter;
        double value = CNCUtils.GetParameterValue(centroidParam);

        return Ok(new
        {
            parameter = parameter,
            parameterName = centroidParam.ToString(),
            value = value,
            message = $"Parameter {parameter} ({centroidParam}) = {value}"
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            message = $"Failed to get parameter {parameter}: {ex.Message}"
        });
    }
}

/// <summary>
/// Set individual parameter value
/// </summary>
/// <param name="request">Parameter set request</param>
/// <returns>Set result</returns>
[HttpPost("SetParameter")]
[ProducesResponseType(200)]
[ProducesResponseType(400)]
[ProducesResponseType(500)]
public IActionResult SetParameter([FromBody] ParameterSetRequest request)
{
    try
    {
        if (request == null)
        {
            return BadRequest(new { message = "Parameter request is required" });
        }

        if (!Enum.IsDefined(typeof(CentroidParameters), request.Parameter))
        {
            return BadRequest(new
            {
                message = "Invalid parameter number",
                parameter = request.Parameter
            });
        }

        var centroidParam = (CentroidParameters)request.Parameter;
        CNCUtils.SetParameterValue(centroidParam, request.Value);

        return Ok(new
        {
            parameter = request.Parameter,
            parameterName = centroidParam.ToString(),
            value = request.Value,
            message = $"Parameter {request.Parameter} ({centroidParam}) set to {request.Value}"
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            message = $"Failed to set parameter {request?.Parameter}: {ex.Message}"
        });
    }
}

/// <summary>
/// Get all available parameters
/// </summary>
/// <returns>List of available parameters</returns>
[HttpGet("GetAvailableParameters")]
[ProducesResponseType(200)]
[ProducesResponseType(500)]
public IActionResult GetAvailableParameters()
{
    try
    {
        var parameters = Enum.GetValues<CentroidParameters>()
            .Select(p => new
            {
                number = (int)p,
                name = p.ToString(),
                description = GetParameterDescription(p)
            })
            .OrderBy(p => p.number)
            .ToList();

        return Ok(new
        {
            message = "Available parameters retrieved successfully",
            parameterCount = parameters.Count,
            parameters = parameters
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            message = $"Failed to get available parameters: {ex.Message}"
        });
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
