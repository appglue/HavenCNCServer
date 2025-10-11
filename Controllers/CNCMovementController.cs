using Microsoft.AspNetCore.Mvc;
using HavenCNCServer.Models;
using HavenCNCServer.Services;

namespace HavenCNCServer.Controllers
{
    /// <summary>
    /// CNC Movement Control - Handles all movement, positioning, and fixture operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CNCMovementController : ControllerBase
    {
        private readonly ICNCMovementService _movementService;

        /// <summary>
        /// Constructor for CNC Movement Controller
        /// </summary>
        public CNCMovementController(ICNCMovementService movementService)
        {
            _movementService = movementService;
        }

        #region Movement Control

        /// <summary>
        /// Set the movement type (relative or absolute)
        /// </summary>
        [HttpPost("SetMoveType")]
        public void SetMoveType([FromBody] MoveType moveType) => _movementService.SetMoveType(moveType);

        /// <summary>
        /// Get the current movement type
        /// </summary>
        [HttpGet("GetMoveType")]
        public MoveType GetMoveType() => _movementService.GetMoveType();

        /// <summary>
        /// Get current machine position
        /// </summary>
        [HttpGet("GetCurrentPosition")]
        public MachinePoint GetCurrentPosition() => _movementService.GetCurrentPosition();

        /// <summary>
        /// Move to specified coordinates
        /// </summary>
        [HttpPost("MoveTo")]
        public async Task MoveTo([FromBody] MoveToRequest request) => await _movementService.MoveToAsync(request);

        /// <summary>
        /// Move to coordinates until an IO event occurs
        /// </summary>
        [HttpPost("MoveToUtil")]
        public async Task MoveToUtil([FromBody] MoveToUntilRequest request) => await _movementService.MoveToUntilAsync(request);

        /// <summary>
        /// Move in a direction until an IO event occurs
        /// </summary>
        [HttpPost("MoveDirectionUntil")]
        public async Task MoveDirectionUntil([FromBody] MoveDirectionUntilRequest request) => await _movementService.MoveDirectionUntilAsync(request);

        #endregion

        #region Fixture Management

        /// <summary>
        /// Set fixture point using current machine position or specified coordinates
        /// </summary>
        [HttpPost("SetFixturePoint")]
        public async Task SetFixturePoint([FromBody] MachinePoint point) => await _movementService.SetFixturePointAsync(point);

        #endregion

        #region Feed Rate Control

        /// <summary>
        /// Get fast feed rate
        /// </summary>
        [HttpGet("GetFastFeedRate")]
        public async Task<double> GetFastFeedRate() => await _movementService.GetFastFeedRateAsync();

        /// <summary>
        /// Set fast feed rate
        /// </summary>
        [HttpPost("SetFastFeedRate")]
        public async Task SetFastFeedRate([FromBody] double feedRate) => await _movementService.SetFastFeedRateAsync(feedRate);

        /// <summary>
        /// Get normal feed rate
        /// </summary>
        [HttpGet("GetNormalFeedRate")]
        public async Task<double> GetNormalFeedRate() => await _movementService.GetNormalFeedRateAsync();

        /// <summary>
        /// Set normal feed rate
        /// </summary>
        [HttpPost("SetNormalFeedRate")]
        public async Task SetNormalFeedRate([FromBody] double feedRate) => await _movementService.SetNormalFeedRateAsync(feedRate);

        /// <summary>
        /// Adjust normal feed rate by factor
        /// </summary>
        [HttpPost("AdjustNormalFeedRate")]
        public async Task AdjustNormalFeedRate([FromBody] double factor) => await _movementService.AdjustNormalFeedRateAsync(factor);

        /// <summary>
        /// Get current normal feed rate adjustment factor
        /// </summary>
        [HttpGet("GetCurrentNormalFeedRateFactor")]
        public async Task<double> GetCurrentNormalFeedRateFactor() => await _movementService.GetCurrentNormalFeedRateFactorAsync();

        /// <summary>
        /// Reset normal feed rate factor to default
        /// </summary>
        [HttpPost("ResetNormalFeedRateFactor")]
        public async Task ResetNormalFeedRateFactor() => await _movementService.ResetNormalFeedRateFactorAsync();

        /// <summary>
        /// Adjust fast feed rate by factor
        /// </summary>
        [HttpPost("AdjustFastFeedRate")]
        public async Task AdjustFastFeedRate([FromBody] double factor) => await _movementService.AdjustFastFeedRateAsync(factor);

        /// <summary>
        /// Get current fast feed rate adjustment factor
        /// </summary>
        [HttpGet("GetCurrentFastFeedRateFactor")]
        public async Task<double> GetCurrentFastFeedRateFactor() => await _movementService.GetCurrentFastFeedRateFactorAsync();

        /// <summary>
        /// Reset fast feed rate factor to default
        /// </summary>
        [HttpPost("ResetFastFeedRateFactor")]
        public async Task ResetFastFeedRateFactor() => await _movementService.ResetFastFeedRateFactorAsync();

        #endregion
    }
}
