using Microsoft.AspNetCore.Mvc;
using HavenCNCServer.Services;

namespace HavenCNCServer.Controllers
{
    /// <summary>
    /// CNC System Control - Handles system operations, homing, errors, and machine state
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CNCSystemController : ControllerBase
    {
        private readonly ICNCSystemService _systemService;

        /// <summary>
        /// Constructor for CNC System Controller
        /// </summary>
        public CNCSystemController(ICNCSystemService systemService)
        {
            _systemService = systemService;
        }

        #region System Control

        /// <summary>
        /// Enter full screen mode
        /// </summary>
        [HttpPost("EnterFullScreen")]
        public async Task<bool> EnterFullScreen() => await _systemService.EnterFullScreenAsync();

        /// <summary>
        /// Exit full screen mode
        /// </summary>
        [HttpPost("ExitFullScreen")]
        public async Task<bool> ExitFullScreen() => await _systemService.ExitFullScreenAsync();

        /// <summary>
        /// Get current full screen state
        /// </summary>
        [HttpGet("GetFullScreenState")]
        public bool GetFullScreenState() => _systemService.IsFullScreen;

        /// <summary>
        /// Shutdown the system
        /// </summary>
        [HttpPost("Shutdown")]
        public void Shutdown() => _systemService.Shutdown();

        /// <summary>
        /// Restart Centroid system
        /// </summary>
        [HttpPost("RestartCentroid")]
        public void RestartCentroid() => _systemService.RestartCentroid();

        #endregion

        #region Machine State Control

        /// <summary>
        /// Emergency stop
        /// </summary>
        [HttpPost("EmergencyStop")]
        public void EmergencyStop() => _systemService.EmergencyStop();

        /// <summary>
        /// Check if machine is homed
        /// </summary>
        [HttpGet("IsHomed")]
        public bool IsHomed() => _systemService.IsHomed();

        /// <summary>
        /// Unhome the machine
        /// </summary>
        [HttpPost("UnhomeMachine")]
        public void UnhomeMachine() => _systemService.UnhomeMachine();

        /// <summary>
        /// Home the machine
        /// </summary>
        [HttpPost("HomeMachine")]
        public void HomeMachine() => _systemService.HomeMachine();

        /// <summary>
        /// Get current error state
        /// </summary>
        [HttpGet("GetCurrentErrorState")]
        public string[]? GetCurrentErrorState() => _systemService.GetCurrentErrorState();

        /// <summary>
        /// Reset error state
        /// </summary>
        [HttpPost("ResetErrorState")]
        public void ResetErrorState() => _systemService.ResetErrorState();

        /// <summary>
        /// Reset the machine
        /// </summary>
        [HttpPost("ResetMachine")]
        public void ResetMachine() => _systemService.ResetMachine();

        /// <summary>
        /// Check if machine has current errors
        /// </summary>
        [HttpGet("HasCurrentErrors")]
        public bool HasCurrentErrors() => _systemService.HasCurrentErrors();

        #endregion
    }
}
