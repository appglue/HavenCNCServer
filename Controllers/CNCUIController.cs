using Microsoft.AspNetCore.Mvc;
using HavenCNCServer.Centroid;
using HavenCNCServer.Models;
using HavenCNCServer.Services;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Controllers
{
    /// <summary>
    /// CNC UI Functions - Handles common UI button/control events
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CNCUIController : ControllerBase
    {
        private static bool? _machineHomedState = null; // Cached homed state, null = unknown
        private static readonly object _homedStateLock = new object();

        /// <summary>
        /// Initialize the homed state from Centroid (called on startup)
        /// </summary>
        public static void InitializeHomedState()
        {
            lock (_homedStateLock)
            {
                try
                {
                    var cncPipe = CNCConnectionManager.GetCNCPipe();
                    if (cncPipe != null)
                    {
                        // Check each axis home status
                        cncPipe.plc.GetPcSystemVariableBit(CentroidAPI.PcToMpuSysVarBit.SV_HOME_SET_AXIS_1, out var state_axis_1);
                        cncPipe.plc.GetPcSystemVariableBit(CentroidAPI.PcToMpuSysVarBit.SV_HOME_SET_AXIS_2, out var state_axis_2);
                        cncPipe.plc.GetPcSystemVariableBit(CentroidAPI.PcToMpuSysVarBit.SV_HOME_SET_AXIS_3, out var state_axis_3);

                        if (state_axis_1 == CentroidAPI.CNCPipe.Plc.IOState.IO_LOGICAL_1 &&
                            state_axis_2 == CentroidAPI.CNCPipe.Plc.IOState.IO_LOGICAL_1 &&
                            state_axis_3 == CentroidAPI.CNCPipe.Plc.IOState.IO_LOGICAL_1)
                        {
                            cncPipe.plc.GetPcSystemVariableBit(CentroidAPI.PcToMpuSysVarBit.SV_PC_HOME_SET, out var state_home);
                            _machineHomedState = (state_home == CentroidAPI.CNCPipe.Plc.IOState.IO_LOGICAL_1);
                        }
                        else
                        {
                            _machineHomedState = false;
                        }

                        LogInfo($"Machine homed state initialized: {_machineHomedState}", "CNCUIController");
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Error initializing homed state from Centroid: {ex.Message}", "CNCUIController");
                    _machineHomedState = null;
                }
            }
        }

        /// <summary>
        /// Get the current cached machine homed state
        /// </summary>
        /// <returns>True if homed, false if not homed, null if unknown</returns>
        public static bool? GetCachedMachineHomedState()
        {
            lock (_homedStateLock)
            {
                return _machineHomedState;
            }
        }

        #region General Skin Event Controls

        /// <summary>
        /// Trigger any skin event by number (press, wait 100ms, release)
        /// </summary>
        /// <param name="eventNumber">Skin event number (1-256)</param>
        [HttpPost("TriggerSkinEvent/{eventNumber}")]
        public IActionResult TriggerSkinEvent(int eventNumber)
        {
            var result = CNCUtils.TriggerSkinEvent(eventNumber);
            return result ? Ok(new { success = true, message = $"Skin event {eventNumber} triggered" })
                          : StatusCode(500, new { success = false, message = $"Failed to trigger event {eventNumber}" });
        }

        /// <summary>
        /// Start (press and hold) any skin event by number
        /// </summary>
        /// <param name="eventNumber">Skin event number (1-256)</param>
        /// <param name="willRenew">If true, client must call RenewSkinEvent every 100ms or event will auto-stop after 250ms</param>
        [HttpPost("StartSkinEvent/{eventNumber}")]
        public IActionResult StartSkinEvent(int eventNumber, [FromQuery] bool willRenew = false)
        {
            var result = CNCUtils.StartSkinEvent(eventNumber, willRenew);
            return result ? Ok(new { success = true, message = $"Skin event {eventNumber} started{(willRenew ? " with renewal tracking" : "")}" })
                          : StatusCode(500, new { success = false, message = $"Failed to start event {eventNumber}" });
        }

        /// <summary>
        /// Stop (release) any skin event by number
        /// </summary>
        /// <param name="eventNumber">Skin event number (1-256)</param>
        [HttpPost("StopSkinEvent/{eventNumber}")]
        public IActionResult StopSkinEvent(int eventNumber)
        {
            var result = CNCUtils.StopSkinEvent(eventNumber);
            return result ? Ok(new { success = true, message = $"Skin event {eventNumber} stopped" })
                          : StatusCode(500, new { success = false, message = $"Failed to stop event {eventNumber}" });
        }

        /// <summary>
        /// Renew a skin event to prevent auto-stop (for events started with willRenew=true)
        /// Must be called every 100ms to keep event active
        /// </summary>
        /// <param name="eventNumber">Skin event number (1-256)</param>
        [HttpPost("RenewSkinEvent/{eventNumber}")]
        public IActionResult RenewSkinEvent(int eventNumber)
        {
            var result = CNCUtils.RenewSkinEvent(eventNumber);
            return result ? Ok(new { success = true, message = $"Skin event {eventNumber} renewed" })
                          : StatusCode(404, new { success = false, message = $"Event {eventNumber} not found in renewal tracking" });
        }

        #endregion

        #region Jog Mode Controls

        /// <summary>
        /// Set continuous/incremental jog mode (INC_CONT_JOG_UI)
        /// Continuous mode (true) holds event 26, incremental mode (false) releases it
        /// </summary>
        /// <param name="isContinuous">True for continuous mode, false for incremental mode</param>
        [HttpPost("SetJogMode")]
        public IActionResult SetJogMode([FromQuery] bool isContinuous)
        {
            bool result;
            if (isContinuous)
            {
                // Start (hold) event 26 for continuous mode
                result = CNCUtils.StartSkinEvent(26, false);
            }
            else
            {
                // Stop (release) event 26 for incremental mode
                result = CNCUtils.StopSkinEvent(26);
            }

            if (result)
            {
                // Update the incremental mode state (inverted: continuous = !incremental)
                CNCMovementController.UpdateIncrementalMode(!isContinuous);
            }
            return result ? Ok(new { success = true, message = $"Jog mode set to {(isContinuous ? "continuous" : "incremental")}" })
                          : StatusCode(500, new { success = false, message = "Failed to set jog mode" });
        }

        /// <summary>
        /// Trigger jog increment speed (X1, X10, X100)
        /// </summary>
        /// <param name="speed">Jog increment speed to activate</param>
        [HttpPost("TriggerJogIncrementSpeed")]
        public IActionResult TriggerJogIncrementSpeed([FromQuery] JogIncrementSpeed speed)
        {
            LogInfo($"TriggerJogIncrementSpeed called with speed: {speed} (value: {(int)speed})", "CNCUIController");

            int eventNumber = speed switch
            {
                JogIncrementSpeed.X1 => 27,   // JOG_X1_UI
                JogIncrementSpeed.X10 => 28,  // JOG_X10_UI
                JogIncrementSpeed.X100 => 29, // JOG_X100_UI
                _ => 27
            };

            LogInfo($"Triggering skin event {eventNumber} for jog increment speed {speed}", "CNCUIController");

            var result = CNCUtils.TriggerSkinEvent(eventNumber);

            LogInfo($"TriggerSkinEvent result: {result}", "CNCUIController");

            if (result)
            {
                // Update the increment speed state and broadcast
                CNCMovementController.UpdateJogIncrementSpeed(speed);
            }
            return result ? Ok(new { success = true, message = $"Jog {speed} mode activated" })
                          : StatusCode(500, new { success = false, message = "Failed to trigger event" });
        }

        /// <summary>
        /// Set fast/slow jog mode (JOG_FAST_SLOW_UI)
        /// Fast mode (true) holds event 38, slow mode (false) releases it
        /// </summary>
        /// <param name="isFast">True for fast mode, false for slow mode</param>
        [HttpPost("SetFastJog")]
        public IActionResult SetFastJog([FromQuery] bool isFast)
        {
            bool result;
            if (isFast)
            {
                // Start (hold) event 38 for fast mode
                result = CNCUtils.StartSkinEvent(38, false);
            }
            else
            {
                // Stop (release) event 38 for slow mode
                result = CNCUtils.StopSkinEvent(38);
            }

            if (result)
            {
                // Update the slow jog mode state (inverted: fast = !slow)
                CNCMovementController.UpdateSlowJogMode(!isFast);
            }
            return result ? Ok(new { success = true, message = $"Jog speed set to {(isFast ? "fast" : "slow")}" })
                          : StatusCode(500, new { success = false, message = "Failed to set jog speed" });
        }

        #endregion

        #region Axis 1 Jog Controls

        /// <summary>
        /// Trigger Axis 1 jog in plus direction (JOG_AXIS1_PLUS_UI)
        /// </summary>
        [HttpPost("TriggerJogAxis1Plus")]
        public IActionResult TriggerJogAxis1Plus()
        {
            var result = CNCUtils.TriggerSkinEvent(39);
            return result ? Ok(new { success = true, message = "Axis 1 jogged plus" })
                          : StatusCode(500, new { success = false, message = "Failed to trigger event" });
        }

        /// <summary>
        /// Start Axis 1 jog in plus direction (hold) (JOG_AXIS1_PLUS_UI)
        /// </summary>
        /// <param name="willRenew">If true, client must call RenewSkinEvent every 100ms</param>
        [HttpPost("StartJogAxis1Plus")]
        public IActionResult StartJogAxis1Plus([FromQuery] bool willRenew = false)
        {
            var result = CNCUtils.StartSkinEvent(39, willRenew);
            return result ? Ok(new { success = true, message = "Axis 1 jog plus started" })
                          : StatusCode(500, new { success = false, message = "Failed to start event" });
        }

        /// <summary>
        /// Stop Axis 1 jog in plus direction (release) (JOG_AXIS1_PLUS_UI)
        /// </summary>
        [HttpPost("StopJogAxis1Plus")]
        public IActionResult StopJogAxis1Plus()
        {
            var result = CNCUtils.StopSkinEvent(39);
            return result ? Ok(new { success = true, message = "Axis 1 jog plus stopped" })
                          : StatusCode(500, new { success = false, message = "Failed to stop event" });
        }

        /// <summary>
        /// Trigger Axis 1 jog in minus direction (JOG_AXIS1_MINUS_UI)
        /// </summary>
        [HttpPost("TriggerJogAxis1Minus")]
        public IActionResult TriggerJogAxis1Minus()
        {
            var result = CNCUtils.TriggerSkinEvent(37);
            return result ? Ok(new { success = true, message = "Axis 1 jogged minus" })
                          : StatusCode(500, new { success = false, message = "Failed to trigger event" });
        }

        /// <summary>
        /// Start Axis 1 jog in minus direction (hold) (JOG_AXIS1_MINUS_UI)
        /// </summary>
        /// <param name="willRenew">If true, client must call RenewSkinEvent every 100ms</param>
        [HttpPost("StartJogAxis1Minus")]
        public IActionResult StartJogAxis1Minus([FromQuery] bool willRenew = false)
        {
            var result = CNCUtils.StartSkinEvent(37, willRenew);
            return result ? Ok(new { success = true, message = "Axis 1 jog minus started" })
                          : StatusCode(500, new { success = false, message = "Failed to start event" });
        }

        /// <summary>
        /// Stop Axis 1 jog in minus direction (release) (JOG_AXIS1_MINUS_UI)
        /// </summary>
        [HttpPost("StopJogAxis1Minus")]
        public IActionResult StopJogAxis1Minus()
        {
            var result = CNCUtils.StopSkinEvent(37);
            return result ? Ok(new { success = true, message = "Axis 1 jog minus stopped" })
                          : StatusCode(500, new { success = false, message = "Failed to stop event" });
        }

        #endregion

        #region Axis 2 Jog Controls

        /// <summary>
        /// Trigger Axis 2 jog in plus direction (JOG_AXIS2_PLUS_UI)
        /// </summary>
        [HttpPost("TriggerJogAxis2Plus")]
        public IActionResult TriggerJogAxis2Plus()
        {
            var result = CNCUtils.TriggerSkinEvent(33);
            return result ? Ok(new { success = true, message = "Axis 2 jogged plus" })
                          : StatusCode(500, new { success = false, message = "Failed to trigger event" });
        }

        /// <summary>
        /// Start Axis 2 jog in plus direction (hold) (JOG_AXIS2_PLUS_UI)
        /// </summary>
        /// <param name="willRenew">If true, client must call RenewSkinEvent every 100ms</param>
        [HttpPost("StartJogAxis2Plus")]
        public IActionResult StartJogAxis2Plus([FromQuery] bool willRenew = false)
        {
            var result = CNCUtils.StartSkinEvent(33, willRenew);
            return result ? Ok(new { success = true, message = "Axis 2 jog plus started" })
                          : StatusCode(500, new { success = false, message = "Failed to start event" });
        }

        /// <summary>
        /// Stop Axis 2 jog in plus direction (release) (JOG_AXIS2_PLUS_UI)
        /// </summary>
        [HttpPost("StopJogAxis2Plus")]
        public IActionResult StopJogAxis2Plus()
        {
            var result = CNCUtils.StopSkinEvent(33);
            return result ? Ok(new { success = true, message = "Axis 2 jog plus stopped" })
                          : StatusCode(500, new { success = false, message = "Failed to stop event" });
        }

        /// <summary>
        /// Trigger Axis 2 jog in minus direction (JOG_AXIS2_MINUS_UI)
        /// </summary>
        [HttpPost("TriggerJogAxis2Minus")]
        public IActionResult TriggerJogAxis2Minus()
        {
            var result = CNCUtils.TriggerSkinEvent(43);
            return result ? Ok(new { success = true, message = "Axis 2 jogged minus" })
                          : StatusCode(500, new { success = false, message = "Failed to trigger event" });
        }

        /// <summary>
        /// Start Axis 2 jog in minus direction (hold) (JOG_AXIS2_MINUS_UI)
        /// </summary>
        /// <param name="willRenew">If true, client must call RenewSkinEvent every 100ms</param>
        [HttpPost("StartJogAxis2Minus")]
        public IActionResult StartJogAxis2Minus([FromQuery] bool willRenew = false)
        {
            var result = CNCUtils.StartSkinEvent(43, willRenew);
            return result ? Ok(new { success = true, message = "Axis 2 jog minus started" })
                          : StatusCode(500, new { success = false, message = "Failed to start event" });
        }

        /// <summary>
        /// Stop Axis 2 jog in minus direction (release) (JOG_AXIS2_MINUS_UI)
        /// </summary>
        [HttpPost("StopJogAxis2Minus")]
        public IActionResult StopJogAxis2Minus()
        {
            var result = CNCUtils.StopSkinEvent(43);
            return result ? Ok(new { success = true, message = "Axis 2 jog minus stopped" })
                          : StatusCode(500, new { success = false, message = "Failed to stop event" });
        }

        #endregion

        #region Axis 3 Jog Controls

        /// <summary>
        /// Trigger Axis 3 jog in plus direction (JOG_AXIS3_PLUS_UI)
        /// </summary>
        [HttpPost("TriggerJogAxis3Plus")]
        public IActionResult TriggerJogAxis3Plus()
        {
            var result = CNCUtils.TriggerSkinEvent(35);
            return result ? Ok(new { success = true, message = "Axis 3 jogged plus" })
                          : StatusCode(500, new { success = false, message = "Failed to trigger event" });
        }

        /// <summary>
        /// Start Axis 3 jog in plus direction (hold) (JOG_AXIS3_PLUS_UI)
        /// </summary>
        /// <param name="willRenew">If true, client must call RenewSkinEvent every 100ms</param>
        [HttpPost("StartJogAxis3Plus")]
        public IActionResult StartJogAxis3Plus([FromQuery] bool willRenew = false)
        {
            var result = CNCUtils.StartSkinEvent(35, willRenew);
            return result ? Ok(new { success = true, message = "Axis 3 jog plus started" })
                          : StatusCode(500, new { success = false, message = "Failed to start event" });
        }

        /// <summary>
        /// Stop Axis 3 jog in plus direction (release) (JOG_AXIS3_PLUS_UI)
        /// </summary>
        [HttpPost("StopJogAxis3Plus")]
        public IActionResult StopJogAxis3Plus()
        {
            var result = CNCUtils.StopSkinEvent(35);
            return result ? Ok(new { success = true, message = "Axis 3 jog plus stopped" })
                          : StatusCode(500, new { success = false, message = "Failed to stop event" });
        }

        /// <summary>
        /// Trigger Axis 3 jog in minus direction (JOG_AXIS3_MINUS_UI)
        /// </summary>
        [HttpPost("TriggerJogAxis3Minus")]
        public IActionResult TriggerJogAxis3Minus()
        {
            var result = CNCUtils.TriggerSkinEvent(45);
            return result ? Ok(new { success = true, message = "Axis 3 jogged minus" })
                          : StatusCode(500, new { success = false, message = "Failed to trigger event" });
        }

        /// <summary>
        /// Start Axis 3 jog in minus direction (hold) (JOG_AXIS3_MINUS_UI)
        /// </summary>
        /// <param name="willRenew">If true, client must call RenewSkinEvent every 100ms</param>
        [HttpPost("StartJogAxis3Minus")]
        public IActionResult StartJogAxis3Minus([FromQuery] bool willRenew = false)
        {
            var result = CNCUtils.StartSkinEvent(45, willRenew);
            return result ? Ok(new { success = true, message = "Axis 3 jog minus started" })
                          : StatusCode(500, new { success = false, message = "Failed to start event" });
        }

        /// <summary>
        /// Stop Axis 3 jog in minus direction (release) (JOG_AXIS3_MINUS_UI)
        /// </summary>
        [HttpPost("StopJogAxis3Minus")]
        public IActionResult StopJogAxis3Minus()
        {
            var result = CNCUtils.StopSkinEvent(45);
            return result ? Ok(new { success = true, message = "Axis 3 jog minus stopped" })
                          : StatusCode(500, new { success = false, message = "Failed to stop event" });
        }

        #endregion

        #region Axis 4 Jog Controls

        /// <summary>
        /// Trigger Axis 4 jog in plus direction (JOG_AXIS4_PLUS_UI)
        /// </summary>
        [HttpPost("TriggerJogAxis4Plus")]
        public IActionResult TriggerJogAxis4Plus()
        {
            var result = CNCUtils.TriggerSkinEvent(31);
            return result ? Ok(new { success = true, message = "Axis 4 jogged plus" })
                          : StatusCode(500, new { success = false, message = "Failed to trigger event" });
        }

        /// <summary>
        /// Start Axis 4 jog in plus direction (hold) (JOG_AXIS4_PLUS_UI)
        /// </summary>
        /// <param name="willRenew">If true, client must call RenewSkinEvent every 100ms</param>
        [HttpPost("StartJogAxis4Plus")]
        public IActionResult StartJogAxis4Plus([FromQuery] bool willRenew = false)
        {
            var result = CNCUtils.StartSkinEvent(31, willRenew);
            return result ? Ok(new { success = true, message = "Axis 4 jog plus started" })
                          : StatusCode(500, new { success = false, message = "Failed to start event" });
        }

        /// <summary>
        /// Stop Axis 4 jog in plus direction (release) (JOG_AXIS4_PLUS_UI)
        /// </summary>
        [HttpPost("StopJogAxis4Plus")]
        public IActionResult StopJogAxis4Plus()
        {
            var result = CNCUtils.StopSkinEvent(31);
            return result ? Ok(new { success = true, message = "Axis 4 jog plus stopped" })
                          : StatusCode(500, new { success = false, message = "Failed to stop event" });
        }

        /// <summary>
        /// Trigger Axis 4 jog in minus direction (JOG_AXIS4_MINUS_UI)
        /// </summary>
        [HttpPost("TriggerJogAxis4Minus")]
        public IActionResult TriggerJogAxis4Minus()
        {
            var result = CNCUtils.TriggerSkinEvent(41);
            return result ? Ok(new { success = true, message = "Axis 4 jogged minus" })
                          : StatusCode(500, new { success = false, message = "Failed to trigger event" });
        }

        /// <summary>
        /// Start Axis 4 jog in minus direction (hold) (JOG_AXIS4_MINUS_UI)
        /// </summary>
        /// <param name="willRenew">If true, client must call RenewSkinEvent every 100ms</param>
        [HttpPost("StartJogAxis4Minus")]
        public IActionResult StartJogAxis4Minus([FromQuery] bool willRenew = false)
        {
            var result = CNCUtils.StartSkinEvent(41, willRenew);
            return result ? Ok(new { success = true, message = "Axis 4 jog minus started" })
                          : StatusCode(500, new { success = false, message = "Failed to start event" });
        }

        /// <summary>
        /// Stop Axis 4 jog in minus direction (release) (JOG_AXIS4_MINUS_UI)
        /// </summary>
        [HttpPost("StopJogAxis4Minus")]
        public IActionResult StopJogAxis4Minus()
        {
            var result = CNCUtils.StopSkinEvent(41);
            return result ? Ok(new { success = true, message = "Axis 4 jog minus stopped" })
                          : StatusCode(500, new { success = false, message = "Failed to stop event" });
        }

        #endregion

        #region Cycle Controls

        /// <summary>
        /// Trigger cycle start (START_CYCLE_UI)
        /// </summary>
        [HttpPost("TriggerCycleStart")]
        public IActionResult TriggerCycleStart()
        {
            var result = CNCUtils.TriggerSkinEvent(50);
            return result ? Ok(new { success = true, message = "Cycle start triggered" })
                          : StatusCode(500, new { success = false, message = "Failed to trigger event" });
        }

        /// <summary>
        /// Start cycle start button (hold) (START_CYCLE_UI)
        /// </summary>
        /// <param name="willRenew">If true, client must call RenewSkinEvent every 100ms</param>
        [HttpPost("StartCycleStart")]
        public IActionResult StartCycleStart([FromQuery] bool willRenew = false)
        {
            var result = CNCUtils.StartSkinEvent(50, willRenew);
            return result ? Ok(new { success = true, message = "Cycle start button pressed" })
                          : StatusCode(500, new { success = false, message = "Failed to start event" });
        }

        /// <summary>
        /// Stop cycle start button (release) (START_CYCLE_UI)
        /// </summary>
        [HttpPost("StopCycleStart")]
        public IActionResult StopCycleStart()
        {
            var result = CNCUtils.StopSkinEvent(50);
            return result ? Ok(new { success = true, message = "Cycle start button released" })
                          : StatusCode(500, new { success = false, message = "Failed to stop event" });
        }

        /// <summary>
        /// Trigger cycle cancel/stop (CANCEL_CYCLE_UI)
        /// </summary>
        [HttpPost("TriggerCycleCancel")]
        public IActionResult TriggerCycleCancel()
        {
            var result = CNCUtils.TriggerSkinEvent(46);
            return result ? Ok(new { success = true, message = "Cycle cancel triggered" })
                          : StatusCode(500, new { success = false, message = "Failed to trigger event" });
        }

        /// <summary>
        /// Start cycle cancel button (hold) (CANCEL_CYCLE_UI)
        /// </summary>
        /// <param name="willRenew">If true, client must call RenewSkinEvent every 100ms</param>
        [HttpPost("StartCycleCancel")]
        public IActionResult StartCycleCancel([FromQuery] bool willRenew = false)
        {
            var result = CNCUtils.StartSkinEvent(46, willRenew);
            return result ? Ok(new { success = true, message = "Cycle cancel button pressed" })
                          : StatusCode(500, new { success = false, message = "Failed to start event" });
        }

        /// <summary>
        /// Stop cycle cancel button (release) (CANCEL_CYCLE_UI)
        /// </summary>
        [HttpPost("StopCycleCancel")]
        public IActionResult StopCycleCancel()
        {
            var result = CNCUtils.StopSkinEvent(46);
            return result ? Ok(new { success = true, message = "Cycle cancel button released" })
                          : StatusCode(500, new { success = false, message = "Failed to stop event" });
        }

        #endregion

        #region Feed and Tool Controls

        /// <summary>
        /// Trigger feed hold (FEED_HOLD_UI)
        /// </summary>
        [HttpPost("TriggerFeedHold")]
        public IActionResult TriggerFeedHold()
        {
            var result = CNCUtils.TriggerSkinEvent(49);
            return result ? Ok(new { success = true, message = "Feed hold triggered" })
                          : StatusCode(500, new { success = false, message = "Failed to trigger event" });
        }

        /// <summary>
        /// Start feed hold button (hold) (FEED_HOLD_UI)
        /// </summary>
        /// <param name="willRenew">If true, client must call RenewSkinEvent every 100ms</param>
        [HttpPost("StartFeedHold")]
        public IActionResult StartFeedHold([FromQuery] bool willRenew = false)
        {
            var result = CNCUtils.StartSkinEvent(49, willRenew);
            return result ? Ok(new { success = true, message = "Feed hold button pressed" })
                          : StatusCode(500, new { success = false, message = "Failed to start event" });
        }

        /// <summary>
        /// Stop feed hold button (release) (FEED_HOLD_UI)
        /// </summary>
        [HttpPost("StopFeedHold")]
        public IActionResult StopFeedHold()
        {
            var result = CNCUtils.StopSkinEvent(49);
            return result ? Ok(new { success = true, message = "Feed hold button released" })
                          : StatusCode(500, new { success = false, message = "Failed to stop event" });
        }

        /// <summary>
        /// Trigger tool check (TOOL_CHECK_UI)
        /// </summary>
        [HttpPost("TriggerToolCheck")]
        public IActionResult TriggerToolCheck()
        {
            var result = CNCUtils.TriggerSkinEvent(48);
            return result ? Ok(new { success = true, message = "Tool check triggered" })
                          : StatusCode(500, new { success = false, message = "Failed to trigger event" });
        }

        /// <summary>
        /// Start tool check button (hold) (TOOL_CHECK_UI)
        /// </summary>
        /// <param name="willRenew">If true, client must call RenewSkinEvent every 100ms</param>
        [HttpPost("StartToolCheck")]
        public IActionResult StartToolCheck([FromQuery] bool willRenew = false)
        {
            var result = CNCUtils.StartSkinEvent(48, willRenew);
            return result ? Ok(new { success = true, message = "Tool check button pressed" })
                          : StatusCode(500, new { success = false, message = "Failed to start event" });
        }

        /// <summary>
        /// Stop tool check button (release) (TOOL_CHECK_UI)
        /// </summary>
        [HttpPost("StopToolCheck")]
        public IActionResult StopToolCheck()
        {
            var result = CNCUtils.StopSkinEvent(48);
            return result ? Ok(new { success = true, message = "Tool check button released" })
                          : StatusCode(500, new { success = false, message = "Failed to stop event" });
        }

        #endregion

        #region Machine Control

        /// <summary>
        /// Trigger stop button (STOP_UI)
        /// </summary>
        [HttpPost("Stop")]
        public IActionResult Stop()
        {
            var result = CNCUtils.TriggerSkinEvent(47);
            return result ? Ok(new { success = true, message = "Stop triggered" })
                          : StatusCode(500, new { success = false, message = "Failed to trigger stop" });
        }

        /// <summary>
        /// Trigger cycle cancel (CANCEL_CYCLE_UI) - cancels running program
        /// </summary>
        [HttpPost("CancelCycle")]
        public IActionResult CancelCycle()
        {
            var result = CNCUtils.TriggerSkinEvent(46);
            return result ? Ok(new { success = true, message = "Cycle cancelled" })
                          : StatusCode(500, new { success = false, message = "Failed to cancel cycle" });
        }

        /// <summary>
        /// Set machine homed state (MACHINE_HOMED_UI)
        /// </summary>
        /// <param name="isHomed">True to set machine as homed, false to set as unhomed</param>
        [HttpPost("SetMachineHomed")]
        public IActionResult SetMachineHomed([FromQuery] bool isHomed)
        {
            var cncPipe = CNCConnectionManager.GetCNCPipe();
            if (cncPipe == null)
            {
                return StatusCode(500, new { success = false, message = "CNC not connected" });
            }

            try
            {
                // SkinEvent 57 = MACHINE_HOMED_UI
                // Set state to 1 if homed, 0 if unhomed
                var result = cncPipe.plc.SetSkinEventState(57, isHomed ? 1 : 0);

                if (result == CentroidAPI.CNCPipe.ReturnCode.SUCCESS)
                {
                    // Update cached state
                    lock (_homedStateLock)
                    {
                        _machineHomedState = isHomed;
                    }
                    LogInfo($"Machine homed state set to: {isHomed}", "CNCUIController");

                    return Ok(new { success = true, message = $"Machine set to {(isHomed ? "homed" : "unhomed")}" });
                }
                else
                {
                    return StatusCode(500, new { success = false, message = $"Failed to set homed state: {result}" });
                }
            }
            catch (Exception ex)
            {
                LogError($"Error setting machine homed state: {ex.Message}", "CNCUIController");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Get the current machine homed state
        /// </summary>
        /// <returns>Object with isHomed property (true/false/null if unknown)</returns>
        [HttpGet("GetMachineHomedState")]
        public IActionResult GetMachineHomedState()
        {
            var homedState = GetCachedMachineHomedState();
            return Ok(new { isHomed = homedState });
        }

        /// <summary>
        /// Mark machine as homed (MACHINE_HOMED_UI)
        /// </summary>
        [HttpPost("MachineHomed")]
        public IActionResult MachineHomed()
        {
            return SetMachineHomed(true);
        }

        /// <summary>
        /// Mark machine as unhomed (MACHINE_HOMED_UI)
        /// </summary>
        [HttpPost("MachineUnhomed")]
        public IActionResult MachineUnhomed()
        {
            return SetMachineHomed(false);
        }

        /// <summary>
        /// Trigger reset (RESET_UI) - clears errors and resets machine state
        /// </summary>
        [HttpPost("Reset")]
        public IActionResult Reset()
        {
            var result = CNCUtils.TriggerSkinEvent(56);
            return result ? Ok(new { success = true, message = "Reset triggered" })
                          : StatusCode(500, new { success = false, message = "Failed to trigger reset" });
        }

        /// <summary>
        /// Trigger emergency stop (ESTOP_UI) - activates emergency stop condition
        /// </summary>
        [HttpPost("EStop")]
        public IActionResult EStop()
        {
            var result = CNCUtils.TriggerSkinEvent(58); // ESTOP_UI event
            return result ? Ok(new { success = true, message = "Emergency stop triggered" })
                          : StatusCode(500, new { success = false, message = "Failed to trigger emergency stop" });
        }

        #endregion

        #region Legacy Reset Control

        /// <summary>
        /// Trigger reset (RESET_UI) - Legacy endpoint, use /Reset instead
        /// </summary>
        [HttpPost("TriggerReset")]
        public IActionResult TriggerReset()
        {
            return Reset();
        }

        /// <summary>
        /// Start reset button (hold) (RESET_UI)
        /// </summary>
        /// <param name="willRenew">If true, client must call RenewSkinEvent every 100ms</param>
        [HttpPost("StartReset")]
        public IActionResult StartReset([FromQuery] bool willRenew = false)
        {
            var result = CNCUtils.StartSkinEvent(56, willRenew);
            return result ? Ok(new { success = true, message = "Reset button pressed" })
                          : StatusCode(500, new { success = false, message = "Failed to start event" });
        }

        /// <summary>
        /// Stop reset button (release) (RESET_UI)
        /// </summary>
        [HttpPost("StopReset")]
        public IActionResult StopReset()
        {
            var result = CNCUtils.StopSkinEvent(56);
            return result ? Ok(new { success = true, message = "Reset button released" })
                          : StatusCode(500, new { success = false, message = "Failed to stop event" });
        }

        #endregion
    }
}
