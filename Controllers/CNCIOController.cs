using Microsoft.AspNetCore.Mvc;
using HavenCNCServer.Models;
using HavenCNCServer.Centroid;
using HavenCNCServer.Services;
using System.Text.RegularExpressions;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Controllers
{
    /// <summary>
    /// CNC IO Control - Handles inputs, outputs, vacuum, dust collection, and specialized IO
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CNCIOController : ControllerBase
    {
        // Track forced outputs: Key = output number, Value = force state ("ForcedOn" or "ForcedOff")
        private static readonly Dictionary<int, string> _forcedOutputs = new Dictionary<int, string>();
        private static readonly object _forcedOutputsLock = new object();

        /// <summary>
        /// Constructor for CNC IO Controller
        /// </summary>
        public CNCIOController()
        {
        }

        #region Board Configuration

        /// <summary>
        /// Get board configuration including type and expansion boards
        /// </summary>
        [HttpGet("GetBoardConfiguration")]
        public ActionResult<BoardConfiguration> GetBoardConfiguration()
        {
            try
            {
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                    return StatusCode(500, new { message = "CNC connection not available" });

                // Try to determine board type from system info
                var systemInfo = CNCUtils.GetSystemInfo();
                var boardType = "Acorn"; // Default
                var hasExtensionBoard = false;

                // Check for Acorn6 indicators in system info
                if (systemInfo.Contains("Acorn 6", StringComparison.OrdinalIgnoreCase) ||
                    systemInfo.Contains("AcornSix", StringComparison.OrdinalIgnoreCase))
                {
                    boardType = "Acorn6";
                }

                // TODO: Detect extension board - could check parameter or I/O availability
                // For now, assume no extension board unless we can detect it
                // This could be enhanced by checking if inputs > 16 are accessible

                var config = new BoardConfiguration
                {
                    BoardType = boardType,
                    HasExtensionBoard = hasExtensionBoard,
                    MaxInputs = boardType == "Acorn6" ? (hasExtensionBoard ? 32 : 24) : (hasExtensionBoard ? 24 : 16),
                    MaxOutputs = boardType == "Acorn6" ? (hasExtensionBoard ? 32 : 24) : (hasExtensionBoard ? 24 : 16)
                };

                return Ok(config);
            }
            catch (Exception ex)
            {
                LogError($"Failed to get board configuration: {ex.Message}", "IO");
                return StatusCode(500, new { message = $"Failed to get board configuration: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get available input port numbers based on board hardware capabilities
        /// </summary>
        [HttpGet("GetAvailableInputs")]
        public ActionResult<int[]> GetAvailableInputs()
        {
            try
            {
                var boardConfigResult = GetBoardConfiguration();
                if (boardConfigResult.Result is OkObjectResult okResult && okResult.Value is BoardConfiguration config)
                {
                    // Generate array of available input numbers: 1 to MaxInputs
                    var inputs = Enumerable.Range(1, config.MaxInputs).ToArray();
                    return Ok(inputs);
                }

                // Fallback to default Acorn (16 inputs)
                LogWarning("Could not determine board configuration, using default Acorn (16 inputs)", "IO");
                return Ok(Enumerable.Range(1, 16).ToArray());
            }
            catch (Exception ex)
            {
                LogError($"Failed to get available inputs: {ex.Message}", "IO");
                return StatusCode(500, new { message = $"Failed to get available inputs: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get available output port numbers based on board hardware capabilities
        /// </summary>
        [HttpGet("GetAvailableOutputs")]
        public ActionResult<int[]> GetAvailableOutputs()
        {
            try
            {
                var boardConfigResult = GetBoardConfiguration();
                if (boardConfigResult.Result is OkObjectResult okResult && okResult.Value is BoardConfiguration config)
                {
                    // Generate array of available output numbers: 1 to MaxOutputs
                    var outputs = Enumerable.Range(1, config.MaxOutputs).ToArray();
                    return Ok(outputs);
                }

                // Fallback to default Acorn (16 outputs)
                LogWarning("Could not determine board configuration, using default Acorn (16 outputs)", "IO");
                return Ok(Enumerable.Range(1, 16).ToArray());
            }
            catch (Exception ex)
            {
                LogError($"Failed to get available outputs: {ex.Message}", "IO");
                return StatusCode(500, new { message = $"Failed to get available outputs: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get input states for specified input numbers
        /// </summary>
        /// <param name="inputNumbers">Array of input numbers to query</param>
        /// <returns>Dictionary mapping input number to its state (true = on, false = off)</returns>
        [HttpPost("GetInputState")]
        [ProducesResponseType(typeof(Dictionary<int, bool>), 200)]
        public ActionResult<Dictionary<int, bool>> GetInputState([FromBody] int[] inputNumbers)
        {
            try
            {
                if (inputNumbers == null || inputNumbers.Length == 0)
                {
                    return Ok(new Dictionary<int, bool>());
                }

                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                {
                    LogError("CNC connection not available", "IO");
                    return StatusCode(500, new { message = "CNC connection not available" });
                }

                var result = new Dictionary<int, bool>();

                foreach (var inputNumber in inputNumbers)
                {
                    bool state = false;
                    var getStateResult = cncPipe.plc.GetInputState(inputNumber, out CentroidAPI.CNCPipe.Plc.IOState ioState);
                    if (getStateResult == CentroidAPI.CNCPipe.ReturnCode.SUCCESS)
                    {
                        state = (ioState == CentroidAPI.CNCPipe.Plc.IOState.IO_LOGICAL_1);
                    }

                    result[inputNumber] = state;
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                LogError($"Failed to get input states: {ex.Message}", "IO");
                return StatusCode(500, new { message = $"Failed to get input states: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get output states for specified output numbers
        /// </summary>
        /// <param name="outputNumbers">Array of output numbers to query</param>
        /// <returns>Dictionary mapping output number to its state (true = on, false = off)</returns>
        [HttpPost("GetOutputState")]
        [ProducesResponseType(typeof(Dictionary<int, bool>), 200)]
        public ActionResult<Dictionary<int, bool>> GetOutputState([FromBody] int[] outputNumbers)
        {
            try
            {
                if (outputNumbers == null || outputNumbers.Length == 0)
                {
                    return Ok(new Dictionary<int, bool>());
                }

                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                {
                    LogError("CNC connection not available", "IO");
                    return StatusCode(500, new { message = "CNC connection not available" });
                }

                var result = new Dictionary<int, bool>();

                foreach (var outputNumber in outputNumbers)
                {
                    bool state = false;
                    var getStateResult = cncPipe.plc.GetOutputState(outputNumber, out CentroidAPI.CNCPipe.Plc.IOState ioState);
                    if (getStateResult == CentroidAPI.CNCPipe.ReturnCode.SUCCESS)
                    {
                        state = (ioState == CentroidAPI.CNCPipe.Plc.IOState.IO_LOGICAL_1);
                    }

                    result[outputNumber] = state;
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                LogError($"Failed to get output states: {ex.Message}", "IO");
                return StatusCode(500, new { message = $"Failed to get output states: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get the state of the first 100 inputs and 100 outputs
        /// Returns number and on/off state for each I/O point
        /// </summary>
        [HttpGet("GetBulkIOStates")]
        [ProducesResponseType(typeof(BulkIOStatesResponse), 200)]
        public ActionResult<BulkIOStatesResponse> GetBulkIOStates()
        {
            try
            {
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                {
                    LogError("CNC connection not available", "IO");
                    return StatusCode(500, new { message = "CNC connection not available" });
                }

                var response = new BulkIOStatesResponse
                {
                    ReadAt = DateTime.UtcNow
                };

                // Read first 100 inputs
                for (int i = 1; i <= 100; i++)
                {
                    try
                    {
                        var result = cncPipe.plc.GetInputState(i, out CentroidAPI.CNCPipe.Plc.IOState state);
                        bool isOn = false;

                        if (result == CentroidAPI.CNCPipe.ReturnCode.SUCCESS)
                        {
                            isOn = (state == CentroidAPI.CNCPipe.Plc.IOState.IO_LOGICAL_1);
                        }

                        response.Inputs.Add(new Models.IOState
                        {
                            Number = i,
                            IsOn = isOn
                        });
                    }
                    catch (Exception ex)
                    {
                        LogWarning($"Failed to read input {i}: {ex.Message}", "IO");
                        response.Inputs.Add(new Models.IOState { Number = i, IsOn = false });
                    }
                }

                // Read first 100 outputs
                for (int i = 1; i <= 100; i++)
                {
                    try
                    {
                        var result = cncPipe.plc.GetOutputState(i, out CentroidAPI.CNCPipe.Plc.IOState state);
                        bool isOn = false;

                        if (result == CentroidAPI.CNCPipe.ReturnCode.SUCCESS)
                        {
                            isOn = (state == CentroidAPI.CNCPipe.Plc.IOState.IO_LOGICAL_1);
                        }

                        response.Outputs.Add(new Models.IOState
                        {
                            Number = i,
                            IsOn = isOn
                        });
                    }
                    catch (Exception ex)
                    {
                        LogWarning($"Failed to read output {i}: {ex.Message}", "IO");
                        response.Outputs.Add(new Models.IOState { Number = i, IsOn = false });
                    }
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                LogError($"Failed to get bulk I/O states: {ex.Message}", "IO");
                return StatusCode(500, new { message = $"Failed to get bulk I/O states: {ex.Message}" });
            }
        }

        #endregion

        #region Basic IO Control

        /// <summary>
        /// Check if specific input is active
        /// </summary>
        [HttpGet("IsInputActive/{inputNumber}")]
        public bool IsInputActive(int inputNumber)
        {
            if (inputNumber <= 0)
                throw new ArgumentException("Input number must be greater than 0");

            try
            {
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                    throw new InvalidOperationException("CNC connection not available");

                var result = cncPipe.plc.GetInputState(inputNumber, out CentroidAPI.CNCPipe.Plc.IOState state);
                if (result != CentroidAPI.CNCPipe.ReturnCode.SUCCESS)
                    throw new InvalidOperationException($"Failed to read input {inputNumber}: {result}");

                return (state == CentroidAPI.CNCPipe.Plc.IOState.IO_LOGICAL_1);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to check input {inputNumber}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Check if specific output is active
        /// </summary>
        [HttpGet("IsOutputActive/{outputNumber}")]
        public bool IsOutputActive(int outputNumber)
        {
            if (outputNumber <= 0)
                throw new ArgumentException("Output number must be greater than 0");

            try
            {
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                    throw new InvalidOperationException("CNC connection not available");

                var result = cncPipe.plc.GetOutputState(outputNumber, out CentroidAPI.CNCPipe.Plc.IOState state);
                if (result != CentroidAPI.CNCPipe.ReturnCode.SUCCESS)
                    throw new InvalidOperationException($"Failed to read output {outputNumber}: {result}");

                return (state == CentroidAPI.CNCPipe.Plc.IOState.IO_LOGICAL_1);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to check output {outputNumber}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get forced outputs from a list of output numbers
        /// </summary>
        /// <param name="outputNumbers">Array of output numbers to check for forced state</param>
        /// <returns>Dictionary mapping output number to force state string ("ForcedOn" or "ForcedOff")</returns>
        [HttpPost("GetForcedOutputs")]
        [ProducesResponseType(typeof(Dictionary<int, string>), 200)]
        public ActionResult<Dictionary<int, string>> GetForcedOutputs([FromBody] int[] outputNumbers)
        {
            if (outputNumbers == null)
            {
                return BadRequest(new { message = "Output numbers array is required" });
            }

            lock (_forcedOutputsLock)
            {
                // Filter forced outputs to only those in the requested list
                var result = new Dictionary<int, string>();
                foreach (var outputNumber in outputNumbers)
                {
                    if (_forcedOutputs.TryGetValue(outputNumber, out var forceState))
                    {
                        result[outputNumber] = forceState;
                    }
                }

                return Ok(result);
            }
        }

        /// <summary>
        /// Set output state and broadcast the change via SignalR
        /// </summary>
        [HttpPost("SetOutputState")]
        public void SetOutputState([FromBody] SetOutputRequest request)
        {
            if (request.Number <= 0)
                throw new ArgumentException("Output number must be greater than 0");

            try
            {
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                    throw new InvalidOperationException("CNC connection not available");

                // Use SetIoForceState to control the output
                var forceState = request.Value ?
                    CentroidAPI.CNCPipe.Plc.ForceState.ForcedOn :
                    CentroidAPI.CNCPipe.Plc.ForceState.ForcedOff;

                var result = cncPipe.plc.SetIoForceState(
                    request.Number,
                    CentroidAPI.CNCPipe.Plc.BitType.Output,
                    forceState);

                if (result != CentroidAPI.CNCPipe.ReturnCode.SUCCESS)
                    throw new InvalidOperationException($"Failed to set output {request.Number}: {result}");

                // Track the forced state
                lock (_forcedOutputsLock)
                {
                    _forcedOutputs[request.Number] = forceState.ToString();
                }

                // Broadcast output state change via SignalR
                _ = SignalRManager.BroadcastOutputStateChanged(request.Number, request.Value, forceState.ToString());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to set output {request.Number} to {request.Value}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Reset output to normal (remove forced state)
        /// </summary>
        [HttpPost("ResetOutput/{outputNumber}")]
        public void ResetOutput(int outputNumber)
        {
            if (outputNumber <= 0)
                throw new ArgumentException("Output number must be greater than 0");

            try
            {
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                    throw new InvalidOperationException("CNC connection not available");

                // Reset to normal (remove forced state)
                var result = cncPipe.plc.SetIoForceState(
                    outputNumber,
                    CentroidAPI.CNCPipe.Plc.BitType.Output,
                    CentroidAPI.CNCPipe.Plc.ForceState.NotForced);

                if (result != CentroidAPI.CNCPipe.ReturnCode.SUCCESS)
                    throw new InvalidOperationException($"Failed to reset output {outputNumber}: {result}");

                // Remove from tracking
                lock (_forcedOutputsLock)
                {
                    _forcedOutputs.Remove(outputNumber);
                }

                // Broadcast the reset via SignalR
                _ = SignalRManager.BroadcastOutputStateChanged(outputNumber, false, "NotForced");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to reset output {outputNumber}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Reset all outputs to normal (remove all forced states)
        /// </summary>
        [HttpPost("ResetAllOutputs")]
        public void ResetAllOutputs()
        {
            try
            {
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                    throw new InvalidOperationException("CNC connection not available");

                var availableOutputsResult = GetAvailableOutputs();
                int[] availableOutputs;

                if (availableOutputsResult.Result is OkObjectResult okResult && okResult.Value is int[] outputs)
                {
                    availableOutputs = outputs;
                }
                else
                {
                    // Fallback to default range
                    availableOutputs = Enumerable.Range(1, 16).ToArray();
                }

                var errors = new List<string>();

                foreach (var outputNumber in availableOutputs)
                {
                    try
                    {
                        var result = cncPipe.plc.SetIoForceState(
                            outputNumber,
                            CentroidAPI.CNCPipe.Plc.BitType.Output,
                            CentroidAPI.CNCPipe.Plc.ForceState.NotForced);

                        if (result != CentroidAPI.CNCPipe.ReturnCode.SUCCESS)
                        {
                            errors.Add($"Output {outputNumber}: {result}");
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Output {outputNumber}: {ex.Message}");
                    }
                }

                if (errors.Count > 0)
                {
                    throw new InvalidOperationException($"Failed to reset some outputs: {string.Join(", ", errors)}");
                }

                // Clear all tracked forced outputs
                lock (_forcedOutputsLock)
                {
                    _forcedOutputs.Clear();
                }
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                throw new InvalidOperationException($"Failed to reset all outputs: {ex.Message}", ex);
            }
        }

        #endregion

        #region I/O Port Information

        /// <summary>
        /// Check if input is available
        /// </summary>
        [HttpGet("IsInputAvailable/{inputNumber}")]
        public bool IsInputAvailable(int inputNumber)
        {
            try
            {
                return CNCUtils.IsInputAvailable(inputNumber);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to check input availability: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Check if output is available
        /// </summary>
        [HttpGet("IsOutputAvailable/{outputNumber}")]
        public bool IsOutputAvailable(int outputNumber)
        {
            try
            {
                return CNCUtils.IsOutputAvailable(outputNumber);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to check output availability: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get system information
        /// </summary>
        [HttpGet("GetSystemInfo")]
        [ProducesResponseType(typeof(SystemInfo), 200)]
        public ActionResult<SystemInfo> GetSystemInfo()
        {
            try
            {
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                {
                    LogError("CNC connection not available", "IO");
                    return StatusCode(500, new { message = "CNC connection not available" });
                }

                cncPipe.system.GetUnlockVersion(out CentroidAPI.CNCPipe.Sys.UnlockVersions unlockVersion);

                var systemInfo = new SystemInfo
                {
                    UnlockVersion = unlockVersion.ToString()
                };

                bool isAcorn = unlockVersion.ToString().Contains("ACORN") && !unlockVersion.ToString().Contains("ACORN_SIX");
                bool isAcornSix = unlockVersion.ToString().Contains("ACORN_SIX");
                bool isHickory = unlockVersion.ToString().Contains("HICKORY");

                if (isAcorn)
                {
                    systemInfo.SystemType = SystemType.Acorn;
                    systemInfo.BaseInputs = 8;
                    systemInfo.BaseOutputs = 8;
                    cncPipe.system.GetEther1616DeviceInfo(out List<CentroidAPI.CNCPipe.Sys.Ether1616Device> devices);
                    systemInfo.ExpansionBoardCount = devices?.Count ?? 0;
                    systemInfo.ExpansionInputs = systemInfo.ExpansionBoardCount * 16;
                    systemInfo.ExpansionOutputs = systemInfo.ExpansionBoardCount * 16;
                }
                else if (isAcornSix)
                {
                    systemInfo.SystemType = SystemType.AcornSix;
                    systemInfo.BaseInputs = 16;
                    systemInfo.BaseOutputs = 16;
                    cncPipe.system.GetPLCEXP1616NumberofDevices(out int expansionCount);
                    systemInfo.ExpansionBoardCount = expansionCount;
                    systemInfo.ExpansionInputs = expansionCount * 16;
                    systemInfo.ExpansionOutputs = expansionCount * 16;
                }
                else if (isHickory)
                {
                    systemInfo.SystemType = SystemType.Hickory;
                    systemInfo.BaseInputs = 32;
                    systemInfo.BaseOutputs = 32;
                    cncPipe.system.GetECAT1616NumberOfDevices(out int expansionCount);
                    systemInfo.ExpansionBoardCount = expansionCount;
                    systemInfo.ExpansionInputs = expansionCount * 16;
                    systemInfo.ExpansionOutputs = expansionCount * 16;
                }

                systemInfo.TotalInputs = systemInfo.BaseInputs + systemInfo.ExpansionInputs;
                systemInfo.TotalOutputs = systemInfo.BaseOutputs + systemInfo.ExpansionOutputs;

                return Ok(systemInfo);
            }
            catch (Exception ex)
            {
                LogError($"Failed to get system info: {ex.Message}", "IO");
                return StatusCode(500, new { message = $"Failed to get system info: {ex.Message}" });
            }
        }

        #endregion

        #region I/O Definitions

        /// <summary>
        /// Get I/O definitions from the PLC source file
        /// </summary>
        /// <param name="sourcePath">Optional path to the PLC source file. If not provided, uses C:\cncr\acorn_router_plc.src</param>
        /// <returns>IODefinitionsResponse containing all input and output definitions</returns>
        [HttpGet("GetIODefinitions")]
        public ActionResult<IODefinitionsResponse> GetIODefinitions([FromQuery] string? sourcePath = null)
        {
            try
            {
                // Default to C:\cncr directory (where CNC12 is running from)
                if (string.IsNullOrEmpty(sourcePath))
                {
                    sourcePath = @"C:\cncr\acorn_router_plc.src";
                }

                if (!System.IO.File.Exists(sourcePath))
                {
                    return NotFound(new { error = "PLC source file not found", path = sourcePath });
                }

                var response = ParseIODefinitions(sourcePath);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to parse I/O definitions: {ex.Message}" });
            }
        }

        /// <summary>
        /// Parse I/O definitions from a PLC source file
        /// Reads definitions from INPUT DEFINITIONS and Output Definitions sections only,
        /// stopping immediately after ; #endregion marker
        /// </summary>
        private IODefinitionsResponse ParseIODefinitions(string filePath)
        {
            var response = new IODefinitionsResponse
            {
                SourceFilePath = filePath,
                ParsedAt = DateTime.UtcNow
            };

            // Regex patterns to match input and output definitions
            // Format: Name IS INP<number> or Name IS OUT<number>
            var inputPattern = new Regex(@"^([A-Za-z0-9_]+)\s+IS\s+INP(\d+)", RegexOptions.Compiled);
            var outputPattern = new Regex(@"^([A-Za-z0-9_]+)\s+IS\s+OUT(\d+)", RegexOptions.Compiled);

            var lines = System.IO.File.ReadAllLines(filePath);
            bool inInputSection = false;
            bool inOutputSection = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // Detect section start
                if (trimmedLine.Contains("INPUT DEFINITIONS"))
                {
                    inInputSection = true;
                    inOutputSection = false;
                    continue;
                }
                else if (trimmedLine.Contains("Output Definitions") || trimmedLine.Contains("OUTPUT DEFINITIONS"))
                {
                    inOutputSection = true;
                    inInputSection = false;
                    continue;
                }

                // Stop reading section immediately when we see #endregion
                if (trimmedLine == "; #endregion")
                {
                    inInputSection = false;
                    inOutputSection = false;
                    continue;
                }

                // Skip comments and empty lines
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";"))
                    continue;

                // Parse input definitions when in input section
                if (inInputSection)
                {
                    var inputMatch = inputPattern.Match(trimmedLine);
                    if (inputMatch.Success)
                    {
                        int ioNumber = int.Parse(inputMatch.Groups[2].Value);
                        response.Inputs.Add(new IODefinition
                        {
                            Name = inputMatch.Groups[1].Value,
                            Number = ioNumber,
                            Type = "INPUT",
                            RawDefinition = line
                        });
                    }
                }

                // Parse output definitions when in output section
                if (inOutputSection)
                {
                    var outputMatch = outputPattern.Match(trimmedLine);
                    if (outputMatch.Success)
                    {
                        int ioNumber = int.Parse(outputMatch.Groups[2].Value);
                        response.Outputs.Add(new IODefinition
                        {
                            Name = outputMatch.Groups[1].Value,
                            Number = ioNumber,
                            Type = "OUTPUT",
                            RawDefinition = line
                        });
                    }
                }
            }

            // Sort by number for easier reading
            response.Inputs = response.Inputs.OrderBy(i => i.Number).ToList();
            response.Outputs = response.Outputs.OrderBy(o => o.Number).ToList();

            return response;
        }

        #endregion
    }
}
