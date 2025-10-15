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
        private readonly CNCServerManager _serverManager;

        /// <summary>
        /// Initialize the CNC Server Controller
        /// </summary>
        /// <param name="serverManager">The CNC server manager service</param>
        public CNCServerController(CNCServerManager serverManager)
        {
            _serverManager = serverManager;
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
                IsRunning = _serverManager.IsServerRunning,
                WeStartedServer = _serverManager.WeStartedServer,
                CanStart = !_serverManager.IsServerRunning,
                CanStop = _serverManager.IsServerRunning && _serverManager.WeStartedServer,
                CanRestart = _serverManager.WeStartedServer
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
            if (_serverManager.IsServerRunning)
            {
                return BadRequest("CNC server is already running");
            }

            var success = await _serverManager.StartServerAsync();
            return Ok(success);
        }

        /// <summary>
        /// Stop the CNC server (only if we started it)
        /// </summary>
        /// <returns>Success status</returns>
        [HttpPost("stop")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<ActionResult<bool>> StopServer()
        {
            if (!_serverManager.IsServerRunning)
            {
                return BadRequest("CNC server is not running");
            }

            if (!_serverManager.WeStartedServer)
            {
                return BadRequest("Cannot stop CNC server - we didn't start it");
            }

            var success = await _serverManager.StopServerAsync();
            return Ok(success);
        }

        /// <summary>
        /// Restart the CNC server
        /// </summary>
        /// <returns>Success status</returns>
        [HttpPost("restart")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<ActionResult<bool>> RestartServer()
        {
            if (!_serverManager.WeStartedServer)
            {
                return BadRequest("Cannot restart CNC server - we didn't start it");
            }

            var success = await _serverManager.RestartServerAsync();
            return Ok(success);
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
        /// Whether we started the server (and can manage it)
        /// </summary>
        public bool WeStartedServer { get; set; }

        /// <summary>
        /// Whether the server can be started
        /// </summary>
        public bool CanStart { get; set; }

        /// <summary>
        /// Whether the server can be stopped
        /// </summary>
        public bool CanStop { get; set; }

        /// <summary>
        /// Whether the server can be restarted
        /// </summary>
        public bool CanRestart { get; set; }
    }
}