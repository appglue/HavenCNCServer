using System;
using CentroidAPI;

namespace HavenCNCServer.CentriodAPI
{
    /// <summary>
    /// Clean CNC12 API wrapper class with no external dependencies.
    /// Replicates only the GeneralUtils methods used in PLC documentation.
    /// Requires CentroidAPI reference and initialization.
    /// </summary>
    public static class CNCUtils
    {
        private static CNCPipe? _api;

        /// <summary>
        /// Initialize the CNCUtils with your CentroidAPI CNCPipe instance
        /// </summary>
        /// <param name="cncPipe">Your CentroidAPI.CNCPipe instance</param>
        public static void Initialize(CNCPipe cncPipe)
        {
            _api = cncPipe ?? throw new ArgumentNullException(nameof(cncPipe));
        }

        private static void EnsureInitialized()
        {
            if (_api == null)
                throw new InvalidOperationException("CNCUtils not initialized. Call Initialize() first.");
        }


        /// <summary>
        /// Get a CNC12 parameter value using CentroidParameters enum
        /// </summary>
        /// <param name="parameter">CentroidParameters enum value</param>
        /// <returns>Parameter value as double</returns>
        public static double GetParameterValue(CentroidParameters parameter)
        {
            EnsureInitialized();

            try
            {
                // Use the parameter property of CNCPipe to access parameter methods
                CNCPipe.ReturnCode returnCode = _api!.parameter.GetMachineParameterValue((int)parameter, out double value);
                
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
            EnsureInitialized();

            try
            {
                CNCPipe.ReturnCode returnCode = _api!.parameter.SetMachineParameter((int)parameter, value);

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
            EnsureInitialized();

            try
            {
                // CentroidAPI uses 1-based indexing: G28=1, G30=2, G30P3=3, G30P4=4
                int referenceIndex = (int)reference + 1;
                _api!.wcs.GetWorkpieceReference(referenceIndex, axis, out double value);
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
            EnsureInitialized();

            try
            {
                // CentroidAPI uses 1-based indexing: G28=1, G30=2, G30P3=3, G30P4=4
                int referenceIndex = (int)reference + 1;
                _api!.wcs.SetWorkpieceReference(referenceIndex, axis, point);
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
            EnsureInitialized();

            var availableInputs = new List<int>();
            
            try
            {
                // Get system type to determine I/O layout
                _api!.system.GetUnlockVersion(out CNCPipe.Sys.UnlockVersions unlockVersion);
                
                // Base I/O available on all systems (minimum 8 inputs)
                int baseInputCount = 8;
                int expansionStartIO = 17;
                
                // Determine system-specific configuration
                bool isAcorn = unlockVersion.ToString().Contains("ACORN") && !unlockVersion.ToString().Contains("ACORN_SIX");
                bool isAcornSix = unlockVersion.ToString().Contains("ACORN_SIX");
                bool isHickory = unlockVersion.ToString().Contains("HICKORY");
                
                if (isAcornSix)
                {
                    baseInputCount = 16;
                    expansionStartIO = 65;
                }
                else if (isHickory)
                {
                    baseInputCount = 32;
                    expansionStartIO = 129;
                }
                
                // Add base inputs
                for (int i = 1; i <= baseInputCount; i++)
                {
                    availableInputs.Add(i);
                }
                
                // Add expansion board inputs
                int expansionCount = 0;
                if (isAcorn)
                {
                    _api.system.GetEther1616DeviceInfo(out List<CNCPipe.Sys.Ether1616Device> devices);
                    expansionCount = devices?.Count ?? 0;
                }
                else if (isAcornSix)
                {
                    _api.system.GetPLCEXP1616NumberofDevices(out expansionCount);
                }
                else if (isHickory)
                {
                    _api.system.GetECAT1616NumberOfDevices(out expansionCount);
                }
                
                // Calculate expansion I/O numbers
                if (expansionCount > 0)
                {
                    for (int board = 0; board < expansionCount; board++)
                    {
                        for (int i = 0; i < 16; i++)  // All expansion boards provide 16 I/O each
                        {
                            availableInputs.Add(expansionStartIO + (board * 16) + i);
                        }
                    }
                }
                
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
            EnsureInitialized();

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
            EnsureInitialized();

            try
            {
                _api!.system.GetUnlockVersion(out CNCPipe.Sys.UnlockVersions unlockVersion);
                
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
                    _api.system.GetEther1616DeviceInfo(out List<CNCPipe.Sys.Ether1616Device> devices);
                    int expansionCount = devices?.Count ?? 0;
                    expansionInputs = expansionCount * 16;
                    expansionOutputs = expansionCount * 16;
                }
                else if (isAcornSix)
                {
                    systemType = "AcornSix";
                    baseInputs = 16;
                    baseOutputs = 16;
                    _api.system.GetPLCEXP1616NumberofDevices(out int expansionCount);
                    expansionInputs = expansionCount * 16;
                    expansionOutputs = expansionCount * 16;
                }
                else if (isHickory)
                {
                    systemType = "Hickory";
                    baseInputs = 32;
                    baseOutputs = 32;
                    _api.system.GetECAT1616NumberOfDevices(out int expansionCount);
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
            if (_api == null)
                throw new InvalidOperationException("CNCUtils not initialized. Call Initialize() first.");

            var axisEnum = (CNCPipe.Axes)axis;
            _api.axis.SetRate(axisEnum, rateType, value);
        }

        /// <summary>
        /// Get axis rate for specific axis and rate type
        /// </summary>
        /// <param name="axis">Axis number (1-based)</param>
        /// <param name="rateType">Type of rate to get</param>
        /// <returns>Rate value</returns>
        public static double GetAxisRate(int axis, CNCPipe.Axis.Rate rateType)
        {
            if (_api == null)
                throw new InvalidOperationException("CNCUtils not initialized. Call Initialize() first.");

            var axisEnum = (CNCPipe.Axes)axis;
            _api.axis.GetRate(axisEnum, rateType, out double value);
            return value;
        }

    }
}