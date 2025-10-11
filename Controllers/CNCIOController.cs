using Microsoft.AspNetCore.Mvc;
using HavenCNCServer.Services;
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
        private readonly ICNCIOService _ioService;

        /// <summary>
        /// Constructor for CNC IO Controller
        /// </summary>
        public CNCIOController(ICNCIOService ioService)
        {
            _ioService = ioService;
        }

        #region Basic IO Control

        /// <summary>
        /// Get available input port numbers
        /// </summary>
        [HttpGet("GetAvailableInputs")]
        public int[] GetAvailableInputs() => _ioService.GetAvailableInputs();

        /// <summary>
        /// Get available output port numbers
        /// </summary>
        [HttpGet("GetAvailableOutputs")]
        public int[] GetAvailableOutputs() => _ioService.GetAvailableOutputs();

        /// <summary>
        /// Get current input states
        /// </summary>
        [HttpGet("GetCurrentInputs")]
        public Dictionary<int, bool> GetCurrentInputs() => _ioService.GetCurrentInputs();

        /// <summary>
        /// Get current output states
        /// </summary>
        [HttpGet("GetCurrentOutputs")]
        public Dictionary<int, bool> GetCurrentOutputs() => _ioService.GetCurrentOutputs();

        /// <summary>
        /// Check if specific input is active
        /// </summary>
        [HttpGet("IsInputActive/{inputNumber}")]
        public bool IsInputActive(int inputNumber) => _ioService.IsInputActive(inputNumber);

        /// <summary>
        /// Check if specific output is active
        /// </summary>
        [HttpGet("IsOutputActive/{outputNumber}")]
        public bool IsOutputActive(int outputNumber) => _ioService.IsOutputActive(outputNumber);

        /// <summary>
        /// Set output state
        /// </summary>
        [HttpPost("SetOutputState")]
        public void SetOutputState([FromBody] SetOutputRequest request) => 
            _ioService.SetOutputState(request.Number, request.Value);

        #endregion

        #region Testing Methods (IO Overrides)

        /// <summary>
        /// Override input for testing
        /// </summary>
        [HttpPost("OverrideInput")]
        public void OverrideInput([FromBody] OverrideIORequest request) => 
            _ioService.OverrideInput(request.Number, request.Value);

        /// <summary>
        /// Override output for testing
        /// </summary>
        [HttpPost("OverrideOutput")]
        public void OverrideOutput([FromBody] OverrideIORequest request) => 
            _ioService.OverrideOutput(request.Number, request.Value);

        /// <summary>
        /// Reset all input overrides
        /// </summary>
        [HttpPost("ResetInputOverrides")]
        public void ResetInputOverrides() => _ioService.ResetInputOverrides();

        /// <summary>
        /// Reset all output overrides
        /// </summary>
        [HttpPost("ResetOutputOverrides")]
        public void ResetOutputOverrides() => _ioService.ResetOutputOverrides();

        #endregion

        #region I/O Port Information

        /// <summary>
        /// Check if input is available
        /// </summary>
        [HttpGet("IsInputAvailable/{inputNumber}")]
        public bool IsInputAvailable(int inputNumber) => _ioService.IsInputAvailable(inputNumber);

        /// <summary>
        /// Check if output is available
        /// </summary>
        [HttpGet("IsOutputAvailable/{outputNumber}")]
        public bool IsOutputAvailable(int outputNumber) => _ioService.IsOutputAvailable(outputNumber);

        /// <summary>
        /// Get system information
        /// </summary>
        [HttpGet("GetSystemInfo")]
        public string GetSystemInfo() => _ioService.GetSystemInfo();

        /// <summary>
        /// Invert input polarity
        /// </summary>
        [HttpPost("InvertInput/{inputNumber}")]
        public bool InvertInput(int inputNumber, [FromQuery] bool invert = true) => 
            _ioService.InvertInput(inputNumber, invert);

        /// <summary>
        /// Invert multiple inputs
        /// </summary>
        [HttpPost("InvertInputs")]
        public bool InvertInputs([FromBody] Dictionary<int, bool> inputSettings) => 
            _ioService.InvertInputs(inputSettings);

        #endregion
    }
}
