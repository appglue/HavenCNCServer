using Microsoft.AspNetCore.Mvc;
using HavenCNCServer.Services;

namespace HavenCNCServer.Controllers
{
    /// <summary>
    /// API controller for CNC server management
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CNCServerController : ControllerBase
    {
        /// <summary>
        /// Initialize the CNC Server Controller
        /// </summary>
        public CNCServerController()
        {
        }

        /// <summary>
        /// Get the current CNC server status
        /// </summary>
        /// <returns>Server status information</returns>
        [HttpGet("status")]
        [ProducesResponseType(typeof(CNCServerStatus), 200)]
        public ActionResult<CNCServerStatus> GetServerStatus()
        {
            return Ok(new CNCServerStatus
            {
                IsRunning = CNCServerManager.IsServerRunning,
                CanStart = !CNCServerManager.IsServerRunning,
                CanStop = CNCServerManager.IsServerRunning
            });
        }

        /// <summary>
        /// Start the CNC server
        /// </summary>
        /// <returns>Success status</returns>
        [HttpPost("start")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<ActionResult<bool>> StartServer()
        {
            if (CNCServerManager.IsServerRunning)
            {
                return BadRequest("CNC server is already running");
            }

            var success = await CNCServerManager.StartServerAsync();
            return Ok(success);
        }

        /// <summary>
        /// Stop the CNC server
        /// </summary>
        /// <returns>Success status</returns>
        [HttpPost("stop")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<ActionResult<bool>> StopServer()
        {
            if (!CNCServerManager.IsServerRunning)
            {
                return BadRequest("CNC server is not running");
            }

            var success = await CNCServerManager.StopServerAsync();
            return Ok(success);
        }

        /// <summary>
        /// Set the main window to always stay on top
        /// </summary>
        /// <returns>Success status</returns>
        [HttpPost("ui/always-on-top")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<ActionResult<bool>> SetAlwaysOnTop()
        {
            var success = await UIControlService.SetAlwaysOnTopAsync(true);
            return Ok(success);
        }

        /// <summary>
        /// Cancel the always on top behavior for the main window
        /// </summary>
        /// <returns>Success status</returns>
        [HttpPost("ui/cancel-always-on-top")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<ActionResult<bool>> CancelAlwaysOnTop()
        {
            var success = await UIControlService.SetAlwaysOnTopAsync(false);
            return Ok(success);
        }

        /// <summary>
        /// Get the current always on top state
        /// </summary>
        /// <returns>Always on top status</returns>
        [HttpGet("ui/always-on-top")]
        [ProducesResponseType(typeof(AlwaysOnTopStatus), 200)]
        public ActionResult<AlwaysOnTopStatus> GetAlwaysOnTop()
        {
            var isAlwaysOnTop = UIControlService.GetAlwaysOnTop();
            return Ok(new AlwaysOnTopStatus
            {
                IsAlwaysOnTop = isAlwaysOnTop
            });
        }
    }

    /// <summary>
    /// CNC server status information
    /// </summary>
    public class CNCServerStatus
    {
        /// <summary>
        /// Whether the CNC server is currently running
        /// </summary>
        public bool IsRunning { get; set; }

        /// <summary>
        /// Whether the server can be started
        /// </summary>
        public bool CanStart { get; set; }

        /// <summary>
        /// Whether the server can be stopped
        /// </summary>
        public bool CanStop { get; set; }
    }

    /// <summary>
    /// Always on top status information
    /// </summary>
    public class AlwaysOnTopStatus
    {
        /// <summary>
        /// Whether the main window is currently set to always stay on top
        /// </summary>
        public bool IsAlwaysOnTop { get; set; }
    }
}