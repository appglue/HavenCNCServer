using Microsoft.AspNetCore.Mvc;
using HavenCNCServer.Models;
using HavenCNCServer.Centroid;
using HavenCNCServer.Services;
using System.Text.RegularExpressions;

namespace HavenCNCServer.Controllers
{
    /// <summary>
    /// CNC IO Control - Handles inputs, outputs, vacuum, dust collection, and specialized IO
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CNCIOController : ControllerBase
    {
        /// <summary>
        /// Constructor for CNC IO Controller
        /// </summary>
        public CNCIOController()
        {
        }

        #region Basic IO Control

        /// <summary>
        /// Get available input port numbers
        /// </summary>
        [HttpGet("GetAvailableInputs")]
        public int[] GetAvailableInputs()
        {
            try
            {
                return CNCUtils.GetAvailableInputPorts();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get available inputs: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get available output port numbers
        /// </summary>
        [HttpGet("GetAvailableOutputs")]
        public int[] GetAvailableOutputs()
        {
            try
            {
                return CNCUtils.GetAvailableOutputPorts();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get available outputs: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get current input states for all available inputs
        /// </summary>
        [HttpGet("GetCurrentInputs")]
        public Dictionary<int, bool> GetCurrentInputs()
        {
            try
            {
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                    throw new InvalidOperationException("CNC connection not available");

                var inputStates = new Dictionary<int, bool>();
                var availableInputs = CNCUtils.GetAvailableInputPorts();

                foreach (var inputNumber in availableInputs)
                {
                    var result = cncPipe.plc.GetInputState(inputNumber, out CentroidAPI.CNCPipe.Plc.IOState state);
                    if (result == CentroidAPI.CNCPipe.ReturnCode.SUCCESS)
                    {
                        inputStates[inputNumber] = (state == CentroidAPI.CNCPipe.Plc.IOState.IO_LOGICAL_1);
                    }
                }

                return inputStates;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get input states: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get current output states for all available outputs
        /// </summary>
        [HttpGet("GetCurrentOutputs")]
        public Dictionary<int, bool> GetCurrentOutputs()
        {
            try
            {
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                    throw new InvalidOperationException("CNC connection not available");

                var outputStates = new Dictionary<int, bool>();
                var availableOutputs = CNCUtils.GetAvailableOutputPorts();

                foreach (var outputNumber in availableOutputs)
                {
                    var result = cncPipe.plc.GetOutputState(outputNumber, out CentroidAPI.CNCPipe.Plc.IOState state);
                    if (result == CentroidAPI.CNCPipe.ReturnCode.SUCCESS)
                    {
                        outputStates[outputNumber] = (state == CentroidAPI.CNCPipe.Plc.IOState.IO_LOGICAL_1);
                    }
                }

                return outputStates;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get output states: {ex.Message}", ex);
            }
        }

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
        /// Set output state
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
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to set output {request.Number} to {request.Value}: {ex.Message}", ex);
            }
        }

        #endregion

        #region Testing Methods (IO Overrides)

        /// <summary>
        /// Override input for testing
        /// </summary>
        [HttpPost("OverrideInput")]
        public void OverrideInput([FromBody] OverrideIORequest request)
        {
            if (request.Number <= 0)
                throw new ArgumentException("Input number must be greater than 0");

            // TODO: Implement input override
        }

        /// <summary>
        /// Override output for testing
        /// </summary>
        [HttpPost("OverrideOutput")]
        public void OverrideOutput([FromBody] OverrideIORequest request)
        {
            if (request.Number <= 0)
                throw new ArgumentException("Output number must be greater than 0");

            // TODO: Implement output override
        }

        /// <summary>
        /// Reset all input overrides
        /// </summary>
        [HttpPost("ResetInputOverrides")]
        public void ResetInputOverrides()
        {
            // TODO: Implement reset input overrides
        }

        /// <summary>
        /// Reset all output overrides
        /// </summary>
        [HttpPost("ResetOutputOverrides")]
        public void ResetOutputOverrides()
        {
            // TODO: Implement reset output overrides
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
        public string GetSystemInfo()
        {
            try
            {
                return CNCUtils.GetSystemInfo();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get system info: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Invert input polarity
        /// </summary>
        [HttpPost("InvertInput/{inputNumber}")]
        public bool InvertInput(int inputNumber, [FromQuery] bool invert = true)
        {
            try
            {
                // TODO: Fix reference to CentroidConfigUtil
                // return CentroidConfigUtil.InvertInput(inputNumber, invert);
                return true; // Placeholder
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to invert input: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Invert multiple inputs
        /// </summary>
        [HttpPost("InvertInputs")]
        public bool InvertInputs([FromBody] Dictionary<int, bool> inputSettings)
        {
            try
            {
                // TODO: Fix reference to CentroidConfigUtil
                // return CentroidConfigUtil.InvertInputs(inputSettings);
                return true; // Placeholder
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to invert inputs: {ex.Message}", ex);
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
