using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using HavenCNCServer.Models;

namespace HavenCNCServer.Controllers
{
    /// <summary>
    /// CNC IO Control - Handles inputs, outputs, vacuum, dust collection, and specialized IO
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CNCIOController : ControllerBase
    {
        #region Basic IO Control

        /// <summary>
        /// Get current input states
        /// </summary>
        /// <returns>Dictionary of input numbers and their states</returns>
        [HttpGet("GetCurrentInputs")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetCurrentInputs()
        {
            // TODO: Implement get current inputs
            await Task.Delay(1);
            var inputs = new Dictionary<int, bool>
            {
                { 1, true },
                { 2, false },
                { 3, true }
            };
            return Ok(new { inputs });
        }

        /// <summary>
        /// Get current output states
        /// </summary>
        /// <returns>Dictionary of output numbers and their states</returns>
        [HttpGet("GetCurrentOutputs")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetCurrentOutputs()
        {
            // TODO: Implement get current outputs
            await Task.Delay(1);
            var outputs = new Dictionary<int, bool>
            {
                { 1, false },
                { 2, true },
                { 3, false }
            };
            return Ok(new { outputs });
        }

        /// <summary>
        /// Check if specific input is active
        /// </summary>
        /// <param name="inputNumber">Input number to check</param>
        /// <returns>Input active status</returns>
        [HttpGet("IsInputActive/{inputNumber}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> IsInputActive(int inputNumber)
        {
            if (inputNumber <= 0)
            {
                return BadRequest("Input number must be greater than 0");
            }
            
            // TODO: Implement input active check
            await Task.Delay(1);
            return Ok(new { inputNumber, isActive = false });
        }

        /// <summary>
        /// Check if specific output is active
        /// </summary>
        /// <param name="outputNumber">Output number to check</param>
        /// <returns>Output active status</returns>
        [HttpGet("IsOutputActive/{outputNumber}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> IsOutputActive(int outputNumber)
        {
            if (outputNumber <= 0)
            {
                return BadRequest("Output number must be greater than 0");
            }
            
            // TODO: Implement output active check
            await Task.Delay(1);
            return Ok(new { outputNumber, isActive = false });
        }

        /// <summary>
        /// Set output state
        /// </summary>
        /// <param name="request">Output setting request</param>
        /// <returns>Success response</returns>
        [HttpPost("SetOutputState")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> SetOutputState([FromBody] SetOutputRequest request)
        {
            if (request?.Number <= 0)
            {
                return BadRequest("Output number must be greater than 0");
            }
            
            // TODO: Implement set output state functionality
            await Task.Delay(1);
            return Ok(new { message = $"Output {request!.Number} set to {request.Value}", outputNumber = request.Number, state = request.Value });
        }

        #endregion


        #region Testing Methods (IO Overrides)

        /// <summary>
        /// Override input value (for testing purposes only)
        /// </summary>
        /// <param name="request">Input override request</param>
        /// <returns>Success response</returns>
        [HttpPost("OverrideInput")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> OverrideInput([FromBody] OverrideIORequest request)
        {
            if (request?.Number <= 0)
            {
                return BadRequest("Input number must be greater than 0");
            }
            
            // TODO: Implement input override functionality
            await Task.Delay(1);
            return Ok(new { message = $"Input {request!.Number} overridden to {request.Value}", inputNumber = request.Number, value = request.Value });
        }

        /// <summary>
        /// Reset all input overrides
        /// </summary>
        /// <returns>Success response</returns>
        [HttpPost("ResetInputOverrides")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ResetInputOverrides()
        {
            // TODO: Implement reset input overrides functionality
            await Task.Delay(1);
            return Ok(new { message = "Input overrides reset" });
        }

        /// <summary>
        /// Override output value (for testing purposes only)
        /// </summary>
        /// <param name="request">Output override request</param>
        /// <returns>Success response</returns>
        [HttpPost("OverrideOutput")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> OverrideOutput([FromBody] OverrideIORequest request)
        {
            if (request?.Number <= 0)
            {
                return BadRequest("Output number must be greater than 0");
            }
            
            // TODO: Implement output override functionality
            await Task.Delay(1);
            return Ok(new { message = $"Output {request!.Number} overridden to {request.Value}", outputNumber = request.Number, value = request.Value });
        }

        /// <summary>
        /// Reset all output overrides
        /// </summary>
        /// <returns>Success response</returns>
        [HttpPost("ResetOutputOverrides")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ResetOutputOverrides()
        {
            // TODO: Implement reset output overrides functionality
            await Task.Delay(1);
            return Ok(new { message = "Output overrides reset" });
        }

        #endregion

        #region I/O Port Information

        /// <summary>
        /// Get available input port numbers for the current CNC system
        /// </summary>
        /// <returns>Array of available input port numbers</returns>
        [HttpGet("GetAvailableInputs")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetAvailableInputs()
        {
            try
            {
                await Task.Delay(1); // For async pattern

                var availableInputs = HavenCNCServer.CentriodAPI.CNCUtils.GetAvailableInputPorts();
                
                return Ok(new { 
                    inputs = availableInputs,
                    count = availableInputs.Length,
                    message = $"Found {availableInputs.Length} available input ports"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = $"Failed to get available inputs: {ex.Message}" 
                });
            }
        }

        /// <summary>
        /// Get available output port numbers for the current CNC system
        /// </summary>
        /// <returns>Array of available output port numbers</returns>
        [HttpGet("GetAvailableOutputs")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetAvailableOutputs()
        {
            try
            {
                await Task.Delay(1); // For async pattern

                var availableOutputs = HavenCNCServer.CentriodAPI.CNCUtils.GetAvailableOutputPorts();
                
                return Ok(new { 
                    outputs = availableOutputs,
                    count = availableOutputs.Length,
                    message = $"Found {availableOutputs.Length} available output ports"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = $"Failed to get available outputs: {ex.Message}" 
                });
            }
        }

        /// <summary>
        /// Get both available inputs and outputs with system information
        /// </summary>
        /// <returns>Complete I/O availability information</returns>
        [HttpGet("GetIOAvailability")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetIOAvailability()
        {
            try
            {
                await Task.Delay(1); // For async pattern

                var availableInputs = HavenCNCServer.CentriodAPI.CNCUtils.GetAvailableInputPorts();
                var availableOutputs = HavenCNCServer.CentriodAPI.CNCUtils.GetAvailableOutputPorts();
                var systemInfo = HavenCNCServer.CentriodAPI.CNCUtils.GetSystemInfo();
                
                var response = new IOAvailabilityResponse
                {
                    AvailableInputs = availableInputs,
                    AvailableOutputs = availableOutputs,
                    SystemInfo = systemInfo
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = $"Failed to get I/O availability: {ex.Message}" 
                });
            }
        }

        /// <summary>
        /// Check if a specific input port is available
        /// </summary>
        /// <param name="inputNumber">Input port number to check</param>
        /// <returns>Availability status</returns>
        [HttpGet("IsInputAvailable/{inputNumber}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> IsInputAvailable(int inputNumber)
        {
            try
            {
                await Task.Delay(1); // For async pattern

                var isAvailable = HavenCNCServer.CentriodAPI.CNCUtils.IsInputAvailable(inputNumber);
                
                return Ok(new { 
                    inputNumber,
                    available = isAvailable,
                    message = $"Input {inputNumber} is {(isAvailable ? "available" : "not available")}"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = $"Failed to check input availability: {ex.Message}" 
                });
            }
        }

        /// <summary>
        /// Check if a specific output port is available
        /// </summary>
        /// <param name="outputNumber">Output port number to check</param>
        /// <returns>Availability status</returns>
        [HttpGet("IsOutputAvailable/{outputNumber}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> IsOutputAvailable(int outputNumber)
        {
            try
            {
                await Task.Delay(1); // For async pattern

                var isAvailable = HavenCNCServer.CentriodAPI.CNCUtils.IsOutputAvailable(outputNumber);
                
                return Ok(new { 
                    outputNumber,
                    available = isAvailable,
                    message = $"Output {outputNumber} is {(isAvailable ? "available" : "not available")}"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = $"Failed to check output availability: {ex.Message}" 
                });
            }
        }

        /// <summary>
        /// Get comprehensive system information
        /// </summary>
        /// <returns>System information string</returns>
        [HttpGet("GetSystemInfo")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetSystemInfo()
        {
            try
            {
                await Task.Delay(1); // For async pattern

                var systemInfo = HavenCNCServer.CentriodAPI.CNCUtils.GetSystemInfo();
                
                return Ok(new { 
                    systemInfo,
                    message = "System information retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = $"Failed to get system information: {ex.Message}" 
                });
            }
        }

        /// <summary>
        /// Invert input polarity using CNC12 parameters
        /// </summary>
        /// <param name="inputNumber">Input number to invert (1-80)</param>
        /// <param name="invert">True to invert, false to restore normal polarity</param>
        /// <returns>Inversion result</returns>
        [HttpPost("InvertInput/{inputNumber}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> InvertInput(int inputNumber, [FromQuery] bool invert = true)
        {
            try
            {
                await Task.Delay(1); // For async pattern

                var result = CentroidConfigUtil.InvertInput(inputNumber, invert);

                if (result)
                {
                    return Ok(new { 
                        success = true,
                        inputNumber,
                        inverted = invert,
                        message = $"Input {inputNumber} polarity {(invert ? "inverted" : "restored to normal")}"
                    });
                }
                else
                {
                    return BadRequest(new { 
                        success = false,
                        inputNumber,
                        message = $"Failed to {(invert ? "invert" : "restore")} input {inputNumber}"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = $"Failed to invert input {inputNumber}: {ex.Message}" 
                });
            }
        }

        /// <summary>
        /// Invert multiple inputs using CNC12 parameters
        /// </summary>
        /// <param name="config">Input inversion configuration</param>
        /// <returns>Inversion result</returns>
        [HttpPost("InvertInputs")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> InvertInputs([FromBody] InputInversionConfiguration config)
        {
            try
            {
                await Task.Delay(1); // For async pattern

                var result = CentroidConfigUtil.InvertInputs(config.InputSettings);

                if (result)
                {
                    return Ok(new { 
                        success = true,
                        inputCount = config.InputSettings.Count,
                        settings = config.InputSettings,
                        message = $"Successfully configured {config.InputSettings.Count} input inversions"
                    });
                }
                else
                {
                    return BadRequest(new { 
                        success = false,
                        message = "Failed to configure input inversions"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = $"Failed to invert inputs: {ex.Message}" 
                });
            }
        }

        #endregion
    }
}
