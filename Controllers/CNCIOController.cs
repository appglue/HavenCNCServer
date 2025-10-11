using Microsoft.AspNetCore.Mvc;
using HavenCNCServer.Models;
using HavenCNCServer.CentriodAPI;

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
        /// Get current input states
        /// </summary>
        [HttpGet("GetCurrentInputs")]
        public Dictionary<int, bool> GetCurrentInputs()
        {
            // TODO: Implement actual CNC input reading
            return new Dictionary<int, bool>
            {
                { 1, true },
                { 2, false },
                { 3, true }
            };
        }

        /// <summary>
        /// Get current output states
        /// </summary>
        [HttpGet("GetCurrentOutputs")]
        public Dictionary<int, bool> GetCurrentOutputs()
        {
            // TODO: Implement actual CNC output reading
            return new Dictionary<int, bool>
            {
                { 1, false },
                { 2, true },
                { 3, false }
            };
        }

        /// <summary>
        /// Check if specific input is active
        /// </summary>
        [HttpGet("IsInputActive/{inputNumber}")]
        public bool IsInputActive(int inputNumber)
        {
            if (inputNumber <= 0)
                throw new ArgumentException("Input number must be greater than 0");

            // TODO: Implement actual input check
            return false;
        }

        /// <summary>
        /// Check if specific output is active
        /// </summary>
        [HttpGet("IsOutputActive/{outputNumber}")]
        public bool IsOutputActive(int outputNumber)
        {
            if (outputNumber <= 0)
                throw new ArgumentException("Output number must be greater than 0");

            // TODO: Implement actual output check
            return false;
        }

        /// <summary>
        /// Set output state
        /// </summary>
        [HttpPost("SetOutputState")]
        public void SetOutputState([FromBody] SetOutputRequest request)
        {
            if (request.Number <= 0)
                throw new ArgumentException("Output number must be greater than 0");

            // TODO: Implement actual output setting
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
    }
}
