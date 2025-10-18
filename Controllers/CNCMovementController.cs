using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using HavenCNCServer.Models;
using HavenCNCServer.Centriod;
using HavenCNCServer.Services;
using CentroidAPI;

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
        /// Constructor for CNC Movement Controller
        /// </summary>
        public CNCMovementController()
        {
        }

        #region Movement Control

        /// <summary>
        /// Set the movement type (relative or absolute)
        /// </summary>
        [HttpPost("SetMoveType")]
        public void SetMoveType([FromBody] MoveType moveType)
        {
            var cncPipe = CNCConnectionManager.GetCNCPipe();
            if (cncPipe == null)
                throw new InvalidOperationException("CNC connection not available");

            // Send G90 for absolute, G91 for relative/incremental
            string gcode = moveType == MoveType.Absolute ? "G90" : "G91";

            // Use the CNCProgramController to send the G-code command
            var programController = new CNCProgramController();
            var result = programController.RunGCodeCommand(gcode).Result;

            if (!result.Success)
            {
                throw new InvalidOperationException($"Failed to set move type: {result.Error ?? "Unknown error"}");
            }
        }

        /// <summary>
        /// Get the current movement type
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

        /// <summary>
        /// Move to specified coordinates
        /// </summary>
        [HttpPost("MoveTo")]
        public void MoveTo([FromBody] MoveToRequest request)
        {
            var cncPipe = CNCConnectionManager.GetCNCPipe();
            if (cncPipe == null)
                throw new InvalidOperationException("CNC connection not available");

            var programController = new CNCProgramController();
            var commands = new List<string>();

            if (request.Strategy == MoveStrategy.Direct)
            {
                // Direct move - all axes move simultaneously
                var gcode = BuildMoveCommand(request.Point, request.XYSpeed);
                commands.Add(gcode);
            }
            else // MoveStrategy.ZSeparate
            {
                // Get current position to determine Z direction
                var currentPos = MachinePositionService.GetCurrentPosition();
                double zDelta = request.Point.Z - currentPos.Z;

                if (zDelta > 0)
                {
                    // Moving Z up (positive direction) - move Z first for safety
                    commands.Add(BuildMoveCommand(new MachinePoint { Z = request.Point.Z }, request.ZSpeed));
                    commands.Add(BuildMoveCommand(new MachinePoint { X = request.Point.X, Y = request.Point.Y, A = request.Point.A }, request.XYSpeed));
                }
                else
                {
                    // Moving Z down (negative direction) - move XY first, then Z last for safety
                    commands.Add(BuildMoveCommand(new MachinePoint { X = request.Point.X, Y = request.Point.Y, A = request.Point.A }, request.XYSpeed));
                    commands.Add(BuildMoveCommand(new MachinePoint { Z = request.Point.Z }, request.ZSpeed));
                }
            }

            // Execute the commands via the program controller
            var result = programController.RunGCode(commands.ToArray()).Result;
            if (!result.Success)
            {
                throw new InvalidOperationException($"Failed to execute move commands: {result.Error ?? "Unknown error"}");
            }
        }

        /// <summary>
        /// Build a G-code move command from a MachinePoint
        /// </summary>
        private string BuildMoveCommand(MachinePoint point, double? feedRate = null)
        {
            var parts = new List<string> { "G1" };

            if (point.X != 0) parts.Add($"X{point.X:F4}");
            if (point.Y != 0) parts.Add($"Y{point.Y:F4}");
            if (point.Z != 0) parts.Add($"Z{point.Z:F4}");
            if (point.A != 0) parts.Add($"A{point.A:F4}");

            if (feedRate.HasValue && feedRate.Value > 0)
                parts.Add($"F{feedRate.Value:F2}");

            return string.Join(" ", parts);
        }

        /// <summary>
        /// Build a G31 probe command from a MachinePoint
        /// G31 moves until probe input is triggered
        /// </summary>
        private string BuildProbeCommand(MachinePoint point, double? feedRate = null)
        {
            var parts = new List<string> { "G31" };

            if (point.X != 0) parts.Add($"X{point.X:F4}");
            if (point.Y != 0) parts.Add($"Y{point.Y:F4}");
            if (point.Z != 0) parts.Add($"Z{point.Z:F4}");
            if (point.A != 0) parts.Add($"A{point.A:F4}");

            if (feedRate.HasValue && feedRate.Value > 0)
                parts.Add($"F{feedRate.Value:F2}");

            return string.Join(" ", parts);
        }

        /// <summary>
        /// Move to coordinates until an IO event occurs
        /// Uses G31 probe cycle for move-until-input operations
        /// </summary>
        [HttpPost("MoveToUtil")]
        public void MoveToUntil([FromBody] MoveToUntilRequest request)
        {
            var cncPipe = CNCConnectionManager.GetCNCPipe();
            if (cncPipe == null)
                throw new InvalidOperationException("CNC connection not available");

            var programController = new CNCProgramController();
            var commands = new List<string>();

            // G31 is the probe/digitize cycle - moves until input is triggered
            // Format: G31 X__ Y__ Z__ A__ F__ (feed rate)

            if (request.Strategy == MoveStrategy.Direct)
            {
                // Direct move - all axes move simultaneously using G31
                commands.Add(BuildProbeCommand(request.Point, request.XYSpeed));
            }
            else // MoveStrategy.ZSeparate
            {
                // Get current position to determine Z direction
                var currentPos = MachinePositionService.GetCurrentPosition();
                double zDelta = request.Point.Z - currentPos.Z;

                if (zDelta > 0)
                {
                    // Moving Z up - move Z first for safety
                    commands.Add(BuildProbeCommand(new MachinePoint { Z = request.Point.Z }, request.ZSpeed));
                    commands.Add(BuildProbeCommand(new MachinePoint { X = request.Point.X, Y = request.Point.Y, A = request.Point.A }, request.XYSpeed));
                }
                else
                {
                    // Moving Z down - move XY first, then Z last for safety
                    commands.Add(BuildProbeCommand(new MachinePoint { X = request.Point.X, Y = request.Point.Y, A = request.Point.A }, request.XYSpeed));
                    commands.Add(BuildProbeCommand(new MachinePoint { Z = request.Point.Z }, request.ZSpeed));
                }
            }

            // Execute the probe commands
            var result = programController.RunGCode(commands.ToArray()).Result;
            if (!result.Success)
            {
                throw new InvalidOperationException($"Failed to execute probe move commands: {result.Error ?? "Unknown error"}");
            }
        }

        /// <summary>
        /// Move in a direction until an IO event occurs
        /// Uses G31 probe cycle with G91 (relative mode) for directional probing
        /// </summary>
        [HttpPost("MoveDirectionUntil")]
        public void MoveDirectionUntil([FromBody] MoveDirectionUntilRequest request)
        {
            var cncPipe = CNCConnectionManager.GetCNCPipe();
            if (cncPipe == null)
                throw new InvalidOperationException("CNC connection not available");

            var programController = new CNCProgramController();
            var commands = new List<string>();

            // Save current positioning mode, switch to incremental (G91)
            commands.Add("G91");

            // Build probe move based on direction
            // Use a large distance (e.g., 1000 units) that will be stopped by the probe input
            double probeDistance = 1000.0;
            var probePoint = new MachinePoint();

            switch (request.Direction)
            {
                case MoveDirection.XPositive:
                    probePoint.X = probeDistance;
                    break;
                case MoveDirection.XNegative:
                    probePoint.X = -probeDistance;
                    break;
                case MoveDirection.YPositive:
                    probePoint.Y = probeDistance;
                    break;
                case MoveDirection.YNegative:
                    probePoint.Y = -probeDistance;
                    break;
                case MoveDirection.ZPositive:
                    probePoint.Z = probeDistance;
                    break;
                case MoveDirection.ZNegative:
                    probePoint.Z = -probeDistance;
                    break;
            }

            // Add the G31 probe command with the directional move
            commands.Add(BuildProbeCommand(probePoint, request.Speed));

            // Restore to absolute mode (G90)
            commands.Add("G90");

            // Execute the probe sequence
            var result = programController.RunGCode(commands.ToArray()).Result;
            if (!result.Success)
            {
                throw new InvalidOperationException($"Failed to execute directional probe: {result.Error ?? "Unknown error"}");
            }
        }

        #endregion

        #region Fixture Management

        /// <summary>
        /// Set fixture point using current machine position or specified coordinates
        /// Sets the part location for the ACTIVE WCS (Work Coordinate System)
        /// </summary>
        /// <param name="point">Machine point with X, Y, Z, A coordinates to set as the workpiece origin</param>
        [HttpPost("SetFixturePoint")]
        public void SetFixturePoint([FromBody] MachinePoint point)
        {
            var cncPipe = CNCConnectionManager.GetCNCPipe();
            if (cncPipe == null)
                throw new InvalidOperationException("CNC connection not available. Ensure CNCConnectionManager is connected.");

            // Set workpiece location for each axis in the active WCS
            // Axis numbers: 1=X, 2=Y, 3=Z, 4=A

            // Set X axis (axis 1)
            cncPipe.wcs.SetWorkpieceLocation(1, point.X);

            // Set Y axis (axis 2)
            cncPipe.wcs.SetWorkpieceLocation(2, point.Y);

            // Set Z axis (axis 3)
            cncPipe.wcs.SetWorkpieceLocation(3, point.Z);

            // Set A axis (axis 4)
            cncPipe.wcs.SetWorkpieceLocation(4, point.A);
        }

        #endregion

        #region Feed Rate Control

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
            var result = programController.RunGCodeCommand($"M99 P{percentage}").Result;

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
