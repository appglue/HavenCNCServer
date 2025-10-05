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
        private static CNCPipe _api;

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
        /// Get a CNC12 parameter value by parameter number
        /// </summary>
        /// <param name="parameter">Parameter number (int)</param>
        /// <returns>Parameter value as double</returns>
        public static double GetParameterValue(int parameter)
        {
            EnsureInitialized();

            try
            {
                // Use the parameter property of CNCPipe to access parameter methods
                CNCPipe.ReturnCode returnCode = _api.parameter.GetMachineParameterValue(parameter, out double value);
                
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
        /// Get a CNC12 parameter value using CNC12Parameters enum
        /// </summary>
        /// <param name="parameter">CNC12Parameters enum value</param>
        /// <returns>Parameter value as double</returns>
        public static double GetParameterValue(CNC12Parameters parameter)
        {
            return GetParameterValue((int)parameter);
        }

        /// <summary>
        /// Set a CNC12 parameter value by parameter number
        /// </summary>
        /// <param name="parameter">Parameter number (int)</param>
        /// <param name="value">Value to set</param>
        public static void SetParameterValue(int parameter, double value)
        {
            EnsureInitialized();

            try
            {
                CNCPipe.ReturnCode returnCode = _api.parameter.SetMachineParameter(parameter, value);

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
        /// Set a CNC12 parameter value using CNC12Parameters enum
        /// </summary>
        /// <param name="parameter">CNC12Parameters enum value</param>
        /// <param name="value">Value to set</param>
        public static void SetParameterValue(CNC12Parameters parameter, double value)
        {
            SetParameterValue((int)parameter, value);
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
                _api.wcs.GetWorkpieceReference(referenceIndex, axis, out double value);
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
                _api.wcs.SetWorkpieceReference(referenceIndex, axis, point);
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

    }


    /// <summary>
    /// Workpiece reference points enumeration
    /// </summary>
    public enum ReferencePoints
    {
        G28 = 0,    // G28 reference point
        G30 = 1,    // G30 reference point  
        G30P3 = 2,  // G30 P3 reference point
        G30P4 = 3   // G30 P4 reference point
    }

    /// <summary>
    /// CNC12 parameter enumeration - only the parameters used in PLC documentation
    /// Values taken from actual CNC12Parameters.cs file
    /// </summary>
    public enum CNC12Parameters
    {
        // Basic parameters
        ESTOP_INPUT_PARM = 0,
        X_ORIENTATION_PARM = 1,

        // Spindle Parameters (from CNC12Parameters.cs)
        SPINDLE_COUNTS_REV_PARM = 34,
        SPINDLE_AXIS_PARM = 35,
        RIGID_TAPPING_PARM = 36,
        SPINDLE_DECEL_TIME_PARM = 37,
        LOW_GEAR_RATIO_PARM = 65,
        MED_LOW_GEAR_RATIO_PARM = 66,
        RT_SLOW_SPINDLE_SPEED_PARM = 68,
        RT_SLOW_SPINDLE_TIME_PARM = 69,
        SPINDLE_PARM = 78,
        RT_SPINDLE_CUTOFF_DRIFT_PARM = 82,

        // Threading and Tapping
        THREADING_AND_TAPPING_ACCEL_DECEL_DISTANCE_PARM = 240,
        THREADING_AND_TAPPING_ACCEL_DECEL_ROT_DEG_STEP_AMT_PARM = 241,

        // Probe Parameters
        PROBE_INPUT_TYPE = 406,

        // PLC Parameters
        PLC_ANALOG_PARM = 420,

        // PWM Parameters (Acorn/Laser)
        ACORN_PWM_FREQUENCY_PARM = 814,
        ACORN_PWM_OPTIONS_PARM = 815,
        ACORN_PWM_VELCOCITY_PARM = 816,
        ACORN_PWM_FLOOR_PARM = 817,

        // SSV/FRV Parameters  
        SSV_CYCLE_TIME = 982,
        SSV_AMOUNT = 983,
        FRV_CYCLE_TIME = 984,

        // Delay Timers
        SPINDLE_OK_DELAY_PARM = 996,
        SPINDLE_COOLING_FAN_DELAY_TIMER = 997,
        LASER_COOLING_FAN_DELAY_TIMER = 998
    }
}