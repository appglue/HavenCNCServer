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
        /// Get the global drive fault delay for all axes
        /// </summary>
        /// <returns>Drive fault delay in milliseconds</returns>
        public static int GetDriveFaultDelay()
        {
            return (int)GetParameterValue(CentroidParameters.PLC_CLEARPATH_OR_G540);
        }

        /// <summary>
        /// Set the global drive fault delay for all axes
        /// </summary>
        /// <param name="delayMs">Drive fault delay in milliseconds</param>
        public static void SetDriveFaultDelay(int delayMs)
        {
            SetParameterValue(CentroidParameters.PLC_CLEARPATH_OR_G540, delayMs);
        }

        /// <summary>
        /// Get the global axis signal inversion settings
        /// </summary>
        /// <returns>Axis signal inversion bit field</returns>
        public static int GetAxisSignalInversion()
        {
            return (int)GetParameterValue(CentroidParameters.ACORN_OUTPUT_INVERSION_PARM);
        }

        /// <summary>
        /// Set the global axis signal inversion settings
        /// </summary>
        /// <param name="inversionBits">Axis signal inversion bit field</param>
        public static void SetAxisSignalInversion(int inversionBits)
        {
            SetParameterValue(CentroidParameters.ACORN_OUTPUT_INVERSION_PARM, inversionBits);
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
        /// Get touch plate wall height
        /// </summary>
        /// <returns>Touch plate wall height</returns>
        public static double GetTouchPlateWallHeight()
        {
            return GetParameterValue(CentroidParameters.TOUCH_PLATE_WALL_HEIGHT_PARM);
        }

        /// <summary>
        /// Set touch plate wall height
        /// </summary>
        /// <param name="height">Wall height value</param>
        public static void SetTouchPlateWallHeight(double height)
        {
            SetParameterValue(CentroidParameters.TOUCH_PLATE_WALL_HEIGHT_PARM, height);
        }

        /// <summary>
        /// Get touch plate wall thickness
        /// </summary>
        /// <returns>Touch plate wall thickness</returns>
        public static double GetTouchPlateWallThickness()
        {
            return GetParameterValue(CentroidParameters.TOUCH_PLATE_WALL_THICKNESS_PARM);
        }

        /// <summary>
        /// Set touch plate wall thickness
        /// </summary>
        /// <param name="thickness">Wall thickness value</param>
        public static void SetTouchPlateWallThickness(double thickness)
        {
            SetParameterValue(CentroidParameters.TOUCH_PLATE_WALL_THICKNESS_PARM, thickness);
        }

        /// <summary>
        /// Get touch plate internal diameter
        /// </summary>
        /// <returns>Touch plate internal diameter</returns>
        public static double GetTouchPlateInternalDiameter()
        {
            return GetParameterValue(CentroidParameters.TOUCH_PLATE_INTERNAL_DIAMETER_PARM);
        }

        /// <summary>
        /// Set touch plate internal diameter
        /// </summary>
        /// <param name="diameter">Internal diameter value</param>
        public static void SetTouchPlateInternalDiameter(double diameter)
        {
            SetParameterValue(CentroidParameters.TOUCH_PLATE_INTERNAL_DIAMETER_PARM, diameter);
        }

        /// <summary>
        /// Get touch plate fast probing rate
        /// </summary>
        /// <returns>Touch plate fast rate</returns>
        public static double GetTouchPlateFastRate()
        {
            return GetParameterValue(CentroidParameters.TOUCH_PLATE_FAST_RATE_PARM);
        }

        /// <summary>
        /// Set touch plate fast probing rate
        /// </summary>
        /// <param name="rate">Fast probing rate</param>
        public static void SetTouchPlateFastRate(double rate)
        {
            SetParameterValue(CentroidParameters.TOUCH_PLATE_FAST_RATE_PARM, rate);
        }

        /// <summary>
        /// Get touch plate slow probing rate
        /// </summary>
        /// <returns>Touch plate slow rate</returns>
        public static double GetTouchPlateSlowRate()
        {
            return GetParameterValue(CentroidParameters.TOUCH_PLATE_SLOW_RATE_PARM);
        }

        /// <summary>
        /// Set touch plate slow probing rate
        /// </summary>
        /// <param name="rate">Slow probing rate</param>
        public static void SetTouchPlateSlowRate(double rate)
        {
            SetParameterValue(CentroidParameters.TOUCH_PLATE_SLOW_RATE_PARM, rate);
        }

        /// <summary>
        /// Get touch plate attributes bit field
        /// </summary>
        /// <returns>Touch plate attributes</returns>
        public static int GetTouchPlateAttributes()
        {
            return (int)GetParameterValue(CentroidParameters.TOUCH_PLATE_ATTRIBUTES_PARM);
        }

        /// <summary>
        /// Set touch plate attributes bit field
        /// </summary>
        /// <param name="attributes">Touch plate attributes bit field</param>
        public static void SetTouchPlateAttributes(int attributes)
        {
            SetParameterValue(CentroidParameters.TOUCH_PLATE_ATTRIBUTES_PARM, attributes);
        }

        /// <summary>
        /// Get second spindle maximum speed
        /// </summary>
        /// <returns>Second spindle maximum speed</returns>
        public static int GetSecondSpindleMaxSpeed()
        {
            return (int)GetParameterValue(CentroidParameters.SECOND_SPINDLE_MAX_SPEED);
        }

        /// <summary>
        /// Set second spindle maximum speed
        /// </summary>
        /// <param name="maxSpeed">Maximum speed value</param>
        public static void SetSecondSpindleMaxSpeed(int maxSpeed)
        {
            SetParameterValue(CentroidParameters.SECOND_SPINDLE_MAX_SPEED, maxSpeed);
        }

        /// <summary>
        /// Get second spindle minimum speed
        /// </summary>
        /// <returns>Second spindle minimum speed</returns>
        public static int GetSecondSpindleMinSpeed()
        {
            return (int)GetParameterValue(CentroidParameters.SECOND_SPINDLE_MIN_SPEED);
        }

        /// <summary>
        /// Set second spindle minimum speed
        /// </summary>
        /// <param name="minSpeed">Minimum speed value</param>
        public static void SetSecondSpindleMinSpeed(int minSpeed)
        {
            SetParameterValue(CentroidParameters.SECOND_SPINDLE_MIN_SPEED, minSpeed);
        }

        /// <summary>
        /// Check if second spindle is enabled
        /// </summary>
        /// <returns>True if second spindle is enabled</returns>
        public static bool IsSecondSpindleEnabled()
        {
            return GetParameterValue(CentroidParameters.SECOND_SPINDLE_ENABLE) != 0;
        }

        /// <summary>
        /// Enable or disable second spindle
        /// </summary>
        /// <param name="enabled">True to enable, false to disable</param>
        public static void SetSecondSpindleEnabled(bool enabled)
        {
            SetParameterValue(CentroidParameters.SECOND_SPINDLE_ENABLE, enabled ? 1 : 0);
        }

        /// <summary>
        /// Check if enhanced ATC is enabled
        /// </summary>
        /// <returns>True if enhanced ATC is enabled</returns>
        public static bool IsEnhancedATCEnabled()
        {
            return GetParameterValue(CentroidParameters.ENHANCED_ATC_PARM) != 0;
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
        /// Get SSV (Spindle Speed Variation) cycle time
        /// </summary>
        /// <returns>SSV cycle time</returns>
        public static double GetSSVCycleTime()
        {
            return GetParameterValue(CentroidParameters.SSV_CYCLE_TIME);
        }

        /// <summary>
        /// Set SSV (Spindle Speed Variation) cycle time
        /// </summary>
        /// <param name="cycleTime">SSV cycle time</param>
        public static void SetSSVCycleTime(double cycleTime)
        {
            SetParameterValue(CentroidParameters.SSV_CYCLE_TIME, cycleTime);
        }

        /// <summary>
        /// Get SSV (Spindle Speed Variation) amount
        /// </summary>
        /// <returns>SSV amount percentage</returns>
        public static double GetSSVAmount()
        {
            return GetParameterValue(CentroidParameters.SSV_AMOUNT);
        }

        /// <summary>
        /// Set SSV (Spindle Speed Variation) amount
        /// </summary>
        /// <param name="amount">SSV amount percentage</param>
        public static void SetSSVAmount(double amount)
        {
            SetParameterValue(CentroidParameters.SSV_AMOUNT, amount);
        }

        /// <summary>
        /// Get FRV (Feed Rate Variation) cycle time
        /// </summary>
        /// <returns>FRV cycle time</returns>
        public static double GetFRVCycleTime()
        {
            return GetParameterValue(CentroidParameters.FRV_CYCLE_TIME);
        }

        /// <summary>
        /// Set FRV (Feed Rate Variation) cycle time
        /// </summary>
        /// <param name="cycleTime">FRV cycle time</param>
        public static void SetFRVCycleTime(double cycleTime)
        {
            SetParameterValue(CentroidParameters.FRV_CYCLE_TIME, cycleTime);
        }

        /// <summary>
        /// Get spindle deceleration time
        /// </summary>
        /// <returns>Spindle deceleration time in seconds</returns>
        public static double GetSpindleDecelTime()
        {
            return GetParameterValue(CentroidParameters.SPINDLE_DECEL_TIME_PARM);
        }

        /// <summary>
        /// Set spindle deceleration time
        /// </summary>
        /// <param name="decelTime">Spindle deceleration time in seconds</param>
        public static void SetSpindleDecelTime(double decelTime)
        {
            SetParameterValue(CentroidParameters.SPINDLE_DECEL_TIME_PARM, decelTime);
        }

        /// <summary>
        /// Get rigid tapping slow spindle speed
        /// </summary>
        /// <returns>Rigid tapping slow spindle speed</returns>
        public static double GetRigidTappingSlowSpindleSpeed()
        {
            return GetParameterValue(CentroidParameters.RT_SLOW_SPINDLE_SPEED_PARM);
        }

        /// <summary>
        /// Set rigid tapping slow spindle speed
        /// </summary>
        /// <param name="speed">Rigid tapping slow spindle speed</param>
        public static void SetRigidTappingSlowSpindleSpeed(double speed)
        {
            SetParameterValue(CentroidParameters.RT_SLOW_SPINDLE_SPEED_PARM, speed);
        }

        /// <summary>
        /// Get rigid tapping slow spindle time
        /// </summary>
        /// <returns>Rigid tapping slow spindle time</returns>
        public static double GetRigidTappingSlowSpindleTime()
        {
            return GetParameterValue(CentroidParameters.RT_SLOW_SPINDLE_TIME_PARM);
        }

        /// <summary>
        /// Set rigid tapping slow spindle time
        /// </summary>
        /// <param name="time">Rigid tapping slow spindle time</param>
        public static void SetRigidTappingSlowSpindleTime(double time)
        {
            SetParameterValue(CentroidParameters.RT_SLOW_SPINDLE_TIME_PARM, time);
        }

        /// <summary>
        /// Get threading and tapping acceleration/deceleration distance
        /// </summary>
        /// <returns>Acceleration/deceleration distance</returns>
        public static double GetThreadingTappingAccelDecelDistance()
        {
            return GetParameterValue(CentroidParameters.THREADING_AND_TAPPING_ACCEL_DECEL_DISTANCE_PARM);
        }

        /// <summary>
        /// Set threading and tapping acceleration/deceleration distance
        /// </summary>
        /// <param name="distance">Acceleration/deceleration distance</param>
        public static void SetThreadingTappingAccelDecelDistance(double distance)
        {
            SetParameterValue(CentroidParameters.THREADING_AND_TAPPING_ACCEL_DECEL_DISTANCE_PARM, distance);
        }

    }
}