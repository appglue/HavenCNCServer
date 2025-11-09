using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CentroidAPI;
using HavenCNCServer.Centroid.Data;
using HavenCNCServer.Services;

namespace HavenCNCServer.Centroid
{
    /// <summary>
    /// Static class for generating PLC configuration sections with easy setup methods
    /// 
    /// CentroidAPI Integration Status:
    /// - CentroidAPI is properly imported and CNCPipe is accessible
    /// - Input inversion methods are implemented with parameter mapping (911-915)
    /// - Axis configuration methods are implemented using CNCPipe.Axis API
    /// - All parameter operations use CNCUtils for proper API calls
    /// - Framework includes complete bit manipulation for input inversion parameters
    /// </summary>
    public static partial class CentroidConfigUtil
    {
        #region File Path Constants

        /// <summary>
        /// Path to the PLC template file
        /// </summary>
        public const string TEMPLATE_FILE_PATH = @"C:\CNC12\cncm\AcornSix_Wizard_PLC_Template.src";

        /// <summary>
        /// Path to the working PLC file
        /// </summary>
        public const string WORKING_FILE_PATH = @"C:\CNC12\cncm\AcornSix_Wizard_PLC.src";

        #endregion


        #region PLC File Management

        /// <summary>
        /// Copies the template PLC file to the working file
        /// </summary>
        /// <returns>True if successful</returns>
        public static bool CopyTemplateFile()
        {
            try
            {
                if (!File.Exists(TEMPLATE_FILE_PATH))
                {
                    throw new FileNotFoundException($"Template file not found: {TEMPLATE_FILE_PATH}");
                }

                // Create directory if it doesn't exist
                var directory = Path.GetDirectoryName(WORKING_FILE_PATH);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.Copy(TEMPLATE_FILE_PATH, WORKING_FILE_PATH, true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Updates a wizard region in the PLC file with new I/O definitions
        /// </summary>
        /// <param name="regionName">Name of the wizard region (e.g., "Inputs", "Outputs")</param>
        /// <param name="ioFunctions">List of I/O functions to write</param>
        /// <returns>True if successful</returns>
        public static bool UpdateWizardRegion(string regionName, List<IOFunction> ioFunctions)
        {
            try
            {
                if (!File.Exists(WORKING_FILE_PATH))
                {
                    throw new FileNotFoundException($"PLC file not found: {WORKING_FILE_PATH}");
                }

                var lines = File.ReadAllLines(WORKING_FILE_PATH).ToList();
                var startPattern = $@";\s*#wizardregion\s+{Regex.Escape(regionName)}";
                var endPattern = @";\s*#endregion";

                int startIndex = -1;
                int endIndex = -1;

                // Find the wizard region boundaries
                for (int i = 0; i < lines.Count; i++)
                {
                    if (Regex.IsMatch(lines[i], startPattern, RegexOptions.IgnoreCase))
                    {
                        startIndex = i;
                    }
                    else if (startIndex >= 0 && Regex.IsMatch(lines[i], endPattern, RegexOptions.IgnoreCase))
                    {
                        endIndex = i;
                        break;
                    }
                }

                if (startIndex < 0 || endIndex < 0)
                {
                    throw new InvalidOperationException($"Wizard region '{regionName}' not found in PLC file");
                }

                // Generate new content - determine I/O type from region name
                string ioType = regionName.ToLower().Contains("input") ? "INP" : "OUT";
                var newContent = GenerateIODefinitions(ioFunctions, ioType);

                // Replace the content between the markers
                lines.RemoveRange(startIndex + 1, endIndex - startIndex - 1);
                lines.InsertRange(startIndex + 1, newContent);

                // Write back to file
                File.WriteAllLines(WORKING_FILE_PATH, lines);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Generates properly formatted I/O definitions
        /// </summary>
        /// <param name="ioFunctions">List of I/O functions</param>
        /// <param name="ioType">Type of I/O - "INP" for inputs or "OUT" for outputs</param>
        /// <returns>List of formatted definition lines</returns>
        private static List<string> GenerateIODefinitions(List<IOFunction> ioFunctions, string ioType)
        {
            var result = new List<string>();

            if (!ioFunctions.Any())
            {
                return result;
            }

            // Filter out functions with missing required properties
            var validFunctions = ioFunctions.Where(f => f.Number.HasValue && !string.IsNullOrEmpty(f.Name)).ToList();

            if (!validFunctions.Any())
            {
                return result;
            }

            // Sort by I/O number for consistent ordering
            var sortedFunctions = validFunctions.OrderBy(f => f.Number!.Value).ToList();

            // Find the longest function name for alignment
            int maxNameLength = sortedFunctions.Max(f => f.Name!.Length);
            int alignmentColumn = Math.Max(maxNameLength + 4, 16);

            foreach (var function in sortedFunctions)
            {
                var definition = $"{ioType}{function.Number!.Value}";
                var spacing = new string(' ', alignmentColumn - function.Name!.Length);
                var line = $"{function.Name}{spacing}IS {definition}";
                result.Add(line);
            }

            return result;
        }

        #endregion

        #region Axis Configuration Methods

        /// <summary>
        /// Configures an axis with the specified parameters
        /// </summary>
        /// <param name="config">Axis configuration</param>
        /// <returns>True if successful</returns>
        public static bool ConfigureAxis(AxisConfiguration config)
        {
            try
            {
                var cncPipe = CNCConnectionManager.GetCNCPipe();

                // CNCConnectionManager ensures the pipe is already constructed
                if (cncPipe == null)
                {
                    return false;
                }

                // Convert axis number to enum (1-based to 0-based)
                var axisEnum = (CNCPipe.Axes)(config.AxisNumber - 1);

                // Set basic axis parameters only if provided
                if (config.StepsPerRevolution.HasValue)
                    cncPipe.axis.SetCountsPerTurn(axisEnum, config.StepsPerRevolution.Value);

                if (config.TurnRatio.HasValue)
                    cncPipe.axis.SetScrewPitch(axisEnum, config.TurnRatio.Value);

                if (config.PlusTravelLimit.HasValue)
                    cncPipe.axis.SetTravelLimit(axisEnum, CNCPipe.Axis.Direction.PLUS, config.PlusTravelLimit.Value);

                if (config.MinusTravelLimit.HasValue)
                    cncPipe.axis.SetTravelLimit(axisEnum, CNCPipe.Axis.Direction.MINUS, config.MinusTravelLimit.Value);

                if (config.BacklashCompensation.HasValue)
                    cncPipe.axis.SetLashComp(axisEnum, config.BacklashCompensation.Value);

                if (config.SlowJogRate.HasValue)
                    cncPipe.axis.SetRate(axisEnum, CNCPipe.Axis.Rate.SLOW_JOG, config.SlowJogRate.Value);

                if (config.FastJogRate.HasValue)
                    cncPipe.axis.SetRate(axisEnum, CNCPipe.Axis.Rate.FAST_JOG, config.FastJogRate.Value);

                if (config.MaxRate.HasValue)
                    cncPipe.axis.SetRate(axisEnum, CNCPipe.Axis.Rate.MAX, config.MaxRate.Value);

                if (config.FastJogPlusDirection.HasValue)
                    cncPipe.axis.SetRate(axisEnum, CNCPipe.Axis.Rate.FAST_JOG_PLUS, config.FastJogPlusDirection.Value);

                if (config.FastJogMinusDirection.HasValue)
                    cncPipe.axis.SetRate(axisEnum, CNCPipe.Axis.Rate.FAST_JOG_MINUS, config.FastJogMinusDirection.Value);

                if (config.AccelerationTime.HasValue)
                    cncPipe.axis.SetAccelTime(axisEnum, config.AccelerationTime.Value);

                if (config.HomingFeedrate.HasValue)
                    cncPipe.axis.SetRate(axisEnum, CNCPipe.Axis.Rate.HOME_JOG, config.HomingFeedrate.Value);

                // Note: DriveEnableDelay may be a global parameter rather than per-axis
                // This would need to be set via parameter if it's per-axis specific
                if (config.DriveEnableDelay.HasValue)
                {
                    // This might be a parameter-based setting rather than API call
                    // Implementation depends on actual parameter structure
                    System.Diagnostics.Debug.WriteLine($"DriveEnableDelay for Axis {config.AxisNumber}: {config.DriveEnableDelay.Value}ms - parameter implementation needed");
                }

                if (!string.IsNullOrEmpty(config.Label))
                    cncPipe.axis.SetLabel(axisEnum, config.Label[0]); // SetLabel expects a char

                if (config.IsReversed.HasValue)
                    cncPipe.axis.SetAxisReversal(axisEnum, config.IsReversed.Value);

                // Configure axis properties (rotary, signal inversions, etc.) via parameters
                ConfigureAxisProperties(config);

                System.Diagnostics.Debug.WriteLine($"Configuring Axis {config.AxisNumber} ({config.Label ?? "unknown"}): {config.StepsPerRevolution?.ToString() ?? "not set"} steps/rev");

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Configures axis properties using parameter bit fields (91-94, 166-169)
        /// </summary>
        /// <param name="config">Axis configuration</param>
        /// <returns>True if successful</returns>
        public static bool ConfigureAxisProperties(AxisConfiguration config)
        {
            try
            {
                // Determine parameter number based on axis number
                CentroidParameters parameterNumber = GetAxisPropertyParameter(config.AxisNumber);

                // Get current parameter value
                int axisProperties = (int)CNCUtils.GetParameterValue(parameterNumber);
                bool parameterModified = false;

                // Configure bit fields according to documentation - only if values are provided
                // Bit 0: Linear/Rotary (0=Linear, 1=Rotary)
                if (config.IsRotary.HasValue)
                {
                    axisProperties = CNCUtils.ModifyBit(axisProperties, 0, config.IsRotary.Value);
                    parameterModified = true;
                }

                // Bit 1: Rotary DRO Display (0=Show Rotations, 1=Wrap Around)  
                if (config.RotaryWrapAround.HasValue)
                {
                    axisProperties = CNCUtils.ModifyBit(axisProperties, 1, config.RotaryWrapAround.Value);
                    parameterModified = true;
                }

                // Bit 4: C-Axis Enable
                if (config.CAxisEnabled.HasValue)
                {
                    axisProperties = CNCUtils.ModifyBit(axisProperties, 4, config.CAxisEnabled.Value);
                    parameterModified = true;
                }

                // Bit 7: Prevent Divide by 360 for C-Axis
                if (config.PreventDivideBy360.HasValue)
                {
                    axisProperties = CNCUtils.ModifyBit(axisProperties, 7, config.PreventDivideBy360.Value);
                    parameterModified = true;
                }

                // Bit 9: Hide Axis from DRO (ATC Turret)
                if (config.HideFromDRO.HasValue)
                {
                    axisProperties = CNCUtils.ModifyBit(axisProperties, 9, config.HideFromDRO.Value);
                    parameterModified = true;
                }

                // Bit 11: Parallel to X (Rotary)
                if (config.ParallelToX.HasValue)
                {
                    axisProperties = CNCUtils.ModifyBit(axisProperties, 11, config.ParallelToX.Value);
                    parameterModified = true;
                }

                // Bit 12: Parallel to Y (Rotary)
                if (config.ParallelToY.HasValue)
                {
                    axisProperties = CNCUtils.ModifyBit(axisProperties, 12, config.ParallelToY.Value);
                    parameterModified = true;
                }

                // Only update the parameter if we modified something
                if (parameterModified)
                {
                    CNCUtils.SetParameterValue(parameterNumber, axisProperties);
                    System.Diagnostics.Debug.WriteLine($"Configuring Axis {config.AxisNumber} properties: Parameter {parameterNumber} = {axisProperties}");
                }

                // Handle axis signal inversions (Parameter 961 - 4-bit nibbles per axis)
                if (config.StepSignalInverted.HasValue || config.DirectionSignalInverted.HasValue || config.EnableSignalInverted.HasValue)
                {
                    ConfigureAxisSignalInversion(config);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Configures axis signal inversions using parameter 961 (4-bit nibbles per axis)
        /// </summary>
        /// <param name="config">Axis configuration with signal inversion settings</param>
        /// <returns>True if successful</returns>
        private static bool ConfigureAxisSignalInversion(AxisConfiguration config)
        {
            try
            {
                // Get current axis signal inversion parameter (961)
                int currentInversions = (int)CNCUtils.GetParameterValue(CentroidParameters.ACORN_OUTPUT_INVERSION_PARM);

                // Calculate the nibble position for this axis (4 bits per axis)
                int nibblePosition = (config.AxisNumber - 1) * 4;

                // Clear the current nibble for this axis (4 bits)
                int nibbleMask = 0xF << nibblePosition;
                int clearedInversions = currentInversions & ~nibbleMask;

                // Build the new nibble value
                int newNibble = 0;
                if (config.StepSignalInverted == true) newNibble |= 0x1;      // Bit 0: Step
                if (config.DirectionSignalInverted == true) newNibble |= 0x2; // Bit 1: Direction  
                if (config.EnableSignalInverted == true) newNibble |= 0x4;    // Bit 2: Enable
                                                                              // Bit 3: Quadrature (not exposed in this UI)

                // Set the new nibble in the correct position
                int newInversions = clearedInversions | (newNibble << nibblePosition);

                // Update the parameter
                CNCUtils.SetParameterValue(CentroidParameters.ACORN_OUTPUT_INVERSION_PARM, newInversions);

                System.Diagnostics.Debug.WriteLine($"Configuring Axis {config.AxisNumber} signal inversions: Parameter 961 = {newInversions:X} (nibble: {newNibble:X})");

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the axis property parameter number for the given axis
        /// </summary>
        /// <param name="axisNumber">Axis number (1-8)</param>
        /// <returns>Parameter number or -1 if invalid</returns>
        private static CentroidParameters GetAxisPropertyParameter(int axisNumber)
        {
            return axisNumber switch
            {
                1 => CentroidParameters.AXIS_1_PROPERTIES,
                2 => CentroidParameters.AXIS_2_PROPERTIES,
                3 => CentroidParameters.AXIS_3_PROPERTIES,
                4 => CentroidParameters.AXIS_4_PROPERTIES,
                5 => CentroidParameters.AXIS_5_PROPERTIES,
                6 => CentroidParameters.AXIS_6_PROPERTIES,
                7 => CentroidParameters.AXIS_7_PROPERTIES,
                8 => CentroidParameters.AXIS_8_PROPERTIES,
                _ => throw new ArgumentException($"Invalid axis number: {axisNumber}")
            };
        }

        /// <summary>
        /// Configures axis pairing for slave axis
        /// </summary>
        /// <param name="slaveAxis">Slave axis number (4-8)</param>
        /// <param name="masterAxis">Master axis number (1-3, 0 = none)</param>
        /// <returns>True if successful</returns>
        public static bool ConfigureAxisPairing(int slaveAxis, int masterAxis)
        {
            try
            {
                // Parameter mapping for axis pairing
                CentroidParameters parameterNumber = slaveAxis switch
                {
                    4 => CentroidParameters.AXIS_4_PAIRING,
                    5 => CentroidParameters.AXIS_5_PAIRING,
                    _ => throw new ArgumentException($"Invalid slave axis number: {slaveAxis}")
                };

                CNCUtils.SetParameterValue(parameterNumber, masterAxis);

                System.Diagnostics.Debug.WriteLine($"Pairing Axis {slaveAxis} to Master Axis {masterAxis} (Parameter {parameterNumber})");

                System.Diagnostics.Debug.WriteLine($"Pairing Axis {slaveAxis} to Master Axis {masterAxis} (Parameter {parameterNumber})");

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Spindle Configuration Methods

        /// <summary>
        /// Configures spindle parameters
        /// </summary>
        /// <param name="config">Spindle configuration</param>
        /// <returns>True if successful</returns>
        public static bool ConfigureSpindle(SpindleConfiguration config)
        {
            try
            {
                var cncPipe = CNCConnectionManager.GetCNCPipe();

                // CNCConnectionManager ensures the pipe is already constructed
                if (cncPipe == null)
                {
                    return false;
                }

                // Core parameters via CNCUtils.SetParameterValue() - only set if provided
                if (config.EncoderCounts.HasValue)
                    CNCUtils.SetParameterValue(CentroidParameters.SPINDLE_COUNTS_REV_PARM, config.EncoderCounts.Value);

                if (config.SpindleAxis.HasValue)
                    CNCUtils.SetParameterValue(CentroidParameters.SPINDLE_AXIS_PARM, config.SpindleAxis.Value);

                if (config.LowGearRatio.HasValue)
                    CNCUtils.SetParameterValue(CentroidParameters.LOW_GEAR_RATIO_PARM, (int)(config.LowGearRatio.Value * 1000));

                if (config.MediumGearRatio.HasValue)
                    CNCUtils.SetParameterValue(CentroidParameters.MED_LOW_GEAR_RATIO_PARM, (int)(config.MediumGearRatio.Value * 1000));

                if (config.HighGearRatio.HasValue)
                    CNCUtils.SetParameterValue(CentroidParameters.HIGH_GEAR_RATIO_PARM, (int)(config.HighGearRatio.Value * 1000));

                if (config.AnalogRange.HasValue)
                    CNCUtils.SetParameterValue(CentroidParameters.PLC_ANALOG_PARM, config.AnalogRange.Value);

                if (config.RTGDisplay.HasValue)
                    CNCUtils.SetParameterValue(CentroidParameters.RTG_DISPLAY_PARM, config.RTGDisplay.Value ? 1 : 0);

                if (config.OkDelay.HasValue)
                    CNCUtils.SetParameterValue(CentroidParameters.SPINDLE_OK_DELAY_PARM, (int)(config.OkDelay.Value * 1000));

                if (config.FanDelay.HasValue)
                    CNCUtils.SetParameterValue(CentroidParameters.SPINDLE_COOLING_FAN_DELAY_TIMER, (int)(config.FanDelay.Value * 1000));

                // Speed configuration via API calls (commented out as it depends on MainWindow)
                // if (config.MaxSpeed.HasValue)
                //     MainWindow.skin.state.SetHighRangeSpindleSpeed(CNCPipe.State.Value.MAX, config.MaxSpeed.Value);
                // if (config.MinSpeed.HasValue)
                //     MainWindow.skin.state.SetHighRangeSpindleSpeed(CNCPipe.State.Value.MIN, config.MinSpeed.Value);

                // Configure spindle parameter 78 bit field only if encoder or scaling settings are provided
                if (config.EncoderEnabled.HasValue || config.SecondSpindleEnabled.HasValue || config.SpindleScalingEnabled.HasValue)
                {
                    int spindleControl = 0;
                    if (config.EncoderEnabled == true) spindleControl |= 1;          // Bit 0: Primary Encoder Enable
                    if (config.SecondSpindleEnabled == true) spindleControl |= 8;     // Bit 3: Second Spindle Encoder
                    if (config.SpindleScalingEnabled == true) spindleControl |= 16;   // Bit 4: Spindle Scaling Enable
                    CNCUtils.SetParameterValue(CentroidParameters.SPINDLE_PARM, spindleControl);
                }

                // Configure deceleration time
                if (config.DecelTime.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.SPINDLE_DECEL_TIME_PARM, config.DecelTime.Value);
                }

                // Configure rigid tapping parameters
                if (config.RigidTappingSlowSpeed.HasValue || config.MinimumRigidTappingRPM.HasValue)
                {
                    double rpmValue = config.MinimumRigidTappingRPM ?? config.RigidTappingSlowSpeed ?? 0;
                    CNCUtils.SetParameterValue(CentroidParameters.RT_SLOW_SPINDLE_SPEED_PARM, rpmValue);
                }

                if (config.RigidTappingSlowTime.HasValue || config.DurationForMinRigidTappingRPM.HasValue)
                {
                    double timeValue = config.DurationForMinRigidTappingRPM ?? config.RigidTappingSlowTime ?? 0;
                    CNCUtils.SetParameterValue(CentroidParameters.RT_SLOW_SPINDLE_TIME_PARM, timeValue);
                }

                if (config.SpindleDrift.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.RT_SPINDLE_CUTOFF_DRIFT_PARM, config.SpindleDrift.Value);
                }

                if (config.RigidTappingZAxisSyncDistance.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.THREADING_AND_TAPPING_ACCEL_DECEL_ROT_DEG_STEP_AMT_PARM, config.RigidTappingZAxisSyncDistance.Value);
                }

                // Configure rigid tapping parameter 36 bit field for enable, override and index pulse settings
                if (config.RigidTappingEnabled.HasValue || config.AllowSpindleOverride.HasValue || config.DoNotWaitForIndexPulse.HasValue)
                {
                    int rigidTappingControl = 0;
                    if (config.RigidTappingEnabled == true) rigidTappingControl |= 1;           // Enable rigid tapping
                    if (config.DoNotWaitForIndexPulse == true) rigidTappingControl |= 2;       // Bit 1: Do Not Wait For Index Pulse
                    if (config.AllowSpindleOverride == true) rigidTappingControl |= 4;         // Bit 2: Allow Spindle Override
                    CNCUtils.SetParameterValue(CentroidParameters.RIGID_TAPPING_PARM, rigidTappingControl);
                }

                // Configure threading/tapping acceleration/deceleration distance
                if (config.ThreadingTappingAccelDecelDistance.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.THREADING_AND_TAPPING_ACCEL_DECEL_DISTANCE_PARM, config.ThreadingTappingAccelDecelDistance.Value);
                }

                // Configure SSV parameters
                if (config.SSVCycleTime.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.SSV_CYCLE_TIME, config.SSVCycleTime.Value);
                }

                if (config.SSVAmount.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.SSV_AMOUNT, config.SSVAmount.Value);
                }

                // Configure FRV parameters
                if (config.FRVCycleTime.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.FRV_CYCLE_TIME, config.FRVCycleTime.Value);
                }

                System.Diagnostics.Debug.WriteLine($"Configuring Spindle: {config.EncoderCounts?.ToString() ?? "not set"} counts, Max: {config.MaxSpeed?.ToString() ?? "not set"}, Min: {config.MinSpeed?.ToString() ?? "not set"}");

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region PWM Configuration Methods

        /// <summary>
        /// Configures PWM output parameters
        /// </summary>
        /// <param name="config">PWM configuration</param>
        /// <returns>True if successful</returns>
        public static bool ConfigurePWM(PWMConfiguration config)
        {
            try
            {
                bool parametersSet = false;

                if (config.Frequency.HasValue)
                {
                    CNCUtils.SetPWMFrequency(config.OutputNumber, config.Frequency.Value);
                    parametersSet = true;
                }

                if (config.Floor.HasValue)
                {
                    CNCUtils.SetPWMFloor(config.OutputNumber, (int)(config.Floor.Value * 100));
                    parametersSet = true;
                }

                // Configure PWM Options parameter bit field
                if (config.InverseOutput.HasValue || config.InverseEnabled.HasValue ||
                    config.SCommandRange1000.HasValue || config.Velocity100.HasValue ||
                    config.OnlyApplyFloorDuringVelocityMoves.HasValue || config.Floor.HasValue)
                {
                    int pwmOptions = (int)CNCUtils.GetPWMOptions(config.OutputNumber);

                    // Bit 0: Inverse Output
                    bool inverseValue = config.InverseOutput ?? config.InverseEnabled ?? false;
                    if (config.InverseOutput.HasValue || config.InverseEnabled.HasValue)
                        pwmOptions = CNCUtils.ModifyBit(pwmOptions, 0, inverseValue);

                    // Bit 1: S Command Range (true = 0-1000, false = 0-100)  
                    bool sRange1000 = config.SCommandRange1000 ?? config.Velocity100 ?? false;
                    if (config.SCommandRange1000.HasValue || config.Velocity100.HasValue)
                        pwmOptions = CNCUtils.ModifyBit(pwmOptions, 1, sRange1000);

                    // Bit 2: Only Apply Floor During Velocity Moves
                    if (config.OnlyApplyFloorDuringVelocityMoves.HasValue)
                        pwmOptions = CNCUtils.ModifyBit(pwmOptions, 2, config.OnlyApplyFloorDuringVelocityMoves.Value);
                    else if (config.Floor.HasValue && !config.OnlyApplyFloorDuringVelocityMoves.HasValue)
                        pwmOptions = CNCUtils.ModifyBit(pwmOptions, 2, config.Floor.Value > 0);

                    CNCUtils.SetPWMOptions(config.OutputNumber, pwmOptions);
                    parametersSet = true;
                }

                // Configure laser cooling fan delay timer
                if (config.LaserCoolingFanDelayTimer.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.LASER_COOLING_FAN_DELAY_TIMER, config.LaserCoolingFanDelayTimer.Value);
                    parametersSet = true;
                }

                if (parametersSet)
                {
                    System.Diagnostics.Debug.WriteLine($"Configuring PWM Output {config.OutputNumber}: " +
                        $"Freq: {config.Frequency?.ToString() ?? "not set"}Hz, " +
                        $"Floor: {config.Floor?.ToString() ?? "not set"}%, " +
                        $"S Range: {(config.SCommandRange1000 == true ? "0-1000" : config.SCommandRange1000 == false ? "0-100" : "not set")}, " +
                        $"Inverse: {config.InverseOutput?.ToString() ?? "not set"}, " +
                        $"Fan Delay: {config.LaserCoolingFanDelayTimer?.ToString() ?? "not set"}s");
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region ATC Configuration Methods

        /// <summary>
        /// Configures ATC (Automatic Tool Changer) parameters
        /// </summary>
        /// <param name="config">ATC configuration</param>
        /// <returns>True if successful</returns>
        public static bool ConfigureATC(ATCConfiguration config)
        {
            try
            {
                // Core ATC parameters:
                CNCUtils.SetParameterValue(CentroidParameters.TOOL_CHANGER_INSTALLED, config.Type != ATCType.None ? 1 : 0);
                CNCUtils.SetParameterValue(CentroidParameters.ATC_TYPE, (int)config.Type);
                CNCUtils.SetParameterValue(CentroidParameters.ATC_MAX_BINS, config.MaxBins);

                // Type-specific parameters
                switch (config.Type)
                {
                    case ATCType.Carousel:
                        CNCUtils.SetParameterValue(CentroidParameters.ATC_SKIP_FIRST_COUNT_ON_REVERSAL, config.SkipFirstCountOnReversal ? 1 : 0);
                        // Set G30 reference points for tool change position
                        // MainWindow.skin.reference.SetG30(config.ChangePositionX, config.ChangePositionY, config.ChangePositionZ);
                        break;

                    case ATCType.RackMount:
                        CNCUtils.SetParameterValue(CentroidParameters.ATC_HOLDING_CONFIGURATION, config.HoldingConfiguration);
                        CNCUtils.SetParameterValue(CentroidParameters.ATC_TOOL_LENGTH_METHOD, config.ToolLengthMethod);
                        break;

                    case ATCType.CounterTurret:
                    case ATCType.TimeTurret:
                    case ATCType.ElectricTurret:
                        CNCUtils.SetParameterValue(CentroidParameters.ATC_TIME_DELAY_TO_START, (int)(config.TimeDelayToStart * 1000));
                        CNCUtils.SetParameterValue(CentroidParameters.ATC_TIME_TO_REVERSE, (int)(config.TimeToReverse * 1000));
                        CNCUtils.SetParameterValue(CentroidParameters.ATC_TIME_TO_FAULT, (int)(config.TimeToFault * 1000));
                        CNCUtils.SetParameterValue(CentroidParameters.ATC_TIME_DELAY_TO_START_ALT, (int)(config.TimeDelayToStart * 1000));
                        if (config.Type == ATCType.TimeTurret)
                        {
                            CNCUtils.SetParameterValue(CentroidParameters.ATC_TIME_PER_TOOL_POSITION, (int)(config.TimePerToolPosition * 1000));
                        }
                        break;

                    case ATCType.AxisDrivenTurret:
                        CNCUtils.SetParameterValue(CentroidParameters.ATC_TRAVEL_PAST_DISTANCE, (int)(config.TravelPastDistance * 10000));
                        CNCUtils.SetParameterValue(CentroidParameters.ATC_TRAVEL_BEHIND_DISTANCE, (int)(config.TravelBehindDistance * 10000));
                        break;
                }

                System.Diagnostics.Debug.WriteLine($"Configuring ATC: Type {config.Type}, {config.MaxBins} bins");

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Probe Configuration Methods

        /// <summary>
        /// Configures probe and touch plate parameters
        /// </summary>
        /// <param name="config">Probe configuration</param>
        /// <returns>True if successful</returns>
        public static bool ConfigureProbe(ProbeConfiguration config)
        {
            try
            {
                bool parametersSet = false;

                // Set probe PLC input (parameter 405) - support both new and legacy properties
                int? inputNumber = config.ProbePLCInput ?? config.InputNumber;
                if (inputNumber.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.PROBE_INPUT_PARM, inputNumber.Value);
                    parametersSet = true;
                }

                // Set probe input type/state when tripped (parameter 406)
                int? inputType = null;
                if (config.InputStateWhenTripped.HasValue)
                    inputType = (int)config.InputStateWhenTripped.Value;
                else if (config.InputType.HasValue)
                    inputType = config.InputType.Value;

                if (inputType.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.PROBE_INPUT_TYPE, inputType.Value);
                    parametersSet = true;
                }

                // Set probe type (parameter 409)
                int? probeType = null;
                if (config.ProbeType.HasValue)
                    probeType = (int)config.ProbeType.Value;
                else if (config.ProbeTypeInt.HasValue)
                    probeType = config.ProbeTypeInt.Value;

                if (probeType.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.PROBE_TYPE, probeType.Value);
                    parametersSet = true;
                }

                // Set probe tool number (parameter 12)
                if (config.ProbeToolNumber.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.PROBE_TOOL_NUMBER_PARM, config.ProbeToolNumber.Value);
                    parametersSet = true;
                }

                // Set fast probe rate (parameter 14)
                if (config.FastProbeRate.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.FAST_PROBING_RATE_PARM, config.FastProbeRate.Value);
                    parametersSet = true;
                }
                else if (config.FeedRate.HasValue) // Legacy support
                {
                    CNCUtils.SetParameterValue(CentroidParameters.FAST_PROBING_RATE_PARM, config.FeedRate.Value);
                    parametersSet = true;
                }

                // Set slow probe rate (parameter 15)
                if (config.SlowProbeRate.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.SLOW_PROBING_RATE_PARM, config.SlowProbeRate.Value);
                    parametersSet = true;
                }

                // Set recovery distance (parameter 13)
                if (config.RecoveryDistance.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.PROBING_RECOVERY_DISTANCE_PARM, config.RecoveryDistance.Value);
                    parametersSet = true;
                }

                // Set maximum probing distance (need parameter number)
                if (config.MaximumProbingDistance.HasValue)
                {
                    // TODO: Add parameter number when available
                    System.Diagnostics.Debug.WriteLine($"Maximum probing distance: {config.MaximumProbingDistance.Value} (parameter number needed)");
                }

                // Configure probe protection bit field (parameter 416 or 153)
                if (config.ProbeProtectionEnabled.HasValue || config.ProbeProtectionBasedOnToolNumber.HasValue || config.ProbeInhibit.HasValue)
                {
                    int protectionValue = config.ProbeInhibit ?? 0;

                    if (config.ProbeProtectionEnabled.HasValue)
                        protectionValue = CNCUtils.ModifyBit(protectionValue, 0, config.ProbeProtectionEnabled.Value);

                    if (config.ProbeProtectionBasedOnToolNumber.HasValue)
                        protectionValue = CNCUtils.ModifyBit(protectionValue, 1, config.ProbeProtectionBasedOnToolNumber.Value);

                    CNCUtils.SetParameterValue(CentroidParameters.PROBE_INHIBIT_PARM, protectionValue);
                    parametersSet = true;
                }

                // Set display warning (parameter 410 or 153)
                bool? displayWarning = config.DisplayWarningToVerifyProbe ?? config.DisplayProbeWarning;
                if (displayWarning.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.DISPLAY_PROBE_WARNING_PARAM, displayWarning.Value ? 1 : 0);
                    parametersSet = true;
                }

                // Set inhibit spindle when detect is on
                if (config.InhibitSpindleWhenDetectOn.HasValue)
                {
                    // TODO: Add parameter number when available
                    System.Diagnostics.Debug.WriteLine($"Inhibit spindle when detect on: {config.InhibitSpindleWhenDetectOn.Value} (parameter number needed)");
                }

                // Configure jogging speed limits for each axis
                if (config.ProbeSlowJogSpeeds != null)
                {
                    for (int axis = 0; axis < Math.Min(config.ProbeSlowJogSpeeds.Length, 4); axis++)
                    {
                        CNCUtils.SetAxisRate(axis + 1, CNCPipe.Axis.Rate.SLOW_JOG_PROBE, config.ProbeSlowJogSpeeds[axis]);
                    }
                    parametersSet = true;
                }

                if (config.ProbeFastJogNegativeSpeeds != null)
                {
                    for (int axis = 0; axis < Math.Min(config.ProbeFastJogNegativeSpeeds.Length, 4); axis++)
                    {
                        CNCUtils.SetAxisRate(axis + 1, CNCPipe.Axis.Rate.FAST_JOG_MINUS_PROBE, config.ProbeFastJogNegativeSpeeds[axis]);
                    }
                    parametersSet = true;
                }

                if (config.ProbeFastJogPositiveSpeeds != null)
                {
                    for (int axis = 0; axis < Math.Min(config.ProbeFastJogPositiveSpeeds.Length, 4); axis++)
                    {
                        CNCUtils.SetAxisRate(axis + 1, CNCPipe.Axis.Rate.FAST_JOG_PLUS_PROBE, config.ProbeFastJogPositiveSpeeds[axis]);
                    }
                    parametersSet = true;
                }

                // Legacy touch plate configuration
                if (config.TouchPlateInputNumber.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.TOUCH_PLATE_INPUT_NUMBER, config.TouchPlateInputNumber.Value);
                    parametersSet = true;
                }

                if (config.TouchPlateInputType.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.TOUCH_PLATE_INPUT_TYPE, config.TouchPlateInputType.Value);
                    parametersSet = true;
                }

                if (parametersSet)
                {
                    System.Diagnostics.Debug.WriteLine($"Configuring Probe: Input {inputNumber?.ToString() ?? "not set"}, " +
                        $"Type: {config.ProbeType?.ToString() ?? "not set"}, " +
                        $"Tool: {config.ProbeToolNumber?.ToString() ?? "not set"}, " +
                        $"Fast Rate: {config.FastProbeRate?.ToString() ?? "not set"}, " +
                        $"Slow Rate: {config.SlowProbeRate?.ToString() ?? "not set"}");
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Touch Plate Configuration Methods

        /// <summary>
        /// Configures touch plate parameters
        /// </summary>
        /// <param name="config">Touch plate configuration</param>
        /// <returns>True if successful</returns>
        public static bool ConfigureTouchPlate(TouchPlateConfiguration config)
        {
            try
            {
                bool parametersSet = false;

                // Set touch plate parameters using CNCUtils - only if provided
                if (config.InputNumber.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.TOUCH_PLATE_INPUT, config.InputNumber.Value);
                    parametersSet = true;
                }

                if (config.DetectInput.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.TOUCH_PLATE_DETECT, config.DetectInput.Value);
                    parametersSet = true;
                }

                if (config.InputType.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.TOUCH_PLATE_INPUT_TYPE_PARM, config.InputType.Value);
                    parametersSet = true;
                }

                if (config.WallHeight.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.TOUCH_PLATE_WALL_HEIGHT_PARM, config.WallHeight.Value);
                    parametersSet = true;
                }

                if (config.WallThickness.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.TOUCH_PLATE_WALL_THICKNESS_PARM, config.WallThickness.Value);
                    parametersSet = true;
                }

                if (config.InternalDiameter.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.TOUCH_PLATE_INTERNAL_DIAMETER_PARM, config.InternalDiameter.Value);
                    parametersSet = true;
                }

                if (config.MaxDistance.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.TOUCH_PLATE_MAX_DISTANCE_PARM, config.MaxDistance.Value);
                    parametersSet = true;
                }

                if (config.RetractDistance.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.TOUCH_PLATE_RETRACT_DISTANCE_PARM, config.RetractDistance.Value);
                    parametersSet = true;
                }

