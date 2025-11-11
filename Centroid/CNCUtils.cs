using System;
using System.Linq;
using CentroidAPI;
using HavenCNCServer.Services;
using HavenCNCServer.Centroid.Data;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Centroid
{
    /// <summary>
    /// Clean CNC12 API wrapper class using centralized CNCConnectionManager.
    /// Provides stateless utility methods for CNC parameter operations.
    /// All methods get CNCPipe from CNCConnectionManager automatically.
    /// </summary>
    public static class CNCUtils
    {
        /// <summary>
        /// Default skin event number for reset button operations
        /// Acorn uses event 51 for Reset button (toggle type)
        /// </summary>
        public const int DEFAULT_CYCLE_START_EVENT = 56;


        /// <summary>
        /// Get a CNC12 parameter value using CentroidParameters enum
        /// </summary>
        /// <param name="parameter">CentroidParameters enum value</param>
        /// <returns>Parameter value as double</returns>
        public static double GetParameterValue(CentroidParameters parameter)
        {
            var cncPipe = CNCConnectionManager.GetCNCPipe();
            if (cncPipe == null)
                throw new InvalidOperationException("CNC connection not available. Ensure CNCConnectionManager is connected.");

            try
            {
                // Use the parameter property of CNCPipe to access parameter methods
                CNCPipe.ReturnCode returnCode = cncPipe.parameter.GetMachineParameterValue((int)parameter, out double value);

                if (returnCode != CNCPipe.ReturnCode.SUCCESS)
                {
                    throw new InvalidOperationException($"Failed to get parameter {parameter}: Return code {returnCode}");
                }

                return value;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get parameter {parameter}: {ex.Message}", ex);
            }
        }



        /// <summary>
        /// Set a CNC12 parameter value using CentroidParameters enum
        /// </summary>
        /// <param name="parameter">CentroidParameters enum value</param>
        /// <param name="value">Value to set</param>
        public static void SetParameterValue(CentroidParameters parameter, double value)
        {
            var cncPipe = CNCConnectionManager.GetCNCPipe();
            if (cncPipe == null)
                throw new InvalidOperationException("CNC connection not available. Ensure CNCConnectionManager is connected.");

            try
            {
                CNCPipe.ReturnCode returnCode = cncPipe.parameter.SetMachineParameter((int)parameter, value);

                if (returnCode == CNCPipe.ReturnCode.STATUS_UNKNOWN)
                {
                    throw new InvalidOperationException($"Failed to set parameter {parameter}: Status unknown - parameter may be read-only or invalid");
                }
                else if (returnCode != CNCPipe.ReturnCode.SUCCESS)
                {
                    throw new InvalidOperationException($"Failed to set parameter {parameter} to {value}: Return code {returnCode}");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to set parameter {parameter} to {value}: {ex.Message}", ex);
            }
        }





        /// <summary>
        /// Get a workpiece reference point (G28, G30, etc.)
        /// </summary>
        /// <param name="reference">Reference point (G28=0, G30=1, G30P3=2, G30P4=3)</param>
        /// <param name="axis">Axis number (1=X, 2=Y, 3=Z, etc.)</param>
        /// <returns>Reference point value</returns>
        public static double GetWorkpieceReferencePoint(ReferencePoints reference, int axis)
        {
            var cncPipe = CNCConnectionManager.GetCNCPipe();
            if (cncPipe == null)
                throw new InvalidOperationException("CNC connection not available. Ensure CNCConnectionManager is connected.");

            try
            {
                // CentroidAPI uses 1-based indexing: G28=1, G30=2, G30P3=3, G30P4=4
                int referenceIndex = (int)reference + 1;
                cncPipe.wcs.GetWorkpieceReference(referenceIndex, axis, out double value);
                return value;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get workpiece reference point {reference} axis {axis}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Set a workpiece reference point (G28, G30, etc.)
        /// </summary>
        /// <param name="reference">Reference point (G28=0, G30=1, G30P3=2, G30P4=3)</param>
        /// <param name="axis">Axis number (1=X, 2=Y, 3=Z, etc.)</param>
        /// <param name="point">Value to set</param>
        public static void SetWorkpieceReferencePoint(ReferencePoints reference, int axis, double point)
        {
            var cncPipe = CNCConnectionManager.GetCNCPipe();
            if (cncPipe == null)
                throw new InvalidOperationException("CNC connection not available. Ensure CNCConnectionManager is connected.");

            try
            {
                // CentroidAPI uses 1-based indexing: G28=1, G30=2, G30P3=3, G30P4=4
                int referenceIndex = (int)reference + 1;
                cncPipe.wcs.SetWorkpieceReference(referenceIndex, axis, point);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to set workpiece reference point {reference} axis {axis} to {point}: {ex.Message}", ex);
            }
        }



        /// <summary>
        /// Check if a specific bit is set in an integer value
        /// </summary>
        /// <param name="num">Integer value to check</param>
        /// <param name="bitnum">Bit number (0-31)</param>
        /// <returns>True if bit is set, false otherwise</returns>
        public static bool IsBitSet(int num, int bitnum)
        {
            if (bitnum < 0 || bitnum > 31)
                throw new ArgumentOutOfRangeException(nameof(bitnum), "Bit number must be between 0 and 31");

            return (num & (1 << bitnum)) != 0;
        }

        /// <summary>
        /// Modify a specific bit in an integer value
        /// </summary>
        /// <param name="num">Original integer value</param>
        /// <param name="bitnum">Bit number to modify (0-31)</param>
        /// <param name="value">True to set bit, false to clear bit</param>
        /// <returns>Modified integer value</returns>
        public static int ModifyBit(int num, int bitnum, bool value)
        {
            if (bitnum < 0 || bitnum > 31)
                throw new ArgumentOutOfRangeException(nameof(bitnum), "Bit number must be between 0 and 31");

            if (value)
            {
                // Set the bit
                return num | (1 << bitnum);
            }
            else
            {
                // Clear the bit
                return num & ~(1 << bitnum);
            }
        }

        /// <summary>
        /// Get PWM frequency parameter value for specific output
        /// </summary>
        /// <param name="outputNumber">PWM output number (1-3)</param>
        /// <returns>Frequency value</returns>
        public static double GetPWMFrequency(int outputNumber)
        {
            var parameter = outputNumber switch
            {
                1 => CentroidParameters.ACORN_PWM_FREQUENCY_PARM,
                2 => CentroidParameters.ACORN_PWM_FREQUENCY_PARM_2,
                3 => CentroidParameters.ACORN_PWM_FREQUENCY_PARM_3,
                _ => throw new ArgumentOutOfRangeException(nameof(outputNumber), "PWM output number must be 1-3")
            };
            return GetParameterValue(parameter);
        }

        /// <summary>
        /// Set PWM frequency parameter value for specific output
        /// </summary>
        /// <param name="outputNumber">PWM output number (1-3)</param>
        /// <param name="frequency">Frequency value to set</param>
        public static void SetPWMFrequency(int outputNumber, double frequency)
        {
            var parameter = outputNumber switch
            {
                1 => CentroidParameters.ACORN_PWM_FREQUENCY_PARM,
                2 => CentroidParameters.ACORN_PWM_FREQUENCY_PARM_2,
                3 => CentroidParameters.ACORN_PWM_FREQUENCY_PARM_3,
                _ => throw new ArgumentOutOfRangeException(nameof(outputNumber), "PWM output number must be 1-3")
            };
            SetParameterValue(parameter, frequency);
        }

        /// <summary>
        /// Get PWM options parameter value for specific output
        /// </summary>
        /// <param name="outputNumber">PWM output number (1-3)</param>
        /// <returns>Options value</returns>
        public static double GetPWMOptions(int outputNumber)
        {
            var parameter = outputNumber switch
            {
                1 => CentroidParameters.ACORN_PWM_OPTIONS_PARM,
                2 => CentroidParameters.ACORN_PWM_OPTIONS_PARM_2,
                3 => CentroidParameters.ACORN_PWM_OPTIONS_PARM_3,
                _ => throw new ArgumentOutOfRangeException(nameof(outputNumber), "PWM output number must be 1-3")
            };
            return GetParameterValue(parameter);
        }

        /// <summary>
        /// Set PWM options parameter value for specific output
        /// </summary>
        /// <param name="outputNumber">PWM output number (1-3)</param>
        /// <param name="options">Options value to set</param>
        public static void SetPWMOptions(int outputNumber, double options)
        {
            var parameter = outputNumber switch
            {
                1 => CentroidParameters.ACORN_PWM_OPTIONS_PARM,
                2 => CentroidParameters.ACORN_PWM_OPTIONS_PARM_2,
                3 => CentroidParameters.ACORN_PWM_OPTIONS_PARM_3,
                _ => throw new ArgumentOutOfRangeException(nameof(outputNumber), "PWM output number must be 1-3")
            };
            SetParameterValue(parameter, options);
        }

        /// <summary>
        /// Get PWM floor parameter value for specific output
        /// </summary>
        /// <param name="outputNumber">PWM output number (1-3)</param>
        /// <returns>Floor value</returns>
        public static double GetPWMFloor(int outputNumber)
        {
            var parameter = outputNumber switch
            {
                1 => CentroidParameters.ACORN_PWM_FLOOR_PARM,
                2 => CentroidParameters.ACORN_PWM_FLOOR_PARM_2,
                3 => CentroidParameters.ACORN_PWM_FLOOR_PARM_3,
                _ => throw new ArgumentOutOfRangeException(nameof(outputNumber), "PWM output number must be 1-3")
            };
            return GetParameterValue(parameter);
        }

        /// <summary>
        /// Set PWM floor parameter value for specific output
        /// </summary>
        /// <param name="outputNumber">PWM output number (1-3)</param>
        /// <param name="floor">Floor value to set</param>
        public static void SetPWMFloor(int outputNumber, double floor)
        {
            var parameter = outputNumber switch
            {
                1 => CentroidParameters.ACORN_PWM_FLOOR_PARM,
                2 => CentroidParameters.ACORN_PWM_FLOOR_PARM_2,
                3 => CentroidParameters.ACORN_PWM_FLOOR_PARM_3,
                _ => throw new ArgumentOutOfRangeException(nameof(outputNumber), "PWM output number must be 1-3")
            };
            SetParameterValue(parameter, floor);
        }

        /// <summary>
        /// Get the current global step frequency for all axes
        /// </summary>
        /// <returns>Step frequency in steps per second</returns>
        public static int GetStepFrequency()
        {
            const int PulseStepFrequency = 1200000;
            double paramValue = GetParameterValue(CentroidParameters.ACORN_STEPPER_PULSE_RATE_PARM);

            // If parameter is 0, default is 200,000 steps/second
            if (paramValue == 0)
                return 200000;

            return (int)(PulseStepFrequency / paramValue);
        }

        /// <summary>
        /// Get the current global step frequency as enum value
        /// </summary>
        /// <returns>Step frequency enum value</returns>
        public static StepFrequency GetStepFrequencyEnum()
        {
            int frequency = GetStepFrequency();
            return (StepFrequency)frequency;
        }

        /// <summary>
        /// Set the global step frequency for all axes
        /// Supported frequencies: 100000, 200000, 240000, 300000, 400000 steps/second
        /// </summary>
        /// <param name="frequency">Step frequency in steps per second</param>
        public static void SetStepFrequency(int frequency)
        {
            const int PulseStepFrequency = 1200000;

            // Validate supported frequencies
            var supportedFrequencies = new[] { 100000, 200000, 240000, 300000, 400000 };
            if (!supportedFrequencies.Contains(frequency))
            {
                throw new ArgumentException($"Unsupported step frequency: {frequency}. Supported values: {string.Join(", ", supportedFrequencies)}");
            }

            double paramValue = PulseStepFrequency / (double)frequency;
            SetParameterValue(CentroidParameters.ACORN_STEPPER_PULSE_RATE_PARM, paramValue);
        }

        /// <summary>
        /// Set the global step frequency using enum value
        /// </summary>
        /// <param name="frequency">Step frequency enum value</param>
        public static void SetStepFrequency(StepFrequency frequency)
        {
            SetStepFrequency((int)frequency);
        }

        /// <summary>
        /// Get all available input port numbers for the current CNC system
        /// Detects system type and expansion boards to calculate exact I/O numbering
        /// </summary>
        /// <returns>Array of available input port numbers</returns>
        public static int[] GetAvailableInputPorts()
        {
            var cncPipe = CNCConnectionManager.GetCNCPipe();
            if (cncPipe == null)
                throw new InvalidOperationException("CNC connection not available. Ensure CNCConnectionManager is connected.");

            var availableInputs = new List<int>();

            try
            {
                // Get system type to determine I/O layout
                cncPipe.system.GetUnlockVersion(out CNCPipe.Sys.UnlockVersions unlockVersion);

                // Determine system-specific configuration
                string systemName = unlockVersion.ToString();
                bool isAcorn = systemName.Contains("ACORN") && !systemName.Contains("ACORN_SIX");
                bool isAcornSix = systemName.Contains("ACORN_SIX");
                bool isHickory = systemName.Contains("HICKORY");
                bool isProRouter = systemName.Contains("PRO_ROUTER");

                LogInfo($"Detecting available I/O - System: {unlockVersion} (Acorn:{isAcorn}, AcornSix:{isAcornSix}, Hickory:{isHickory}, ProRouter:{isProRouter})", "CNCUtils");

                // Platform-specific I/O configuration
                int baseInputCount;
                int expansionStartIO;

                if (isAcorn || isProRouter)
                {
                    // Acorn: Base I/O is 1-8, Ether1616 expansion starts at 33
                    baseInputCount = 8;
                    expansionStartIO = 33;
                }
                else if (isAcornSix)
                {
                    // AcornSix: Base I/O is 1-16, PLCEXP1616 expansion starts at 65
                    baseInputCount = 16;
                    expansionStartIO = 65;
                }
                else if (isHickory)
                {
                    // Hickory: Base I/O is 1-32, ECAT1616 expansion starts at 129
                    baseInputCount = 32;
                    expansionStartIO = 129;
                }
                else
                {
                    // Unknown platform - use conservative defaults
                    baseInputCount = 16;
                    expansionStartIO = 65;
                }

                // Add base inputs
                for (int i = 1; i <= baseInputCount; i++)
                {
                    availableInputs.Add(i);
                }

                LogInfo($"Added base I/O: 1-{baseInputCount}", "CNCUtils");

                // Detect expansion boards
                int expansionCount = 0;

                if (isAcorn || isProRouter)
                {
                    var result = cncPipe.system.GetEther1616DeviceInfo(out List<CNCPipe.Sys.Ether1616Device> devices);
                    LogInfo($"GetEther1616DeviceInfo returned: {result}, devices: {devices?.Count ?? 0}", "CNCUtils");

                    if (result == CNCPipe.ReturnCode.SUCCESS && devices != null && devices.Count > 0)
                    {
                        // Filter out invalid/disconnected devices
                        var validDevices = devices.Where(d => d.DeviceNumber.HasValue && d.DeviceNumber.Value >= 0).ToList();
                        expansionCount = validDevices.Count;

                        LogInfo($"Detected {devices.Count} Ether1616 devices, {expansionCount} valid", "CNCUtils");

                        // Log all device details for debugging
                        for (int i = 0; i < devices.Count; i++)
                        {
                            var device = devices[i];
                            bool isValid = device.DeviceNumber.HasValue && device.DeviceNumber.Value >= 0;
                            LogInfo($"  Device {i}: DeviceNumber={device.DeviceNumber}, IP={device.IP}, Valid={isValid}", "CNCUtils");
                        }
                    }
                    else
                    {
                        LogWarning($"GetEther1616DeviceInfo: result={result}, devices={devices?.Count ?? 0}", "CNCUtils");
                        expansionCount = 0;
                    }
                }
                else if (isAcornSix)
                {
                    var result = cncPipe.system.GetPLCEXP1616NumberofDevices(out expansionCount);
                    if (result == CNCPipe.ReturnCode.SUCCESS)
                    {
                        LogInfo($"Detected {expansionCount} PLCEXP1616 expansion boards", "CNCUtils");
                    }
                    else
                    {
                        LogWarning($"GetPLCEXP1616NumberofDevices failed: {result}", "CNCUtils");
                        expansionCount = 0;
                    }
                }
                else if (isHickory)
                {
                    var result = cncPipe.system.GetECAT1616NumberOfDevices(out expansionCount);
                    if (result == CNCPipe.ReturnCode.SUCCESS)
                    {
                        LogInfo($"Detected {expansionCount} ECAT1616 expansion boards", "CNCUtils");
                    }
                    else
                    {
                        LogWarning($"GetECAT1616NumberOfDevices failed: {result}", "CNCUtils");
                        expansionCount = 0;
                    }
                }

                // Add expansion board I/O
                // Each expansion board provides 16 I/O
                if (expansionCount > 0)
                {
                    for (int board = 0; board < expansionCount; board++)
                    {
                        int startIO = expansionStartIO + (board * 16);
                        int endIO = startIO + 15;
                        LogInfo($"Adding expansion board {board + 1} I/O: {startIO}-{endIO}", "CNCUtils");

                        for (int i = 0; i < 16; i++)
                        {
                            availableInputs.Add(startIO + i);
                        }
                    }
                }

                LogInfo($"Total available I/O ports: {availableInputs.Count} ({string.Join(", ", availableInputs.Take(5))}...{string.Join(", ", availableInputs.Skip(Math.Max(0, availableInputs.Count - 5)))})", "CNCUtils");

                return availableInputs.ToArray();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get available input ports: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get all available output port numbers for the current CNC system
        /// Detects system type and expansion boards to calculate exact I/O numbering
        /// Note: Output numbering follows identical pattern to inputs
        /// </summary>
        /// <returns>Array of available output port numbers</returns>
        public static int[] GetAvailableOutputPorts()
        {
            // Output numbering follows identical pattern to inputs
            // Use the same detection logic as GetAvailableInputPorts()
            return GetAvailableInputPorts();
        }

        /// <summary>
        /// Check if a specific input port number is available on the current system
        /// </summary>
        /// <param name="inputNumber">Input port number to check</param>
        /// <returns>True if input is available, false otherwise</returns>
        public static bool IsInputAvailable(int inputNumber)
        {
            int[] availableInputs = GetAvailableInputPorts();
            return Array.IndexOf(availableInputs, inputNumber) >= 0;
        }

        /// <summary>
        /// Check if a specific output port number is available on the current system
        /// </summary>
        /// <param name="outputNumber">Output port number to check</param>
        /// <returns>True if output is available, false otherwise</returns>
        public static bool IsOutputAvailable(int outputNumber)
        {
            int[] availableOutputs = GetAvailableOutputPorts();
            return Array.IndexOf(availableOutputs, outputNumber) >= 0;
        }

        /// <summary>
        /// Get comprehensive information about the current CNC system's I/O configuration
        /// </summary>
        /// <returns>String describing the system type and I/O capabilities</returns>
        public static string GetSystemInfo()
        {
            var cncPipe = CNCConnectionManager.GetCNCPipe();
            if (cncPipe == null)
                throw new InvalidOperationException("CNC connection not available. Ensure CNCConnectionManager is connected.");

            try
            {
                cncPipe.system.GetUnlockVersion(out CNCPipe.Sys.UnlockVersions unlockVersion);

                string systemType = "Unknown";
                int baseInputs = 8;
                int baseOutputs = 8;
                int expansionInputs = 0;
                int expansionOutputs = 0;

                bool isAcorn = unlockVersion.ToString().Contains("ACORN") && !unlockVersion.ToString().Contains("ACORN_SIX");
                bool isAcornSix = unlockVersion.ToString().Contains("ACORN_SIX");
                bool isHickory = unlockVersion.ToString().Contains("HICKORY");

                if (isAcorn)
                {
                    systemType = "Acorn";
                    cncPipe.system.GetEther1616DeviceInfo(out List<CNCPipe.Sys.Ether1616Device> devices);
                    int expansionCount = devices?.Count ?? 0;
                    expansionInputs = expansionCount * 16;
                    expansionOutputs = expansionCount * 16;
                }
                else if (isAcornSix)
                {
                    systemType = "AcornSix";
                    baseInputs = 16;
                    baseOutputs = 16;
                    cncPipe.system.GetPLCEXP1616NumberofDevices(out int expansionCount);
                    expansionInputs = expansionCount * 16;
                    expansionOutputs = expansionCount * 16;
                }
                else if (isHickory)
                {
                    systemType = "Hickory";
                    baseInputs = 32;
                    baseOutputs = 32;
                    cncPipe.system.GetECAT1616NumberOfDevices(out int expansionCount);
                    expansionInputs = expansionCount * 16;
                    expansionOutputs = expansionCount * 16;
                }

                int totalInputs = baseInputs + expansionInputs;
                int totalOutputs = baseOutputs + expansionOutputs;

                return $"{systemType}: {totalInputs} inputs ({baseInputs} base + {expansionInputs} expansion), " +
                       $"{totalOutputs} outputs ({baseOutputs} base + {expansionOutputs} expansion)";
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get system information: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get second spindle maximum speed
        /// </summary>
        /// <summary>
        /// Enable or disable second spindle
        /// </summary>
        /// <param name="enabled">True to enable, false to disable</param>
        public static void SetSecondSpindleEnabled(bool enabled)
        {
            SetParameterValue(CentroidParameters.SECOND_SPINDLE_ENABLE, enabled ? 1 : 0);
        }

        /// <summary>
        /// Enable or disable enhanced ATC
        /// </summary>
        /// <param name="enabled">True to enable, false to disable</param>
        public static void SetEnhancedATCEnabled(bool enabled)
        {
            SetParameterValue(CentroidParameters.ENHANCED_ATC_PARM, enabled ? 1 : 0);
        }

        /// <summary>
        /// Check if gang tool is enabled
        /// </summary>
        /// <returns>True if gang tool is enabled</returns>
        public static bool IsGangToolEnabled()
        {
            return IsBitSet((int)GetParameterValue(CentroidParameters.GANG_TOOL_ENABLE), 0);
        }

        /// <summary>
        /// Enable or disable gang tool
        /// </summary>
        /// <param name="enabled">True to enable, false to disable</param>
        public static void SetGangToolEnabled(bool enabled)
        {
            int currentValue = (int)GetParameterValue(CentroidParameters.GANG_TOOL_ENABLE);
            int newValue = ModifyBit(currentValue, 0, enabled);
            SetParameterValue(CentroidParameters.GANG_TOOL_ENABLE, newValue);
        }

        /// <summary>
        /// Set axis rate for specific axis and rate type
        /// </summary>
        /// <param name="axis">Axis number (1-based)</param>
        /// <param name="rateType">Type of rate to set</param>
        /// <param name="value">Rate value</param>
        public static void SetAxisRate(int axis, CNCPipe.Axis.Rate rateType, double value)
        {
            var cncPipe = CNCConnectionManager.GetCNCPipe();
            if (cncPipe == null)
                throw new InvalidOperationException("CNC connection not available. Ensure CNCConnectionManager is connected.");

            var axisEnum = (CNCPipe.Axes)axis;
            cncPipe.axis.SetRate(axisEnum, rateType, value);
        }

        /// <summary>
        /// Get axis rate for specific axis and rate type
        /// </summary>
        /// <param name="axis">Axis number (1-based)</param>
        /// <param name="rateType">Type of rate to get</param>
        /// <returns>Rate value</returns>
        public static double GetAxisRate(int axis, CNCPipe.Axis.Rate rateType)
        {
            var cncPipe = CNCConnectionManager.GetCNCPipe();
            if (cncPipe == null)
                throw new InvalidOperationException("CNC connection not available. Ensure CNCConnectionManager is connected.");

            var axisEnum = (CNCPipe.Axes)axis;
            cncPipe.axis.GetRate(axisEnum, rateType, out double value);
            return value;
        }

        /// <summary>
        /// Check if Centroid is in a state that can accept API calls and attempt to reset if needed.
        /// This checks both the general API restriction state and the SV_STOP system variable.
        /// If SV_STOP is set, it will attempt to clear it by toggling the reset button skin event twice.
        /// The reset button is a toggle - must press once to trip reset, then again to clear.
        /// </summary>
        /// <param name="skinEventNumber">Skin event number to trigger for reset (default from DEFAULT_CYCLE_START_EVENT constant)</param>
        /// <returns>True if Centroid is ready or was successfully reset, false otherwise</returns>
        public static bool CheckAndResetCentroidState(int skinEventNumber = DEFAULT_CYCLE_START_EVENT)
        {
            var cncPipe = CNCConnectionManager.GetCNCPipe();
            if (cncPipe == null)
                throw new InvalidOperationException("CNC connection not available. Ensure CNCConnectionManager is connected.");

            try
            {
                // Check if API is generally restricted
                var apiCheckResult = cncPipe.state.IsAPIRestricted(out bool apiRestricted);
                if (apiCheckResult != CNCPipe.ReturnCode.SUCCESS)
                {
                    LogWarning($"Failed to check API restriction state: {apiCheckResult}", "CNCUtils");
                    return false;
                }

                // Check SV_STOP system variable via PLC
                // SV_STOP is a predefined PLC system variable that indicates the machine is in a stopped/fault state
                var svStopResult = cncPipe.plc.GetPlcSystemVariableBit(CentroidAPI.MpuToPcSysVarBit.SV_STOP, out CNCPipe.Plc.IOState svStopState);
                if (svStopResult != CNCPipe.ReturnCode.SUCCESS)
                {
                    LogWarning($"Failed to check SV_STOP state: {svStopResult}", "CNCUtils");
                    return false;
                }

                bool svStopActive = (svStopState == CNCPipe.Plc.IOState.IO_LOGICAL_1);

                // If API is restricted for general reasons (e.g., in middle of cycle, editing, etc.)
                // We can't do much about it, so return false
                if (apiRestricted)
                {
                    LogWarning("API is restricted - cannot accept commands", "CNCUtils");
                    return false;
                }

                // If SV_STOP is active, attempt to reset by triggering reset button skin event
                // The reset button is a TOGGLE, not momentary - must press twice:
                //   First press: trips the reset (clears fault)
                //   Second press: clears the reset state
                if (svStopActive)
                {
                    LogInfo($"🔄 SV_STOP is active, attempting reset with skin event {skinEventNumber}", "CNCUtils");

                    // Try up to 2 attempts to clear SV_STOP
                    for (int attempt = 1; attempt <= 2; attempt++)
                    {
                        // FIRST TOGGLE: Press reset button (set to 1, then back to 0)
                        // This trips the reset and clears the fault
                        LogInfo($"   Attempt {attempt}: Pressing reset button (toggle 1/2)...", "CNCUtils");
                        var resetPressResult = cncPipe.plc.SetSkinEventState(skinEventNumber, 1);
                        if (resetPressResult != CNCPipe.ReturnCode.SUCCESS)
                        {
                            LogError($"   Attempt {attempt}: Failed to press reset button (set to 1): {resetPressResult}", "CNCUtils");
                            return false;
                        }

                        System.Threading.Thread.Sleep(100);

                        var resetReleaseResult = cncPipe.plc.SetSkinEventState(skinEventNumber, 0);
                        if (resetReleaseResult != CNCPipe.ReturnCode.SUCCESS)
                        {
                            LogWarning($"   Attempt {attempt}: Failed to release reset button (set to 0): {resetReleaseResult}", "CNCUtils");
                        }

                        System.Threading.Thread.Sleep(100);

                        // SECOND TOGGLE: Press reset button again (set to 1, then back to 0)
                        // This clears the reset state and returns to normal operation
                        LogInfo($"   Attempt {attempt}: Pressing reset button again (toggle 2/2)...", "CNCUtils");
                        var resetClearPressResult = cncPipe.plc.SetSkinEventState(skinEventNumber, 1);
                        if (resetClearPressResult != CNCPipe.ReturnCode.SUCCESS)
                        {
                            LogWarning($"   Attempt {attempt}: Failed to press reset button second time (set to 1): {resetClearPressResult}", "CNCUtils");
                        }

                        System.Threading.Thread.Sleep(100);

                        var resetClearReleaseResult = cncPipe.plc.SetSkinEventState(skinEventNumber, 0);
                        if (resetClearReleaseResult != CNCPipe.ReturnCode.SUCCESS)
                        {
                            LogWarning($"   Attempt {attempt}: Failed to release reset button second time (set to 0): {resetClearReleaseResult}", "CNCUtils");
                        }

                        System.Threading.Thread.Sleep(100);

                        // Check if SV_STOP was cleared
                        svStopResult = cncPipe.plc.GetPlcSystemVariableBit(CentroidAPI.MpuToPcSysVarBit.SV_STOP, out svStopState);
                        svStopActive = (svStopState == CNCPipe.Plc.IOState.IO_LOGICAL_1);

                        if (!svStopActive)
                        {
                            LogInfo($"✅ Successfully cleared SV_STOP on attempt {attempt}", "CNCUtils");
                            break;
                        }

                        if (attempt < 2)
                        {
                            LogWarning($"   SV_STOP still active after attempt {attempt}, retrying...", "CNCUtils");
                        }
                    }

                    if (svStopResult == CNCPipe.ReturnCode.SUCCESS && !svStopActive)
                    {
                        LogInfo("✅ Successfully reset SV_STOP state", "CNCUtils");

                        // Verify API is not restricted after reset
                        apiCheckResult = cncPipe.state.IsAPIRestricted(out apiRestricted);
                        if (apiCheckResult == CNCPipe.ReturnCode.SUCCESS && !apiRestricted)
                        {
                            LogInfo("✅ API is ready after reset", "CNCUtils");
                            return true;
                        }

                        LogWarning("SV_STOP cleared but API still restricted", "CNCUtils");
                        return false;
                    }

                    LogError("❌ SV_STOP still active after all reset attempts", "CNCUtils");
                    return false;
                }

                // API is not restricted and SV_STOP is not active - ready to accept commands
                LogInfo("✅ CNC is ready - API not restricted, SV_STOP not active", "CNCUtils");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error checking/resetting Centroid state: {ex.Message}", "CNCUtils");
                return false;
            }
        }

        /// <summary>
        /// Check if Centroid is ready to accept API calls without attempting reset
        /// </summary>
        /// <returns>True if ready, false otherwise</returns>
        public static bool IsCentroidReady()
        {
            var cncPipe = CNCConnectionManager.GetCNCPipe();
            if (cncPipe == null)
                return false;

            try
            {
                // Check API restriction
                var apiCheckResult = cncPipe.state.IsAPIRestricted(out bool apiRestricted);
                if (apiCheckResult != CNCPipe.ReturnCode.SUCCESS || apiRestricted)
                {
                    return false;
                }

                // Check SV_STOP using GetPlcSystemVariableBit
                var svStopResult = cncPipe.plc.GetPlcSystemVariableBit(CentroidAPI.MpuToPcSysVarBit.SV_STOP, out CNCPipe.Plc.IOState svStopState);
                if (svStopResult != CNCPipe.ReturnCode.SUCCESS)
                {
                    LogWarning($"IsCentroidReady: Failed to check SV_STOP: {svStopResult}", "CNCUtils");
                    return false;
                }

                bool svStopActive = (svStopState == CNCPipe.Plc.IOState.IO_LOGICAL_1);
                if (svStopActive)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get travel limits for a specific axis
        /// </summary>
        /// <param name="axisNumber">Axis number (1-8)</param>
        /// <param name="plusLimit">Plus travel limit</param>
        /// <param name="minusLimit">Minus travel limit</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool GetAxisTravelLimits(int axisNumber, out double plusLimit, out double minusLimit)
        {
            plusLimit = 0;
            minusLimit = 0;

            var cncPipe = CNCConnectionManager.GetCNCPipe();
            if (cncPipe == null)
            {
                LogError("CNC connection not available", "CNCUtils");
                return false;
            }

            try
            {
                // Convert axis number to CNCPipe.Axes enum
                var axis = (CNCPipe.Axes)(axisNumber - 1);

                // Get plus limit
                var plusResult = cncPipe.axis.GetTravelLimit(axis, CNCPipe.Axis.Direction.PLUS, out plusLimit);
                if (plusResult != CNCPipe.ReturnCode.SUCCESS)
                {
                    LogError($"Failed to get plus travel limit for axis {axisNumber}: {plusResult}", "CNCUtils");
                    return false;
                }

                // Get minus limit
                var minusResult = cncPipe.axis.GetTravelLimit(axis, CNCPipe.Axis.Direction.MINUS, out minusLimit);
                if (minusResult != CNCPipe.ReturnCode.SUCCESS)
                {
                    LogError($"Failed to get minus travel limit for axis {axisNumber}: {minusResult}", "CNCUtils");
                    return false;
                }

                // Log the raw values from the API to diagnose coordinate system issues
                string axisLabel = GetAxisLabel(axisNumber);
                LogInfo($"Travel limits for Axis {axisNumber} ({axisLabel}): Plus={plusLimit:F4}, Minus={minusLimit:F4}", "CNCUtils");

                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error getting travel limits for axis {axisNumber}: {ex.Message}", "CNCUtils");
                return false;
            }
        }

        /// <summary>
        /// Get axis label (X, Y, Z, A, etc.) for a given axis number
        /// </summary>
        /// <param name="axisNumber">Axis number (1-8)</param>
        /// <returns>Axis label or empty string if not available</returns>
        public static string GetAxisLabel(int axisNumber)
        {
            var cncPipe = CNCConnectionManager.GetCNCPipe();
            if (cncPipe == null)
                return string.Empty;

            try
            {
                var axis = (CNCPipe.Axes)(axisNumber - 1);
                var result = cncPipe.axis.GetLabel(axis, out char axisLabel);

                if (result == CNCPipe.ReturnCode.SUCCESS)
                {
                    return axisLabel.ToString();
                }

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

    }
}