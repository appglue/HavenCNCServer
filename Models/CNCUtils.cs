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

    }


    /// <summary>
    /// Workpiece reference points enumeration
    /// </summary>
    public enum ReferencePoints
    {
        /// <summary>G28 reference point</summary>
        G28 = 0,
        /// <summary>G30 reference point</summary>
        G30 = 1,
        /// <summary>G30 P3 reference point</summary>
        G30P3 = 2,
        /// <summary>G30 P4 reference point</summary>
        G30P4 = 3
    }

    /// <summary>
    /// Comprehensive Centroid CNC parameter enumeration
    /// Contains all parameters used across the system for type safety and clarity
    /// </summary>
    public enum CentroidParameters
    {
        // Basic System Parameters
        /// <summary>Emergency stop input parameter</summary>
        ESTOP_INPUT_PARM = 0,
        /// <summary>X axis orientation parameter</summary>
        X_ORIENTATION_PARM = 1,
        /// <summary>Tool changer installed parameter</summary>
        TOOL_CHANGER_INSTALLED = 6,

        // Spindle Parameters
        /// <summary>Spindle encoder counts per revolution parameter</summary>
        SPINDLE_COUNTS_REV_PARM = 34,
        /// <summary>Spindle axis assignment parameter</summary>
        SPINDLE_AXIS_PARM = 35,
        /// <summary>Rigid tapping enable parameter</summary>
        RIGID_TAPPING_PARM = 36,
        /// <summary>Spindle deceleration time parameter</summary>
        SPINDLE_DECEL_TIME_PARM = 37,

        // Gear Ratio Parameters
        /// <summary>Low gear ratio parameter</summary>
        LOW_GEAR_RATIO_PARM = 65,
        /// <summary>Medium-low gear ratio parameter</summary>
        MED_LOW_GEAR_RATIO_PARM = 66,
        /// <summary>High gear ratio parameter</summary>
        HIGH_GEAR_RATIO_PARM = 67,

        // Rigid Tapping Parameters
        /// <summary>Rigid tapping slow spindle speed parameter</summary>
        RT_SLOW_SPINDLE_SPEED_PARM = 68,
        /// <summary>Rigid tapping slow spindle time parameter</summary>
        RT_SLOW_SPINDLE_TIME_PARM = 69,
        /// <summary>Spindle control parameter</summary>
        SPINDLE_PARM = 78,
        /// <summary>Rigid tapping spindle cutoff drift parameter</summary>
        RT_SPINDLE_CUTOFF_DRIFT_PARM = 82,

        // Axis Property Parameters (Bit Fields)
        /// <summary>Axis 1 properties parameter</summary>
        AXIS_1_PROPERTIES = 91,
        /// <summary>Axis 2 properties parameter</summary>
        AXIS_2_PROPERTIES = 92,
        /// <summary>Axis 3 properties parameter</summary>
        AXIS_3_PROPERTIES = 93,
        /// <summary>Axis 4 properties parameter</summary>
        AXIS_4_PROPERTIES = 94,

        // ATC Parameters
        /// <summary>ATC maximum bins parameter</summary>
        ATC_MAX_BINS = 161,

        // Additional Axis Properties
        /// <summary>Axis 5 properties parameter</summary>
        AXIS_5_PROPERTIES = 166,
        /// <summary>Axis 6 properties parameter</summary>
        AXIS_6_PROPERTIES = 167,
        /// <summary>Axis 7 properties parameter</summary>
        AXIS_7_PROPERTIES = 168,
        /// <summary>Axis 8 properties parameter</summary>
        AXIS_8_PROPERTIES = 169,

        // Threading and Tapping
        /// <summary>Threading and tapping acceleration/deceleration distance parameter</summary>
        THREADING_AND_TAPPING_ACCEL_DECEL_DISTANCE_PARM = 240,
        /// <summary>Threading and tapping acceleration/deceleration rotation degree step amount parameter</summary>
        THREADING_AND_TAPPING_ACCEL_DECEL_ROT_DEG_STEP_AMT_PARM = 241,

        // Encoder Parameter
        /// <summary>Encoder port assignment parameter</summary>
        ENCODER_PORT_ASSIGNMENT = 315,

        // Probe Parameters
        /// <summary>Probe input parameter</summary>
        PROBE_INPUT_PARM = 405,
        /// <summary>Probe input type parameter</summary>
        PROBE_INPUT_TYPE = 406,
        /// <summary>Touch plate input number parameter</summary>
        TOUCH_PLATE_INPUT_NUMBER = 407,
        /// <summary>Touch plate input type parameter</summary>
        TOUCH_PLATE_INPUT_TYPE = 408,

        // PLC Parameters
        /// <summary>PLC analog parameter</summary>
        PLC_ANALOG_PARM = 420,
        /// <summary>RTG display parameter</summary>
        RTG_DISPLAY_PARM = 430,
        /// <summary>ATC holding configuration parameter</summary>
        ATC_HOLDING_CONFIGURATION = 431,
        /// <summary>ATC tool length method parameter</summary>
        ATC_TOOL_LENGTH_METHOD = 432,

        // Axis Pairing Parameters
        /// <summary>Axis 4 pairing parameter</summary>
        AXIS_4_PAIRING = 554,
        /// <summary>Axis 5 pairing parameter</summary>
        AXIS_5_PAIRING = 555,

        // PWM Parameters (Output 1)
        /// <summary>Acorn PWM frequency parameter</summary>
        ACORN_PWM_FREQUENCY_PARM = 814,
        /// <summary>Acorn PWM options parameter</summary>
        ACORN_PWM_OPTIONS_PARM = 815,
        /// <summary>Acorn PWM velocity parameter</summary>
        ACORN_PWM_VELOCITY_PARM = 816,
        /// <summary>Acorn PWM floor parameter</summary>
        ACORN_PWM_FLOOR_PARM = 817,

        // PWM Parameters (Output 2)
        /// <summary>Acorn PWM frequency parameter (output 2)</summary>
        ACORN_PWM_FREQUENCY_PARM_2 = 824,
        /// <summary>Acorn PWM options parameter (output 2)</summary>
        ACORN_PWM_OPTIONS_PARM_2 = 825,
        /// <summary>Acorn PWM velocity parameter (output 2)</summary>
        ACORN_PWM_VELOCITY_PARM_2 = 826,
        /// <summary>Acorn PWM floor parameter (output 2)</summary>
        ACORN_PWM_FLOOR_PARM_2 = 827,

        // PWM Parameters (Output 3)
        /// <summary>Acorn PWM frequency parameter (output 3)</summary>
        ACORN_PWM_FREQUENCY_PARM_3 = 834,
        /// <summary>Acorn PWM options parameter (output 3)</summary>
        ACORN_PWM_OPTIONS_PARM_3 = 835,
        /// <summary>Acorn PWM velocity parameter (output 3)</summary>
        ACORN_PWM_VELOCITY_PARM_3 = 836,
        /// <summary>Acorn PWM floor parameter (output 3)</summary>
        ACORN_PWM_FLOOR_PARM_3 = 837,

        // ATC Type and Configuration
        /// <summary>ATC type parameter</summary>
        ATC_TYPE = 830,
        /// <summary>ATC time to reverse parameter</summary>
        ATC_TIME_TO_REVERSE = 848,
        /// <summary>ATC time to fault parameter</summary>
        ATC_TIME_TO_FAULT = 849,
        /// <summary>ATC time delay to start parameter</summary>
        ATC_TIME_DELAY_TO_START = 850,
        /// <summary>ATC time delay to start alternate parameter</summary>
        ATC_TIME_DELAY_TO_START_ALT = 851,
        /// <summary>ATC skip first count on reversal parameter</summary>
        ATC_SKIP_FIRST_COUNT_ON_REVERSAL = 852,
        /// <summary>ATC travel past distance parameter</summary>
        ATC_TRAVEL_PAST_DISTANCE = 853,
        /// <summary>ATC travel behind distance parameter</summary>
        ATC_TRAVEL_BEHIND_DISTANCE = 854,

        // Input Inversion Parameters
        /// <summary>Input inversion parameter for inputs 1-16</summary>
        INPUT_INVERSION_1_16 = 911,
        /// <summary>Input inversion parameter for inputs 17-32</summary>
        INPUT_INVERSION_17_32 = 912,
        /// <summary>Input inversion parameter for inputs 33-48</summary>
        INPUT_INVERSION_33_48 = 913,
        /// <summary>Input inversion parameter for inputs 49-64</summary>
        INPUT_INVERSION_49_64 = 914,
        /// <summary>Input inversion parameter for inputs 65-80</summary>
        INPUT_INVERSION_65_80 = 915,

        // Turn Ratio Parameter
        /// <summary>Turn ratio parameter</summary>
        TURN_RATIO_PARM = 968,

        // ATC Time Parameters
        /// <summary>ATC time per tool position parameter</summary>
        ATC_TIME_PER_TOOL_POSITION = 975,

        // SSV/FRV Parameters
        /// <summary>SSV cycle time parameter</summary>
        SSV_CYCLE_TIME = 982,
        /// <summary>SSV amount parameter</summary>
        SSV_AMOUNT = 983,
        /// <summary>FRV cycle time parameter</summary>
        FRV_CYCLE_TIME = 984,

        // Delay Timers
        /// <summary>Spindle OK delay parameter</summary>
        SPINDLE_OK_DELAY_PARM = 996,
        /// <summary>Spindle cooling fan delay timer parameter</summary>
        SPINDLE_COOLING_FAN_DELAY_TIMER = 997,
        /// <summary>Laser cooling fan delay timer parameter</summary>
        LASER_COOLING_FAN_DELAY_TIMER = 998
    }
}