                if (config.FastRate.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.TOUCH_PLATE_FAST_RATE_PARM, config.FastRate.Value);
                    parametersSet = true;
                }

                if (config.SlowRate.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.TOUCH_PLATE_SLOW_RATE_PARM, config.SlowRate.Value);
                    parametersSet = true;
                }

                // Configure touch plate attributes bit field only if options are provided
                if (config.InsideTouch.HasValue || config.BoreEnabled.HasValue || config.SurfacePlate.HasValue)
                {
                    int touchPlateAttributes = (int)CNCUtils.GetParameterValue(CentroidParameters.TOUCH_PLATE_ATTRIBUTES_PARM);

                    if (config.InsideTouch.HasValue)
                        touchPlateAttributes = CNCUtils.ModifyBit(touchPlateAttributes, 0, config.InsideTouch.Value);

                    if (config.BoreEnabled.HasValue)
                        touchPlateAttributes = CNCUtils.ModifyBit(touchPlateAttributes, 1, config.BoreEnabled.Value);

                    if (config.SurfacePlate.HasValue)
                        touchPlateAttributes = CNCUtils.ModifyBit(touchPlateAttributes, 2, config.SurfacePlate.Value);

                    CNCUtils.SetParameterValue(CentroidParameters.TOUCH_PLATE_ATTRIBUTES_PARM, touchPlateAttributes);
                    parametersSet = true;
                }

                if (parametersSet)
                {
                    System.Diagnostics.Debug.WriteLine($"Configuring Touch Plate: Fast Rate: {config.FastRate?.ToString() ?? "not set"}, Slow Rate: {config.SlowRate?.ToString() ?? "not set"}");
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Tool Touch Off Configuration Methods

        /// <summary>
        /// Configures tool touch off parameters
        /// </summary>
        /// <param name="config">Tool touch off configuration</param>
        /// <returns>True if successful</returns>
        public static bool ConfigureToolTouchOff(ToolTouchOffConfiguration config)
        {
            try
            {
                bool parametersSet = false;

                // Set touch off tool PLC input (Parameter 44 for Mill, 244 for Lathe)
                if (config.TouchOffToolPLCInput.HasValue)
                {
                    // TODO: Determine if this is Mill or Lathe - for now use Mill parameter
                    CNCUtils.SetParameterValue(CentroidParameters.TOUCH_OFF_TOOL_PLC_INPUT_MILL, config.TouchOffToolPLCInput.Value);
                    parametersSet = true;
                }

                // Set tool touch off type (Parameter 405)
                if (config.ToolTouchOffType.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.TOOL_TOUCH_OFF_TYPE_PARM, (int)config.ToolTouchOffType.Value);
                    parametersSet = true;
                }

                // Set input state when triggered (Parameter 407)
                if (config.InputStateWhenTriggered.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.TOOL_TOUCH_OFF_INPUT_TYPE_PARM, (int)config.InputStateWhenTriggered.Value);
                    parametersSet = true;
                }

                // Set tool touch off detect input (Parameter 257)
                if (config.DetectInputNumber.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.TOOL_TOUCH_OFF_DETECT_INPUT_PARM, config.DetectInputNumber.Value);
                    parametersSet = true;
                }

                // Set TT height (Parameter 71)
                if (config.TTHeight.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.TOOL_TOUCH_OFF_HEIGHT_PARM, config.TTHeight.Value);
                    parametersSet = true;
                }

                // Set fixed location mode (Parameter 17)
                if (config.UseFixedLocation.HasValue)
                {
                    int locationMode = config.UseFixedLocation.Value ? 3 : 0; // 3=Fixed, 0=Moveable
                    CNCUtils.SetParameterValue(CentroidParameters.FIXED_LOCATION_MODE_PARM, locationMode);
                    parametersSet = true;
                }

                // Set G30 P3 reference points for fixed location coordinates
                if (config.FixedLocationX.HasValue)
                {
                    CNCUtils.SetWorkpieceReferencePoint(ReferencePoints.G30P3, 1, config.FixedLocationX.Value); // X axis
                    parametersSet = true;
                }

                if (config.FixedLocationY.HasValue)
                {
                    CNCUtils.SetWorkpieceReferencePoint(ReferencePoints.G30P3, 2, config.FixedLocationY.Value); // Y axis
                    parametersSet = true;
                }

                if (config.ZClearanceHeight.HasValue)
                {
                    CNCUtils.SetWorkpieceReferencePoint(ReferencePoints.G30P3, 3, config.ZClearanceHeight.Value); // Z axis
                    parametersSet = true;
                }

                // Configure tool measure properties bit field (Parameter 43) and modal tool parameter (Parameter 3)
                if (config.ProbeProtectionEnabled.HasValue || config.SubtractHeightWhenSettingOffsets.HasValue)
                {
                    // Set probe protection in tool measure properties (Parameter 43)
                    if (config.ProbeProtectionEnabled.HasValue)
                    {
                        int toolMeasureProps = (int)CNCUtils.GetParameterValue(CentroidParameters.TOOL_MEASURE_PROPERTIES_PARM);
                        toolMeasureProps = CNCUtils.ModifyBit(toolMeasureProps, 0, config.ProbeProtectionEnabled.Value);
                        CNCUtils.SetParameterValue(CentroidParameters.TOOL_MEASURE_PROPERTIES_PARM, toolMeasureProps);
                        parametersSet = true;
                    }

                    // Set height calculation method in modal tool parameter (Parameter 3 bit 1)
                    if (config.SubtractHeightWhenSettingOffsets.HasValue)
                    {
                        int modalToolParam = (int)CNCUtils.GetParameterValue(CentroidParameters.MODAL_TOOL_PARM);
                        modalToolParam = CNCUtils.ModifyBit(modalToolParam, 1, config.SubtractHeightWhenSettingOffsets.Value);
                        CNCUtils.SetParameterValue(CentroidParameters.MODAL_TOOL_PARM, modalToolParam);
                        parametersSet = true;
                    }
                }

                if (parametersSet)
                {
                    System.Diagnostics.Debug.WriteLine($"Configuring Tool Touch Off: Input {config.TouchOffToolPLCInput?.ToString() ?? "not set"}, " +
                        $"Type: {config.ToolTouchOffType?.ToString() ?? "not set"}, " +
                        $"Fixed Location: {config.UseFixedLocation?.ToString() ?? "not set"}, " +
                        $"TT Height: {config.TTHeight?.ToString() ?? "not set"}");
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Second Spindle Configuration Methods

        /// <summary>
        /// Configures second spindle parameters
        /// </summary>
        /// <param name="config">Second spindle configuration</param>
        /// <returns>True if successful</returns>
        public static bool ConfigureSecondSpindle(SecondSpindleConfiguration config)
        {
            try
            {
                bool parametersSet = false;

                // Set second spindle parameters using CNCUtils - only if provided
                if (config.Enabled.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.SECOND_SPINDLE_ENABLE, config.Enabled.Value ? 1 : 0);
                    parametersSet = true;
                }

                if (config.MaxSpeed.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.SECOND_SPINDLE_MAX_SPEED, config.MaxSpeed.Value);
                    parametersSet = true;
                }

                if (config.MinSpeed.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.SECOND_SPINDLE_MIN_SPEED, config.MinSpeed.Value);
                    parametersSet = true;
                }

                if (config.EncoderCounts.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.SECOND_SPINDLE_ENCODER_COUNTS, config.EncoderCounts.Value);
                    parametersSet = true;
                }

                if (parametersSet)
                {
                    System.Diagnostics.Debug.WriteLine($"Configuring Second Spindle: Enabled: {config.Enabled?.ToString() ?? "not set"}, Max Speed: {config.MaxSpeed?.ToString() ?? "not set"}");
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Global System Configuration Methods

        /// <summary>
        /// Configures global system parameters
        /// </summary>
        /// <param name="config">Global system configuration</param>
        /// <returns>True if successful</returns>
        public static bool ConfigureGlobalSystem(GlobalSystemConfiguration config)
        {
            try
            {
                bool parametersSet = false;

                // Set global step frequency - only if provided
                if (config.StepFrequency.HasValue)
                {
                    // Calculate parameter value from step frequency
                    const int PulseStepFrequency = 1200000;
                    double parameterValue = PulseStepFrequency / (double)(int)config.StepFrequency.Value;
                    CNCUtils.SetParameterValue(CentroidParameters.ACORN_STEPPER_PULSE_RATE_PARM, parameterValue);
                    parametersSet = true;
                }

                if (config.DriveFaultDelay.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.PLC_CLEARPATH_OR_G540, config.DriveFaultDelay.Value);
                    parametersSet = true;
                }

                if (config.AxisSignalInversion.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.ACORN_OUTPUT_INVERSION_PARM, config.AxisSignalInversion.Value);
                    parametersSet = true;
                }

                if (config.LowResolutionMode.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.AD2_LOW_RESOLUTION_PARM, config.LowResolutionMode.Value ? 1 : 0);
                    parametersSet = true;
                }

                if (config.ChargePumpDivider.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.CHARGE_PUMP_PARM, config.ChargePumpDivider.Value);
                    parametersSet = true;
                }

                if (parametersSet)
                {
                    System.Diagnostics.Debug.WriteLine($"Configuring Global System: Step Frequency: {config.StepFrequency?.ToString() ?? "not set"}, Drive Fault Delay: {config.DriveFaultDelay?.ToString() ?? "not set"}");
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Rotary Configuration Methods

        /// <summary>
        /// Configures global rotary axis parameters
        /// </summary>
        /// <param name="config">Rotary configuration</param>
        /// <returns>True if successful</returns>
        public static bool ConfigureRotary(RotaryConfiguration config)
        {
            try
            {
                bool parametersSet = false;

                // Set rotary jog increment - only if provided
                if (config.JogIncrement.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.ROTARY_JOG_INCREMENT_PARM, config.JogIncrement.Value);
                    parametersSet = true;
                }

                // Handle CNC compatibility parameter bit flags
                if (config.SlaveRotaryFeedrateToLinear.HasValue || config.PreventRotaryModalFeedrate.HasValue)
                {
                    // Get current CNC compatibility parameter value
                    int compatibilityFlags = (int)CNCUtils.GetParameterValue(CentroidParameters.CNC_COMPATIBILITY_PARM);

                    // Bit 3: Slave rotary axis feedrate to linear move feedrate
                    if (config.SlaveRotaryFeedrateToLinear.HasValue)
                    {
                        compatibilityFlags = CNCUtils.ModifyBit(compatibilityFlags, 3, config.SlaveRotaryFeedrateToLinear.Value);
                    }

                    // Bit 5: Rotary-only moves won't use modal feedrate
                    if (config.PreventRotaryModalFeedrate.HasValue)
                    {
                        compatibilityFlags = CNCUtils.ModifyBit(compatibilityFlags, 5, config.PreventRotaryModalFeedrate.Value);
                    }

                    CNCUtils.SetParameterValue(CentroidParameters.CNC_COMPATIBILITY_PARM, compatibilityFlags);
                    parametersSet = true;
                }

                if (parametersSet)
                {
                    System.Diagnostics.Debug.WriteLine($"Configuring Rotary: Jog Increment: {config.JogIncrement?.ToString() ?? "not set"} degrees");
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region System Hardware Detection Methods

        /// <summary>
        /// Detects system hardware capabilities and I/O configuration
        /// </summary>
        /// <returns>System hardware information</returns>
        public static SystemHardwareInfo DetectSystemHardware()
        {
            try
            {
                var cncPipe = CNCConnectionManager.GetCNCPipe();

                // CNCConnectionManager ensures the pipe is already constructed
                if (cncPipe == null)
                {
                    // Return minimal system info on error
                    return new SystemHardwareInfo
                    {
                        SystemType = "Unknown",
                        BaseInputs = 8,
                        BaseOutputs = 8,
                        ExpansionBoards = 0,
                        TotalInputs = 8,
                        TotalOutputs = 8,
                        AvailableInputs = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 },
                        AvailableOutputs = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 }
                    };
                }

                var info = new SystemHardwareInfo();

                // Detect system type
                // Note: This would require access to system detection methods
                // For now, we'll set defaults and this can be enhanced when the API is available
                info.SystemType = "Unknown";
                info.BaseInputs = 8;
                info.BaseOutputs = 8;
                info.ExpansionBoards = 0;
                info.TotalInputs = info.BaseInputs;
                info.TotalOutputs = info.BaseOutputs;

                // Populate available I/O lists
                for (int i = 1; i <= info.TotalInputs; i++)
                {
                    info.AvailableInputs.Add(i);
                }

                for (int i = 1; i <= info.TotalOutputs; i++)
                {
                    info.AvailableOutputs.Add(i);
                }

                System.Diagnostics.Debug.WriteLine($"Detected System: {info.SystemType}, I/O: {info.TotalInputs} inputs, {info.TotalOutputs} outputs");

                return info;
            }
            catch (Exception)
            {
                // Return minimal system info on error
                return new SystemHardwareInfo
                {
                    SystemType = "Error",
                    BaseInputs = 8,
                    BaseOutputs = 8,
                    TotalInputs = 8,
                    TotalOutputs = 8
                };
            }
        }

        #endregion

        #region Configuration Methods

        /// <summary>
        /// Configures a complete machine setup with all systems
        /// </summary>
        /// <param name="inputs">Input I/O functions</param>
        /// <param name="outputs">Output I/O functions</param>
        /// <param name="axes">Axis configurations</param>
        /// <param name="spindle">Spindle configuration</param>
        /// <param name="probe">Probe configuration (optional)</param>
        /// <param name="pwmOutputs">PWM output configurations (optional)</param>
        /// <param name="atc">ATC configuration (optional)</param>
        /// <returns>True if successful</returns>
        public static bool ConfigureCompleteMachine(
            List<IOFunction> inputs,
            List<IOFunction> outputs,
            List<AxisConfiguration> axes,
            SpindleConfiguration spindle,
            ProbeConfiguration? probe = null,
            List<PWMConfiguration>? pwmOutputs = null,
            ATCConfiguration? atc = null)
        {
            return ConfigureCompleteMachine(inputs, outputs, axes, spindle, probe, pwmOutputs, atc, null, null, null, null);
        }

        /// <summary>
        /// Configures a complete machine setup with all systems including enhanced features
        /// </summary>
        /// <param name="inputs">Input I/O functions</param>
        /// <param name="outputs">Output I/O functions</param>
        /// <param name="axes">Axis configurations</param>
        /// <param name="spindle">Spindle configuration</param>
        /// <param name="probe">Probe configuration (optional)</param>
        /// <param name="pwmOutputs">PWM output configurations (optional)</param>
        /// <param name="atc">ATC configuration (optional)</param>
        /// <param name="touchPlate">Touch plate configuration (optional)</param>
        /// <param name="secondSpindle">Second spindle configuration (optional)</param>
        /// <param name="globalSystem">Global system configuration (optional)</param>
        /// <param name="rotary">Rotary axis configuration (optional)</param>
        /// <returns>True if successful</returns>
        public static bool ConfigureCompleteMachine(
            List<IOFunction> inputs,
            List<IOFunction> outputs,
            List<AxisConfiguration> axes,
            SpindleConfiguration spindle,
            ProbeConfiguration? probe = null,
            List<PWMConfiguration>? pwmOutputs = null,
            ATCConfiguration? atc = null,
            TouchPlateConfiguration? touchPlate = null,
            SecondSpindleConfiguration? secondSpindle = null,
            GlobalSystemConfiguration? globalSystem = null,
            RotaryConfiguration? rotary = null)
        {
            try
            {
                // Step 1: Configure global system settings first if provided
                if (globalSystem != null && !ConfigureGlobalSystem(globalSystem))
                {
                    return false;
                }

                // Step 2: Configure I/O in PLC file
                if (!ConfigureInputsOutputs(inputs, outputs))
                {
                    return false;
                }

                // Step 3: Configure all axes
                foreach (var axis in axes)
                {
                    if (!ConfigureAxis(axis))
                    {
                        return false;
                    }
                }

                // Step 4: Configure spindle
                if (!ConfigureSpindle(spindle))
                {
                    return false;
                }

                // Step 5: Configure second spindle if provided
                if (secondSpindle != null && !ConfigureSecondSpindle(secondSpindle))
                {
                    return false;
                }

                // Step 6: Configure probe if provided
                if (probe != null && !ConfigureProbe(probe))
                {
                    return false;
                }

                // Step 7: Configure touch plate if provided
                if (touchPlate != null && !ConfigureTouchPlate(touchPlate))
                {
                    return false;
                }

                // Step 8: Configure PWM outputs if provided
                if (pwmOutputs != null)
                {
                    foreach (var pwm in pwmOutputs)
                    {
                        if (!ConfigurePWM(pwm))
                        {
                            return false;
                        }
                    }
                }

                // Step 9: Configure ATC if provided
                if (atc != null && !ConfigureATC(atc))
                {
                    return false;
                }

                // Step 10: Configure rotary settings if provided
                if (rotary != null && !ConfigureRotary(rotary))
                {
                    return false;
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Configures inputs and outputs in PLC file
        /// </summary>
        /// <param name="inputs">List of input functions</param>
        /// <param name="outputs">List of output functions</param>
        /// <returns>True if successful</returns>
        public static bool ConfigureInputsOutputs(
            List<IOFunction> inputs,
            List<IOFunction> outputs)
        {
            try
            {
                // Copy template to working file
                if (!CopyTemplateFile())
                {
                    return false;
                }

                // Update wizard regions
                if (!UpdateWizardRegion("Inputs", inputs))
                {
                    return false;
                }

                if (!UpdateWizardRegion("Outputs", outputs))
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
        /// Inverts input polarity using CNC12 parameters
        /// </summary>
        /// <param name="inputNumber">Input number to invert (1-80)</param>
        /// <param name="invert">True to invert, false to restore normal polarity</param>
        /// <returns>True if successful</returns>
        public static bool InvertInput(int inputNumber, bool invert)
        {
            try
            {
                // Get bit position for this input
                int bitPosition = GetBitPosition(inputNumber);
                if (bitPosition == -1) return false;

                var cncPipe = CNCConnectionManager.GetCNCPipe();

                // CNCConnectionManager ensures the pipe is already constructed
                if (cncPipe == null)
                {
                    return false;
                }

                // Try to use the CentroidAPI to modify input inversion parameters
                // Based on the documentation, parameter manipulation should be possible
                // The exact method names may vary - this is a framework for implementation

                try
                {
                    // Get the parameter number for this input
                    CentroidParameters parameterNumber = GetInputInversionParameter(inputNumber);

                    // Get current parameter value and cast to int for bit operations
                    var currentValue = (int)CNCUtils.GetParameterValue(parameterNumber);

                    // Calculate the bit mask for this input
                    int bitMask = 1 << bitPosition;
                    int newValue;

                    // Set or clear the bit based on invert flag
                    if (invert)
                        newValue = currentValue | bitMask;  // Set bit
                    else
                        newValue = currentValue & ~bitMask; // Clear bit

                    // Update the parameter
                    CNCUtils.SetParameterValue(parameterNumber, newValue);

                    System.Diagnostics.Debug.WriteLine($"Input {inputNumber}: Parameter {parameterNumber}, Bit {bitPosition}, Invert: {invert}, Value: {currentValue} -> {newValue}");

                    return true;
                }
                catch
                {
                    // If direct parameter access fails, we might need to use a different approach
                    // such as writing to parameter files or using other CentroidAPI methods
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Inverts multiple inputs using CNC12 parameters
        /// </summary>
        /// <param name="inputSettings">Dictionary of input number to invert setting</param>
        /// <returns>True if all operations successful</returns>
        public static bool InvertInputs(Dictionary<int, bool> inputSettings)
        {
            try
            {
                var cncPipe = CNCConnectionManager.GetCNCPipe();

                // CNCConnectionManager ensures the pipe is already constructed
                if (cncPipe == null)
                {
                    return false;
                }

                // Group inputs by parameter number for efficiency
                var parameterGroups = new Dictionary<CentroidParameters, List<(int inputNum, bool invert)>>();

                foreach (var setting in inputSettings)
                {
                    CentroidParameters parameterNumber = GetInputInversionParameter(setting.Key);

                    if (!parameterGroups.ContainsKey(parameterNumber))
                    {
                        parameterGroups[parameterNumber] = new List<(int, bool)>();
                    }
                    parameterGroups[parameterNumber].Add((setting.Key, setting.Value));
                }

                bool allSuccessful = true;

                // Process each parameter group
                foreach (var group in parameterGroups)
                {
                    try
                    {
                        CentroidParameters parameterNumber = group.Key;

                        // Get current parameter value
                        double currentValue = CNCUtils.GetParameterValue(parameterNumber);
                        int currentIntValue = (int)currentValue;

                        // Modify the bits for each input in this parameter
                        foreach (var (inputNum, invert) in group.Value)
                        {
                            int bitPosition = GetBitPosition(inputNum);
                            if (bitPosition >= 0)
                            {
                                currentIntValue = CNCUtils.ModifyBit(currentIntValue, bitPosition, invert);
                            }
                        }

                        // Set the modified parameter value
                        CNCUtils.SetParameterValue(parameterNumber, currentIntValue);
                    }
                    catch (Exception)
                    {
                        allSuccessful = false;
                    }
                }

                return allSuccessful;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #region Validation Methods

        /// <summary>
        /// Validates I/O configuration for conflicts and issues
        /// </summary>
        /// <param name="inputs">Input functions to validate</param>
        /// <param name="outputs">Output functions to validate</param>
        /// <returns>List of validation messages</returns>
        public static List<string> ValidateIOConfiguration(List<IOFunction> inputs, List<IOFunction> outputs)
        {
            var issues = new List<string>();

            // Check for duplicate input numbers
            var inputNumbers = inputs.Select(i => i.Number).ToList();
            var duplicateInputs = inputNumbers.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key);
            foreach (var dup in duplicateInputs)
            {
                issues.Add($"Duplicate input number: {dup}");
            }

            // Check for duplicate output numbers
            var outputNumbers = outputs.Select(o => o.Number).ToList();
            var duplicateOutputs = outputNumbers.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key);
            foreach (var dup in duplicateOutputs)
            {
                issues.Add($"Duplicate output number: {dup}");
            }

            // Check for invalid I/O numbers (standard range 1-64)
            var invalidInputs = inputs.Where(i => i.Number < 1 || i.Number > 64);
            foreach (var input in invalidInputs)
            {
                issues.Add($"Input number {input.Number} is out of valid range (1-64)");
            }

            var invalidOutputs = outputs.Where(o => o.Number < 1 || o.Number > 64);
            foreach (var output in invalidOutputs)
            {
                issues.Add($"Output number {output.Number} is out of valid range (1-64)");
            }

            return issues;
        }

        /// <summary>
        /// Validates ATC configuration for issues
        /// </summary>
        /// <param name="atc">ATC configuration to validate</param>
        /// <returns>List of validation messages</returns>
        public static List<string> ValidateATCConfiguration(ATCConfiguration atc)
        {
            var issues = new List<string>();

            if (atc.Type == ATCType.None)
            {
                return issues; // No validation needed for disabled ATC
            }

            // Validate tool count
            if (atc.MaxBins < 1 || atc.MaxBins > 99)
            {
                issues.Add($"ATC tool count {atc.MaxBins} is out of valid range (1-99)");
            }

            // Type-specific validation
            switch (atc.Type)
            {
                case ATCType.Carousel:
                    // Position validation for carousel
                    if (atc.ChangePositionZ <= 0)
                    {
                        issues.Add("ATC change position Z must be positive (safe height)");
                    }
                    break;

                case ATCType.CounterTurret:
                case ATCType.TimeTurret:
                case ATCType.ElectricTurret:
                    // Timing validation for turret systems
                    if (atc.TimeDelayToStart < 0)
                    {
                        issues.Add("ATC time delay to start cannot be negative");
                    }
                    if (atc.TimeToReverse <= 0)
                    {
                        issues.Add("ATC time to reverse must be positive");
                    }
                    if (atc.TimeToFault <= atc.TimeToReverse)
                    {
                        issues.Add("ATC time to fault must be greater than time to reverse");
                    }
                    if (atc.Type == ATCType.TimeTurret && atc.TimePerToolPosition <= 0)
                    {
                        issues.Add("Time turret requires positive time per tool position");
                    }
                    break;

                case ATCType.AxisDrivenTurret:
                    // Distance validation for axis-driven systems
                    if (atc.TravelPastDistance < 0)
                    {
                        issues.Add("ATC travel past distance cannot be negative");
                    }
                    if (atc.TravelBehindDistance < 0)
                    {
                        issues.Add("ATC travel behind distance cannot be negative");
                    }
                    break;

                case ATCType.RackMount:
                    // Position validation for rack mount
                    if (atc.ChangePositionZ <= 0)
                    {
                        issues.Add("ATC change position Z must be positive (safe height)");
                    }
                    // Rack mount specific validation
                    if (atc.HoldingConfiguration < 0 || atc.HoldingConfiguration > 1)
                    {
                        issues.Add("Rack mount holding configuration must be 0 (Hole) or 1 (Fork)");
                    }
                    if (atc.ToolLengthMethod < 0 || atc.ToolLengthMethod > 1)
                    {
                        issues.Add("Tool length method must be 0 (Fixed position) or 1 (Surface plate)");
                    }
                    break;
            }

            return issues;
        }

        #endregion

        /// <summary>
        /// Gets the parameter number for input inversion based on input number
        /// </summary>
        /// <param name="inputNumber">Input number (1-80)</param>
        /// <returns>Parameter enum value</returns>
        private static CentroidParameters GetInputInversionParameter(int inputNumber)
        {
            if (inputNumber >= 1 && inputNumber <= 16) return CentroidParameters.INPUT_INVERSION_1_16;
            if (inputNumber >= 17 && inputNumber <= 32) return CentroidParameters.INPUT_INVERSION_17_32;
            if (inputNumber >= 33 && inputNumber <= 48) return CentroidParameters.INPUT_INVERSION_33_48;
            if (inputNumber >= 49 && inputNumber <= 64) return CentroidParameters.INPUT_INVERSION_49_64;
            if (inputNumber >= 65 && inputNumber <= 80) return CentroidParameters.INPUT_INVERSION_65_80;
            throw new ArgumentException($"Invalid input number: {inputNumber}");
        }

        /// <summary>
        /// Gets the bit position within the parameter for the given input number
        /// </summary>
        /// <param name="inputNumber">Input number (1-80)</param>
        /// <returns>Bit position (0-15) or -1 if invalid</returns>
        private static int GetBitPosition(int inputNumber)
        {
            if (inputNumber >= 1 && inputNumber <= 16) return inputNumber - 1;
            if (inputNumber >= 17 && inputNumber <= 32) return inputNumber - 17;
            if (inputNumber >= 33 && inputNumber <= 48) return inputNumber - 33;
            if (inputNumber >= 49 && inputNumber <= 64) return inputNumber - 49;
            if (inputNumber >= 65 && inputNumber <= 80) return inputNumber - 65;
            return -1;
        }

        #endregion
    }
}