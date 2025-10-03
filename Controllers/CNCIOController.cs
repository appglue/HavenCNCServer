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
    }
}
