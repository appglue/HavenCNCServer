using Microsoft.AspNetCore.Mvc;
using HavenCNCServer.Centroid;
using HavenCNCServer.Models;
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
        /// <summary>
        /// Get system status including current time and CNC connection status
        /// </summary>
        /// <returns>System status information</returns>
        [HttpGet("Status")]
        public SystemStatus GetSystemStatus()
        {
            try
            {
                var status = new SystemStatus
                {
                    CurrentDateTime = DateTime.Now,
                    IsCNCConnected = CNCConnectionManager.IsConnected,
                    Status = CNCConnectionManager.IsConnected ? "CNC Connected" : "CNC Disconnected",
                    PlcVersion = GetInstalledPlcVersion()
                };

                return status;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get system status: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get the version number from the currently installed PLC source file
        /// </summary>
        /// <returns>PLC version string or empty if not found</returns>
        private string GetInstalledPlcVersion()
        {
            try
            {
                var cnc12Path = SettingsManager.Settings.Cnc.Cnc12Path;
                var plcSourcePath = System.IO.Path.Combine(cnc12Path, "havencncplc.src");

                if (!System.IO.File.Exists(plcSourcePath))
                {
                    return "Not Installed";
                }

                // Read first few lines to find version
                var lines = System.IO.File.ReadLines(plcSourcePath).Take(10).ToArray();

                // Look for line containing "Version:"
                foreach (var line in lines)
                {
                    if (line.Contains("Version:", StringComparison.OrdinalIgnoreCase))
                    {
                        // Extract version number after "Version:"
                        var versionIndex = line.IndexOf("Version:", StringComparison.OrdinalIgnoreCase);
                        if (versionIndex >= 0)
                        {
                            var version = line.Substring(versionIndex + 8).Trim();
                            return version;
                        }
                    }
                }

                return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        #region System Control

        /// <summary>
        /// Exit the CNC application
        /// </summary>
        [HttpPost("Exit")]
        public void Exit()
        {
            try
            {
                // TODO: Implement exit functionality using CentroidAPI
                // CNCUtils.Exit();
                throw new NotImplementedException("Exit functionality not yet implemented");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to exit: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Check if we are connected to Centroid
        /// </summary>
        /// <returns>True if connected, false otherwise</returns>
        [HttpGet("IsConnectedToCentroid")]
        public bool IsConnectedToCentroid()
        {
            return CNCConnectionManager.IsConnected;
        }

        /// <summary>
        /// Shutdown the system
        /// </summary>
        [HttpPost("Shutdown")]
        public void Shutdown()
        {
            try
            {
                // TODO: Implement shutdown functionality using CentroidAPI
                // CNCUtils.Shutdown();
                throw new NotImplementedException("Shutdown functionality not yet implemented");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to shutdown system: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Restart Centroid system
        /// </summary>
        [HttpPost("RestartCentroid")]
        public void RestartCentroid()
        {
            try
            {
                // TODO: Implement restart Centroid functionality using CentroidAPI
                // CNCUtils.RestartCentroid();
                throw new NotImplementedException("Restart Centroid functionality not yet implemented");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to restart Centroid: {ex.Message}", ex);
            }
        }

        #endregion

        #region Machine State Control

        /// <summary>
        /// Emergency stop
        /// </summary>
        [HttpPost("EmergencyStop")]
        public void EmergencyStop()
        {
            try
            {
                // TODO: Implement emergency stop functionality using CentroidAPI
                // CNCUtils.EmergencyStop();
                throw new NotImplementedException("Emergency stop functionality not yet implemented");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to perform emergency stop: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Check if machine is homed
        /// </summary>
        /// <returns>Machine homed status</returns>
        [HttpGet("IsHomed")]
        public bool IsHomed()
        {
            try
            {
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                    throw new InvalidOperationException("CNC connection not available");

                // Check each axis home status using GetPcSystemVariableBit
                cncPipe.plc.GetPcSystemVariableBit(CentroidAPI.PcToMpuSysVarBit.SV_HOME_SET_AXIS_1, out var state_axis_1);
                cncPipe.plc.GetPcSystemVariableBit(CentroidAPI.PcToMpuSysVarBit.SV_HOME_SET_AXIS_1, out var state_axis_2);
                cncPipe.plc.GetPcSystemVariableBit(CentroidAPI.PcToMpuSysVarBit.SV_HOME_SET_AXIS_1, out var state_axis_3);

                if (state_axis_1 != CentroidAPI.CNCPipe.Plc.IOState.IO_LOGICAL_1 || state_axis_2 != CentroidAPI.CNCPipe.Plc.IOState.IO_LOGICAL_1 || state_axis_3 != CentroidAPI.CNCPipe.Plc.IOState.IO_LOGICAL_1)
                {
                    //Machine not homed
                    return false;
                }

                cncPipe.plc.GetPcSystemVariableBit(CentroidAPI.PcToMpuSysVarBit.SV_PC_HOME_SET, out var state_home);

                if (state_home != CentroidAPI.CNCPipe.Plc.IOState.IO_LOGICAL_1)
                {
                    //Machine not homed
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to check if machine is homed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Enable soft limits (travel limits)
        /// </summary>
        [HttpPost("EnableSoftLimits")]
        public IActionResult EnableSoftLimits()
        {
            try
            {
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                    throw new InvalidOperationException("CNC connection not available");

                CNCUtils.StartSkinEvent(SkinEvent.LimitDefeat);
                System.Threading.Thread.Sleep(100);
                CNCUtils.StopSkinEvent(SkinEvent.LimitDefeat);

                return Ok(new { message = "Soft limits enabled" });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to enable soft limits: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disable soft limits (travel limits)
        /// </summary>
        [HttpPost("DisableSoftLimits")]
        public IActionResult DisableSoftLimits()
        {
            try
            {
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                    throw new InvalidOperationException("CNC connection not available");

                CNCUtils.StartSkinEvent(SkinEvent.LimitDefeat);
                System.Threading.Thread.Sleep(100);
                CNCUtils.StopSkinEvent(SkinEvent.LimitDefeat);

                return Ok(new { message = "Soft limits disabled" });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to disable soft limits: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get current error state
        /// </summary>
        /// <returns>Current error messages</returns>
        [HttpGet("GetCurrentErrorState")]
        public string[] GetCurrentErrorState()
        {
            try
            {
                // TODO: Implement get current error state functionality using CentroidAPI
                // return CNCUtils.GetCurrentErrorState();
                throw new NotImplementedException("Get current error state functionality not yet implemented");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get current error state: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Check if machine has current errors
        /// </summary>
        /// <returns>Has errors status</returns>
        [HttpGet("HasCurrentErrors")]
        public bool HasCurrentErrors()
        {
            try
            {
                // TODO: Implement has current errors check functionality using CentroidAPI
                // return CNCUtils.HasCurrentErrors();
                throw new NotImplementedException("Has current errors check functionality not yet implemented");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to check for current errors: {ex.Message}", ex);
            }
        }

        #endregion
    }
}
