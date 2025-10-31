using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using HavenCNCServer.Models;
using HavenCNCServer.Centriod;
using HavenCNCServer.Services;
using CentroidAPI;
using System.Threading.Tasks;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Controllers
{
    /// <summary>
    /// CNC Movement Control - Handles all movement, positioning, and fixture operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CNCMovementController : ControllerBase
    {
        /// <summary>
        /// The last fixture point that was set - persists in memory across operations
        /// </summary>
        private static MachinePoint? _lastFixturePoint = null;

        /// <summary>
        /// Gets the last fixture point that was set
        /// </summary>
        public static MachinePoint? LastFixturePoint => _lastFixturePoint;

        /// <summary>
        /// Restores the last fixture point if one was previously set
        /// Should be called when Centroid connection becomes available
        /// </summary>
        public static async Task RestoreLastFixturePointAsync()
        {
            if (_lastFixturePoint != null && CNCConnectionManager.IsConnected)
            {
                try
                {
                    LogInfo($"🔄 Restoring last fixture point: X={_lastFixturePoint.X:F4}, Y={_lastFixturePoint.Y:F4}, Z={_lastFixturePoint.Z:F4}, A={_lastFixturePoint.A:F4}", "Fixture");
                    
                    var controller = new CNCMovementController();
                    await controller.SetFixturePoint(_lastFixturePoint);
                    
                    LogSuccess("✓ Last fixture point restored successfully", "Fixture");
                }
                catch (Exception ex)
                {
                    LogError($"❌ Failed to restore last fixture point: {ex.Message}", "Fixture");
                }
            }
        }

        /// <summary>
        /// Constructor for CNC Movement Controller
        /// </summary>
        public CNCMovementController()
        {
        }

        #region Position Query

        /// <summary>
        /// Get the current movement type (absolute or relative positioning mode)
        /// </summary>
        [HttpGet("GetMoveType")]
        public MoveType GetMoveType()
        {
            var cncPipe = CNCConnectionManager.GetCNCPipe();
            if (cncPipe == null)
                throw new InvalidOperationException("CNC connection not available");

            var result = cncPipe.state.GetPositioningMode(out var positioningMode);

            if (result != CNCPipe.ReturnCode.SUCCESS)
                throw new InvalidOperationException($"Failed to get positioning mode: {result}");

            // Map CentroidAPI.CNCPipe.State.PositioningMode to MoveType
            return positioningMode switch
            {
                CNCPipe.State.PositioningMode.ABSOLUTE => MoveType.Absolute,
                CNCPipe.State.PositioningMode.INCREMENTAL => MoveType.Relative,
                _ => MoveType.Absolute // Default to absolute for unknown states
            };
        }

        /// <summary>
        /// Get current machine position
        /// </summary>
        [HttpGet("GetCurrentPosition")]
        public MachinePoint GetCurrentPosition()
        {
            return MachinePositionService.GetCurrentPosition();
        }

        #endregion

        #region Fixture Management

        /// <summary>
        /// Get the last fixture point that was set (persisted in memory)
        /// </summary>
        /// <returns>The last fixture point or null if none has been set</returns>
        [HttpGet("GetLastFixturePoint")]
        public ActionResult<MachinePoint?> GetLastFixturePoint()
        {
            LoggingService.LogInfo($"📍 GetLastFixturePoint called - Last point: {(_lastFixturePoint != null ? $"X={_lastFixturePoint.X:F4}, Y={_lastFixturePoint.Y:F4}, Z={_lastFixturePoint.Z:F4}, A={_lastFixturePoint.A:F4}" : "None")}", "Fixture");
            return Ok(_lastFixturePoint);
        }

        /// <summary>
        /// Set fixture point using current machine position or specified coordinates
        /// Sets the part location for the ACTIVE WCS (Work Coordinate System)
        /// </summary>
        /// <param name="point">Machine point with X, Y, Z, A coordinates to set as the workpiece origin</param>
        [HttpPost("SetFixturePoint")]
        public async Task SetFixturePoint([FromBody] MachinePoint point)
        {
            LoggingService.LogInfo($"📍 SetFixturePoint called: X={point.X:F4}, Y={point.Y:F4}, Z={point.Z:F4}, A={point.A:F4}", "Fixture");

            var cncPipe = CNCConnectionManager.GetCNCPipe();
            if (cncPipe == null)
            {
                LoggingService.LogError("❌ SetFixturePoint failed: CNC connection not available", "Fixture");
                throw new InvalidOperationException("CNC connection not available. Ensure CNCConnectionManager is connected.");
            }

            // Set workpiece location for each axis in the active WCS
            // Axis numbers: 1=X, 2=Y, 3=Z, 4=A

            LoggingService.LogInfo("Setting workpiece location for all axes in active WCS...", "Fixture");

            // Set X axis (axis 1)
            cncPipe.wcs.SetWorkpieceLocation(1, point.X);

            // Set Y axis (axis 2)
            cncPipe.wcs.SetWorkpieceLocation(2, point.Y);

            // Set Z axis (axis 3)
            cncPipe.wcs.SetWorkpieceLocation(3, point.Z);

            // Set A axis (axis 4)
            cncPipe.wcs.SetWorkpieceLocation(4, point.A);

            // Save this as the last fixture point for restoration on reconnect
            _lastFixturePoint = new MachinePoint
            {
                X = point.X,
                Y = point.Y,
                Z = point.Z,
                A = point.A
            };

            LoggingService.LogSuccess($"✅ Fixture point set successfully: X={point.X:F4}, Y={point.Y:F4}, Z={point.Z:F4}, A={point.A:F4}", "Fixture");

            // Broadcast the position change to all SignalR clients
            // Since changing the fixture point changes the work coordinate display
            await SignalRManager.BroadcastCurrentPosition("Fixture point changed");
        }

        #endregion

        #region Feed Rate Override

        /// <summary>
        /// Set feed rate override percentage
        /// </summary>
        /// <param name="percentage">Feed rate override percentage (1-120 typically)</param>
        [HttpPost("SetFeedRateOverride")]
        public void SetFeedRateOverride([FromBody] int percentage)
        {
            var cncPipe = CNCConnectionManager.GetCNCPipe();
            if (cncPipe == null)
                throw new InvalidOperationException("CNC connection not available");

            if (percentage < 1 || percentage > 200)
                throw new ArgumentOutOfRangeException(nameof(percentage), "Feed rate override must be between 1 and 200");

            // Use M99 P__ to set feed rate override percentage
            var programController = new CNCProgramController();
            var request = new Models.RunGCodeCommandRequest
            {
                GCode = $"M99 P{percentage}"
            };
            var result = programController.RunGCodeCommand(request).Result;

            if (!result.Success)
            {
                throw new InvalidOperationException($"Failed to set feed rate override: {result.Error ?? "Unknown error"}");
            }
        }

        /// <summary>
        /// Get current feed rate override percentage
        /// </summary>
        /// <returns>Feed rate override percentage (typically 1-120)</returns>
        [HttpGet("GetFeedRateOverride")]
        public int GetFeedRateOverride()
        {
            var cncPipe = CNCConnectionManager.GetCNCPipe();
            if (cncPipe == null)
                throw new InvalidOperationException("CNC connection not available");

            var result = cncPipe.state.GetFeedrateOverride(out int feedrateOverride);

            if (result != CNCPipe.ReturnCode.SUCCESS)
                throw new InvalidOperationException($"Failed to get feed rate override: {result}");

            return feedrateOverride;
        }

        /// <summary>
        /// Reset feed rate override to 100% (default)
        /// </summary>
        [HttpPost("ResetFeedRateOverride")]
        public void ResetFeedRateOverride()
        {
            SetFeedRateOverride(100);
        }

        #endregion
    }
}
