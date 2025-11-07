using Microsoft.AspNetCore.Mvc;
using HavenCNCServer.Services;
using HavenCNCServer.Models;
using HavenCNCServer.Centriod;

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

        /// <summary>
        /// Trigger a virtual control panel (skin) event
        /// </summary>
        /// <param name="request">The skin event request containing the event and action to perform</param>
        /// <returns>Success status and message</returns>
        [HttpPost("skin-event")]
        [ProducesResponseType(typeof(SkinEventResponse), 200)]
        [ProducesResponseType(typeof(SkinEventResponse), 400)]
        [ProducesResponseType(typeof(SkinEventResponse), 500)]
        public async Task<ActionResult<SkinEventResponse>> TriggerSkinEvent([FromBody] SkinEventRequest request)
        {
            try
            {
                var cncPipe = CNCConnectionManager.GetOrCreateCNCPipe();
                if (cncPipe == null)
                {
                    return BadRequest(new SkinEventResponse
                    {
                        Success = false,
                        Message = "CNC connection not available"
                    });
                }

                var eventNumber = (int)request.SkinEvent;
                var success = false;
                var message = "";

                switch (request.Action)
                {
                    case SkinEventAction.Press:
                        var pressResult = cncPipe.plc.SetSkinEventState(eventNumber, 1);
                        success = pressResult == CentroidAPI.CNCPipe.ReturnCode.SUCCESS;
                        message = success ? $"Pressed skin event {request.SkinEvent} ({eventNumber})"
                                         : $"Failed to press skin event: {pressResult}";
                        break;

                    case SkinEventAction.Release:
                        var releaseResult = cncPipe.plc.SetSkinEventState(eventNumber, 0);
                        success = releaseResult == CentroidAPI.CNCPipe.ReturnCode.SUCCESS;
                        message = success ? $"Released skin event {request.SkinEvent} ({eventNumber})"
                                         : $"Failed to release skin event: {releaseResult}";
                        break;

                    case SkinEventAction.Click:
                        // Press, wait 100ms, then release
                        var clickPressResult = cncPipe.plc.SetSkinEventState(eventNumber, 1);
                        if (clickPressResult == CentroidAPI.CNCPipe.ReturnCode.SUCCESS)
                        {
                            await Task.Delay(100);
                            var clickReleaseResult = cncPipe.plc.SetSkinEventState(eventNumber, 0);
                            success = clickReleaseResult == CentroidAPI.CNCPipe.ReturnCode.SUCCESS;
                            message = success ? $"Clicked skin event {request.SkinEvent} ({eventNumber})"
                                             : $"Failed to complete click (release failed): {clickReleaseResult}";
                        }
                        else
                        {
                            success = false;
                            message = $"Failed to start click (press failed): {clickPressResult}";
                        }
                        break;

                    case SkinEventAction.DoubleClick:
                        // First click: Press, wait 100ms, release, wait 100ms
                        // Second click: Press, wait 100ms, release
                        var firstPressResult = cncPipe.plc.SetSkinEventState(eventNumber, 1);
                        if (firstPressResult == CentroidAPI.CNCPipe.ReturnCode.SUCCESS)
                        {
                            await Task.Delay(100);
                            var firstReleaseResult = cncPipe.plc.SetSkinEventState(eventNumber, 0);
                            if (firstReleaseResult == CentroidAPI.CNCPipe.ReturnCode.SUCCESS)
                            {
                                await Task.Delay(100);
                                var secondPressResult = cncPipe.plc.SetSkinEventState(eventNumber, 1);
                                if (secondPressResult == CentroidAPI.CNCPipe.ReturnCode.SUCCESS)
                                {
                                    await Task.Delay(100);
                                    var secondReleaseResult = cncPipe.plc.SetSkinEventState(eventNumber, 0);
                                    success = secondReleaseResult == CentroidAPI.CNCPipe.ReturnCode.SUCCESS;
                                    message = success ? $"Double-clicked skin event {request.SkinEvent} ({eventNumber})"
                                                     : $"Failed to complete double-click (second release failed): {secondReleaseResult}";
                                }
                                else
                                {
                                    success = false;
                                    message = $"Failed to complete double-click (second press failed): {secondPressResult}";
                                }
                            }
                            else
                            {
                                success = false;
                                message = $"Failed to complete double-click (first release failed): {firstReleaseResult}";
                            }
                        }
                        else
                        {
                            success = false;
                            message = $"Failed to start double-click (first press failed): {firstPressResult}";
                        }
                        break;

                    default:
                        return BadRequest(new SkinEventResponse
                        {
                            Success = false,
                            Message = $"Invalid action: {request.Action}"
                        });
                }

                if (success)
                {
                    return Ok(new SkinEventResponse
                    {
                        Success = true,
                        Message = message
                    });
                }
                else
                {
                    return StatusCode(500, new SkinEventResponse
                    {
                        Success = false,
                        Message = message
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new SkinEventResponse
                {
                    Success = false,
                    Message = $"Error triggering skin event: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Trigger a skin event by number (alternative endpoint for direct event number access)
        /// </summary>
        /// <param name="eventNumber">The skin event number (1-256)</param>
        /// <param name="actionRequest">The action request containing the action to perform</param>
        /// <returns>Success status and message</returns>
        [HttpPost("skin-event/{eventNumber:int}")]
        [ProducesResponseType(typeof(SkinEventResponse), 200)]
        [ProducesResponseType(typeof(SkinEventResponse), 400)]
        [ProducesResponseType(typeof(SkinEventResponse), 500)]
        public async Task<ActionResult<SkinEventResponse>> TriggerSkinEventByNumber(int eventNumber, [FromBody] SkinEventActionRequest actionRequest)
        {
            if (eventNumber < 1 || eventNumber > 256)
            {
                return BadRequest(new SkinEventResponse
                {
                    Success = false,
                    Message = "Event number must be between 1 and 256"
                });
            }

            // Convert to enum request format and call main method
            var request = new SkinEventRequest
            {
                SkinEvent = (SkinEvent)eventNumber,
                Action = actionRequest.Action
            };

            return await TriggerSkinEvent(request);
        }

        /// <summary>
        /// Reset CNC server state when in bad condition during G-code execution
        /// Uses skin events to trigger the reset button and clear faults
        /// </summary>
        /// <param name="resetEventNumber">Optional specific reset event number (defaults to system default)</param>
        /// <returns>Reset operation result</returns>
        [HttpPost("reset-cnc-state")]
        [ProducesResponseType(typeof(CNCResetResponse), 200)]
        [ProducesResponseType(typeof(CNCResetResponse), 400)]
        [ProducesResponseType(typeof(CNCResetResponse), 500)]
        public ActionResult<CNCResetResponse> ResetCNCState([FromQuery] int? resetEventNumber = null)
        {
            try
            {
                var eventNumber = resetEventNumber ?? CNCUtils.DEFAULT_CYCLE_START_EVENT;

                // Use the existing CNCUtils method which handles the skin event reset logic
                var success = CNCUtils.CheckAndResetCentroidState(eventNumber);

                if (success)
                {
                    return Ok(new CNCResetResponse
                    {
                        Success = true,
                        Message = $"CNC state successfully reset using skin event {eventNumber}",
                        ResetEventUsed = eventNumber
                    });
                }
                else
                {
                    return StatusCode(500, new CNCResetResponse
                    {
                        Success = false,
                        Message = $"Failed to reset CNC state with skin event {eventNumber}. Machine may be in restricted mode or reset failed.",
                        ResetEventUsed = eventNumber
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CNCResetResponse
                {
                    Success = false,
                    Message = $"Error during CNC reset: {ex.Message}",
                    ResetEventUsed = resetEventNumber
                });
            }
        }

        /// <summary>
        /// Check if CNC is ready to accept commands (without attempting reset)
        /// </summary>
        /// <returns>CNC readiness status</returns>
        [HttpGet("cnc-ready")]
        [ProducesResponseType(typeof(CNCReadyResponse), 200)]
        [ProducesResponseType(typeof(CNCReadyResponse), 500)]
        public ActionResult<CNCReadyResponse> CheckCNCReady()
        {
            try
            {
                var isReady = CNCUtils.IsCentroidReady();

                return Ok(new CNCReadyResponse
                {
                    IsReady = isReady,
                    Message = isReady ? "CNC is ready to accept commands" : "CNC is not ready (may be restricted or in fault state)"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CNCReadyResponse
                {
                    IsReady = false,
                    Message = $"Error checking CNC readiness: {ex.Message}"
                });
            }
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

    /// <summary>
    /// Request model for triggering skin events
    /// </summary>
    public class SkinEventRequest
    {
        /// <summary>
        /// The skin event to trigger
        /// </summary>
        public SkinEvent SkinEvent { get; set; }

        /// <summary>
        /// The action to perform on the skin event
        /// </summary>
        public SkinEventAction Action { get; set; }
    }

    /// <summary>
    /// Request model for skin event actions (used with event number endpoint)
    /// </summary>
    public class SkinEventActionRequest
    {
        /// <summary>
        /// The action to perform on the skin event
        /// </summary>
        public SkinEventAction Action { get; set; }
    }

    /// <summary>
    /// Response model for skin event operations
    /// </summary>
    public class SkinEventResponse
    {
        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Message describing the result of the operation
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response model for CNC reset operations
    /// </summary>
    public class CNCResetResponse
    {
        /// <summary>
        /// Whether the reset operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Message describing the result of the reset operation
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// The skin event number that was used for the reset operation
        /// </summary>
        public int? ResetEventUsed { get; set; }
    }

    /// <summary>
    /// Response model for CNC readiness check
    /// </summary>
    public class CNCReadyResponse
    {
        /// <summary>
        /// Whether the CNC is ready to accept commands
        /// </summary>
        public bool IsReady { get; set; }

        /// <summary>
        /// Message describing the CNC readiness state
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}