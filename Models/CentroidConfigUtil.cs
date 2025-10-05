using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CentroidAPI;
using HavenCNCServer.CentriodAPI;

namespace HavenCNCServer.Models
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
    public static class CentroidConfigUtil
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

        #region Input/Output Definitions

        /// <summary>
        /// Represents an I/O function assignment
        /// </summary>
        public class IOFunction
        {
            /// <summary>
            /// Function name (e.g., "EStopOk", "SpindleEnable")
            /// </summary>
            public string? Name { get; set; }
            
            /// <summary>
            /// I/O number (1-64 for standard I/O)
            /// </summary>
            public int? Number { get; set; }
            
            /// <summary>
            /// Whether the input/output is inverted
            /// </summary>
            public bool? IsInverted { get; set; }
        }

        #endregion

        #region Axis Configuration Classes

        /// <summary>
        /// Represents axis configuration parameters
        /// </summary>
        public class AxisConfiguration
        {
            /// <summary>
            /// Axis number (1-8) - Required
            /// </summary>
            public int AxisNumber { get; set; }
            
            /// <summary>
            /// Axis label (X, Y, Z, A, B, C, U, V, W)
            /// </summary>
            public string? Label { get; set; }
            
            /// <summary>
            /// Steps per revolution (motor/drive steps)
            /// </summary>
            public int? StepsPerRevolution { get; set; }
            
            /// <summary>
            /// Turn ratio - distance per revolution (inches/mm per rev for linear, degrees for rotary)
            /// </summary>
            public double? TurnRatio { get; set; }
            
            /// <summary>
            /// Plus travel limit
            /// </summary>
            public double? PlusTravelLimit { get; set; }
            
            /// <summary>
            /// Minus travel limit
            /// </summary>
            public double? MinusTravelLimit { get; set; }
            
            /// <summary>
            /// Backlash compensation amount
            /// </summary>
            public double? BacklashCompensation { get; set; }
            
            /// <summary>
            /// Slow jog rate
            /// </summary>
            public double? SlowJogRate { get; set; }
            
            /// <summary>
            /// Fast jog rate
            /// </summary>
            public double? FastJogRate { get; set; }
            
            /// <summary>
            /// Acceleration time in seconds
            /// </summary>
            public double? AccelerationTime { get; set; }
            
            /// <summary>
            /// Whether axis direction is reversed
            /// </summary>
            public bool? IsReversed { get; set; }
            
            /// <summary>
            /// Master axis for pairing (0 = none, 1-8 = master axis number)
            /// </summary>
            public int? MasterAxis { get; set; }
            
            /// <summary>
            /// Whether this is a rotary axis (Parameter bit 0)
            /// </summary>
            public bool? IsRotary { get; set; }
            
            /// <summary>
            /// Rotary DRO wrap around display mode (Parameter bit 1)
            /// </summary>
            public bool? RotaryWrapAround { get; set; }
            
            /// <summary>
            /// C-Axis enable (Parameter bit 4)
            /// </summary>
            public bool? CAxisEnabled { get; set; }
            
            /// <summary>
            /// Prevent divide by 360 for C-Axis (Parameter bit 7)
            /// </summary>
            public bool? PreventDivideBy360 { get; set; }
            
            /// <summary>
            /// Hide axis from DRO display - ATC Turret (Parameter bit 9)
            /// </summary>
            public bool? HideFromDRO { get; set; }
            
            /// <summary>
            /// Rotary axis parallel to X (Parameter bit 11)
            /// </summary>
            public bool? ParallelToX { get; set; }
            
            /// <summary>
            /// Rotary axis parallel to Y (Parameter bit 12)
            /// </summary>
            public bool? ParallelToY { get; set; }
        }

        /// <summary>
        /// Represents spindle configuration parameters
        /// </summary>
        public class SpindleConfiguration
        {
            /// <summary>
            /// Encoder counts per spindle revolution
            /// </summary>
            public int? EncoderCounts { get; set; }
            
            /// <summary>
            /// Spindle axis number (5 standard, 8 for AcornSix/Hickory)
            /// </summary>
            public int? SpindleAxis { get; set; }
            
            /// <summary>
            /// Low gear ratio
            /// </summary>
            public double? LowGearRatio { get; set; }
            
            /// <summary>
            /// Medium gear ratio
            /// </summary>
            public double? MediumGearRatio { get; set; }
            
            /// <summary>
            /// High gear ratio
            /// </summary>
            public double? HighGearRatio { get; set; }
            
            /// <summary>
            /// Maximum spindle speed
            /// </summary>
            public int? MaxSpeed { get; set; }
            
            /// <summary>
            /// Minimum spindle speed
            /// </summary>
            public int? MinSpeed { get; set; }
            
            /// <summary>
            /// Analog output voltage range (0-3)
            /// </summary>
            public int? AnalogRange { get; set; }
            
            /// <summary>
            /// Spindle OK delay in seconds
            /// </summary>
            public double? OkDelay { get; set; }
            
            /// <summary>
            /// Cooling fan delay in seconds
            /// </summary>
            public double? FanDelay { get; set; }
            
            /// <summary>
            /// Enable spindle encoder
            /// </summary>
            public bool? EncoderEnabled { get; set; }
            
            /// <summary>
            /// Enable rigid tapping
            /// </summary>
            public bool? RigidTappingEnabled { get; set; }
            
            /// <summary>
            /// Enable RTG (Real Time Graphics) display
            /// </summary>
            public bool? RTGDisplay { get; set; }
            
            /// <summary>
            /// Enable second spindle
            /// </summary>
            public bool? SecondSpindleEnabled { get; set; }
            
            /// <summary>
            /// Spindle deceleration time in seconds
            /// </summary>
            public double? DecelTime { get; set; }
            
            /// <summary>
            /// Rigid tapping slow spindle speed
            /// </summary>
            public double? RigidTappingSlowSpeed { get; set; }
            
            /// <summary>
            /// Rigid tapping slow spindle time
            /// </summary>
            public double? RigidTappingSlowTime { get; set; }
            
            /// <summary>
            /// Threading and tapping acceleration/deceleration distance
            /// </summary>
            public double? ThreadingTappingAccelDecelDistance { get; set; }
            
            /// <summary>
            /// SSV (Spindle Speed Variation) cycle time
            /// </summary>
            public double? SSVCycleTime { get; set; }
            
            /// <summary>
            /// SSV (Spindle Speed Variation) amount percentage
            /// </summary>
            public double? SSVAmount { get; set; }
            
            /// <summary>
            /// FRV (Feed Rate Variation) cycle time
            /// </summary>
            public double? FRVCycleTime { get; set; }
        }

        /// <summary>
        /// Represents PWM output configuration
        /// </summary>
        public class PWMConfiguration
        {
            /// <summary>
            /// Output number for PWM signal - Required
            /// </summary>
            public int OutputNumber { get; set; }
            
            /// <summary>
            /// PWM frequency in Hz
            /// </summary>
            public int? Frequency { get; set; }
            
            /// <summary>
            /// PWM floor value (minimum duty cycle)
            /// </summary>
            public double? Floor { get; set; }
            
            /// <summary>
            /// Velocity scaling factor
            /// </summary>
            public double? VelocityScaling { get; set; }
            
            /// <summary>
            /// Whether PWM signal is inverted
            /// </summary>
            public bool? IsInverted { get; set; }
            
            /// <summary>
            /// Inverse enable bit (parameter 815, bit 0)
            /// </summary>
            public bool? InverseEnabled { get; set; }
            
            /// <summary>
            /// Velocity 100% mode (parameter 815, bit 1) - true = 0-100%, false = 0-10%
            /// </summary>
            public bool? Velocity100 { get; set; }
        }

        /// <summary>
        /// Represents probe configuration
        /// </summary>
        public class ProbeConfiguration
        {
            /// <summary>
            /// Probe input number
            /// </summary>
            public int? InputNumber { get; set; }
            
            /// <summary>
            /// Probe input type (0=Normally Open, 1=Normally Closed)
            /// </summary>
            public int? InputType { get; set; }
            
            /// <summary>
            /// Probe feed rate
            /// </summary>
            public double? FeedRate { get; set; }
            
            /// <summary>
            /// Touch plate thickness
            /// </summary>
            public double? TouchPlateThickness { get; set; }
            
            /// <summary>
            /// Touch plate input number (if different from probe)
            /// </summary>
            public int? TouchPlateInputNumber { get; set; }
            
            /// <summary>
            /// Touch plate input type
            /// </summary>
            public int? TouchPlateInputType { get; set; }
            
            /// <summary>
            /// Probe type configuration
            /// </summary>
            public int? ProbeType { get; set; }
            
            /// <summary>
            /// Display probe warning
            /// </summary>
            public bool? DisplayProbeWarning { get; set; }
            
            /// <summary>
            /// Probe protection/inhibit settings
            /// </summary>
            public int? ProbeInhibit { get; set; }
        }

        /// <summary>
        /// Represents touch plate configuration system
        /// </summary>
        public class TouchPlateConfiguration
        {
            /// <summary>
            /// Touch plate input number
            /// </summary>
            public int? InputNumber { get; set; }
            
            /// <summary>
            /// Touch plate detection input
            /// </summary>
            public int? DetectInput { get; set; }
            
            /// <summary>
            /// Touch plate input type (0=Normally Open, 1=Normally Closed)
            /// </summary>
            public int? InputType { get; set; }
            
            /// <summary>
            /// Wall height dimension
            /// </summary>
            public double? WallHeight { get; set; }
            
            /// <summary>
            /// Wall thickness dimension
            /// </summary>
            public double? WallThickness { get; set; }
            
            /// <summary>
            /// Internal diameter
            /// </summary>
            public double? InternalDiameter { get; set; }
            
            /// <summary>
            /// Maximum search distance
            /// </summary>
            public double? MaxDistance { get; set; }
            
            /// <summary>
            /// Retract distance after touch
            /// </summary>
            public double? RetractDistance { get; set; }
            
            /// <summary>
            /// Fast probing rate
            /// </summary>
            public double? FastRate { get; set; }
            
            /// <summary>
            /// Slow probing rate
            /// </summary>
            public double? SlowRate { get; set; }
            
            /// <summary>
            /// Inside touch mode enabled
            /// </summary>
            public bool? InsideTouch { get; set; }
            
            /// <summary>
            /// Bore operations enabled
            /// </summary>
            public bool? BoreEnabled { get; set; }
            
            /// <summary>
            /// Surface plate mode
            /// </summary>
            public bool? SurfacePlate { get; set; }
        }

        /// <summary>
        /// Represents second spindle configuration
        /// </summary>
        public class SecondSpindleConfiguration
        {
            /// <summary>
            /// Enable second spindle
            /// </summary>
            public bool? Enabled { get; set; }
            
            /// <summary>
            /// Second spindle maximum speed
            /// </summary>
            public int? MaxSpeed { get; set; }
            
            /// <summary>
            /// Second spindle minimum speed
            /// </summary>
            public int? MinSpeed { get; set; }
            
            /// <summary>
            /// Second spindle encoder counts per revolution
            /// </summary>
            public int? EncoderCounts { get; set; }
        }

        /// <summary>
        /// Represents global system configuration settings
        /// </summary>
        public class GlobalSystemConfiguration
        {
            /// <summary>
            /// Global step frequency for all axes (steps per second)
            /// Supported values: 100000, 200000, 240000, 300000, 400000
            /// </summary>
            public int? StepFrequency { get; set; }
            
            /// <summary>
            /// Global drive fault delay for all axes (milliseconds)
            /// </summary>
            public int? DriveFaultDelay { get; set; }
            
            /// <summary>
            /// Global axis signal inversion settings
            /// </summary>
            public int? AxisSignalInversion { get; set; }
            
            /// <summary>
            /// Low resolution mode for plasma systems
            /// </summary>
            public bool? LowResolutionMode { get; set; }
        }

        /// <summary>
        /// Represents system hardware detection and capabilities
        /// </summary>
        public class SystemHardwareInfo
        {
            /// <summary>
            /// System type (Acorn, AcornSix, Hickory)
            /// </summary>
            public string? SystemType { get; set; }
            
            /// <summary>
            /// Number of base I/O points
            /// </summary>
            public int BaseInputs { get; set; }
            
            /// <summary>
            /// Number of base I/O points
            /// </summary>
            public int BaseOutputs { get; set; }
            
            /// <summary>
            /// Number of expansion boards detected
            /// </summary>
            public int ExpansionBoards { get; set; }
            
            /// <summary>
            /// Total available inputs
            /// </summary>
            public int TotalInputs { get; set; }
            
            /// <summary>
            /// Total available outputs
            /// </summary>
            public int TotalOutputs { get; set; }
            
            /// <summary>
            /// Available input numbers
            /// </summary>
            public List<int> AvailableInputs { get; set; } = new List<int>();
            
            /// <summary>
            /// Available output numbers
            /// </summary>
            public List<int> AvailableOutputs { get; set; } = new List<int>();
        }

        /// <summary>
        /// Represents ATC (Automatic Tool Changer) configuration
        /// </summary>
        public class ATCConfiguration
        {
            /// <summary>
            /// ATC type
            /// </summary>
            public ATCType Type { get; set; } = ATCType.None;
            
            /// <summary>
            /// Maximum number of tool positions
            /// </summary>
            public int MaxBins { get; set; }
            
            /// <summary>
            /// Tool change position X coordinate
            /// </summary>
            public double ChangePositionX { get; set; }
            
            /// <summary>
            /// Tool change position Y coordinate
            /// </summary>
            public double ChangePositionY { get; set; }
            
            /// <summary>
            /// Tool change position Z coordinate
            /// </summary>
            public double ChangePositionZ { get; set; }
            
            /// <summary>
            /// Time delay to start (turret systems)
            /// </summary>
            public double TimeDelayToStart { get; set; }
            
            /// <summary>
            /// Time to reverse (turret systems)
            /// </summary>
            public double TimeToReverse { get; set; }
            
            /// <summary>
            /// Time to fault (turret systems)
            /// </summary>
            public double TimeToFault { get; set; }
            
            /// <summary>
            /// Time per tool position (time-based turret)
            /// </summary>
            public double TimePerToolPosition { get; set; }
            
            /// <summary>
            /// Travel past distance (axis-driven turret)
            /// </summary>
            public double TravelPastDistance { get; set; }
            
            /// <summary>
            /// Travel behind distance (axis-driven turret)
            /// </summary>
            public double TravelBehindDistance { get; set; }
            
            /// <summary>
            /// Skip first count on reversal (carousel)
            /// </summary>
            public bool SkipFirstCountOnReversal { get; set; }
            
            /// <summary>
            /// Holding configuration for rack mount (0=Hole, 1=Fork)
            /// </summary>
            public int HoldingConfiguration { get; set; }
            
            /// <summary>
            /// Tool length measurement method (0=Fixed position, 1=Surface plate)
            /// </summary>
            public int ToolLengthMethod { get; set; }
        }

        /// <summary>
        /// ATC types supported by the system
        /// </summary>
        public enum ATCType
        {
            /// <summary>No automatic tool changer</summary>
            None = 0,
            /// <summary>Rotating carousel ATC</summary>
            Carousel = 1,
            /// <summary>Lathe counter-rotating turret</summary>
            CounterTurret = 2,
            /// <summary>Gray code position sensing (type 1)</summary>
            GreyCode1 = 3,
            /// <summary>Gray code position sensing (type 2)</summary>
            GreyCode2 = 4,
            /// <summary>Time-based turret positioning</summary>
            TimeTurret = 5,
            /// <summary>Servo axis driven turret</summary>
            AxisDrivenTurret = 6,
            /// <summary>Fixed position rack system</summary>
            RackMount = 7,
            /// <summary>Electric motor driven turret</summary>
            ElectricTurret = 8
        }

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
                var cncPipe = new CNCPipe();
                
                // Wait for CNCPipe to be constructed
                while (!cncPipe.IsConstructed())
                {
                    System.Threading.Thread.Sleep(10);
                }

                // Configure axis using CNCPipe.Axis methods
                CNCUtils.Initialize(cncPipe);
                
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
                
                if (config.AccelerationTime.HasValue)
                    cncPipe.axis.SetAccelTime(axisEnum, config.AccelerationTime.Value);
                
                if (!string.IsNullOrEmpty(config.Label))
                    cncPipe.axis.SetLabel(axisEnum, config.Label[0]); // SetLabel expects a char
                
                if (config.IsReversed.HasValue)
                    cncPipe.axis.SetAxisReversal(axisEnum, config.IsReversed.Value);

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
                var cncPipe = new CNCPipe();
                
                // Wait for CNCPipe to be constructed
                while (!cncPipe.IsConstructed())
                {
                    System.Threading.Thread.Sleep(10);
                }

                // Core parameters via CNCUtils.SetParameterValue() - only set if provided
                if (config.EncoderCounts.HasValue)
                    CNCUtils.SetParameterValue(CentroidParameters.SPINDLE_COUNTS_REV_PARM, config.EncoderCounts.Value);
                
                if (config.SpindleAxis.HasValue)
                    CNCUtils.SetParameterValue(CentroidParameters.SPINDLE_AXIS_PARM, config.SpindleAxis.Value);
                
                if (config.RigidTappingEnabled.HasValue)
                    CNCUtils.SetParameterValue(CentroidParameters.RIGID_TAPPING_PARM, config.RigidTappingEnabled.Value ? 1 : 0);
                
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

                // Configure spindle parameter 78 bit field only if encoder settings are provided
                if (config.EncoderEnabled.HasValue || config.SecondSpindleEnabled.HasValue)
                {
                    int spindleControl = 0;
                    if (config.EncoderEnabled == true) spindleControl |= 1;        // Bit 0: Primary Encoder Enable
                    if (config.SecondSpindleEnabled == true) spindleControl |= 8;   // Bit 3: Second Spindle Encoder
                    CNCUtils.SetParameterValue(CentroidParameters.SPINDLE_PARM, spindleControl);
                }

                // Configure deceleration time
                if (config.DecelTime.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.SPINDLE_DECEL_TIME_PARM, config.DecelTime.Value);
                }

                // Configure rigid tapping parameters
                if (config.RigidTappingSlowSpeed.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.RT_SLOW_SPINDLE_SPEED_PARM, config.RigidTappingSlowSpeed.Value);
                }

                if (config.RigidTappingSlowTime.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.RT_SLOW_SPINDLE_TIME_PARM, config.RigidTappingSlowTime.Value);
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

                // Configure PWM Options parameter bit field only if options are provided
                if (config.InverseEnabled.HasValue || config.Velocity100.HasValue || config.Floor.HasValue)
                {
                    int pwmOptions = (int)CNCUtils.GetPWMOptions(config.OutputNumber);
                    
                    if (config.InverseEnabled.HasValue)
                        pwmOptions = CNCUtils.ModifyBit(pwmOptions, 0, config.InverseEnabled.Value);
                    
                    if (config.Velocity100.HasValue)
                        pwmOptions = CNCUtils.ModifyBit(pwmOptions, 1, config.Velocity100.Value);
                    
                    if (config.Floor.HasValue)
                        pwmOptions = CNCUtils.ModifyBit(pwmOptions, 2, config.Floor.Value > 0);
                    
                    CNCUtils.SetPWMOptions(config.OutputNumber, pwmOptions);
                    parametersSet = true;
                }

                if (parametersSet)
                {
                    System.Diagnostics.Debug.WriteLine($"Configuring PWM Output {config.OutputNumber}: {config.Frequency?.ToString() ?? "not set"}Hz, Floor: {config.Floor?.ToString() ?? "not set"}%");
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
                
                // Set probe parameters using CNCUtils - only if provided
                if (config.InputNumber.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.PROBE_INPUT_PARM, config.InputNumber.Value);
                    parametersSet = true;
                }
                
                if (config.InputType.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.PROBE_INPUT_TYPE, config.InputType.Value);
                    parametersSet = true;
                }
                
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
                
                // Enhanced probe configuration parameters
                if (config.ProbeType.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.PROBE_TYPE, config.ProbeType.Value);
                    parametersSet = true;
                }
                
                if (config.DisplayProbeWarning.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.DISPLAY_PROBE_WARNING_PARAM, config.DisplayProbeWarning.Value ? 1 : 0);
                    parametersSet = true;
                }
                
                if (config.ProbeInhibit.HasValue)
                {
                    CNCUtils.SetParameterValue(CentroidParameters.PROBE_INHIBIT_PARM, config.ProbeInhibit.Value);
                    parametersSet = true;
                }

                if (parametersSet)
                {
                    System.Diagnostics.Debug.WriteLine($"Configuring Probe: Input {config.InputNumber?.ToString() ?? "not set"}, Type: {config.InputType?.ToString() ?? "not set"}");
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
                    double parameterValue = PulseStepFrequency / (double)config.StepFrequency.Value;
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

        #region System Hardware Detection Methods

        /// <summary>
        /// Detects system hardware capabilities and I/O configuration
        /// </summary>
        /// <returns>System hardware information</returns>
        public static SystemHardwareInfo DetectSystemHardware()
        {
            try
            {
                var cncPipe = new CNCPipe();
                
                // Wait for CNCPipe to be constructed
                while (!cncPipe.IsConstructed())
                {
                    System.Threading.Thread.Sleep(10);
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
            return ConfigureCompleteMachine(inputs, outputs, axes, spindle, probe, pwmOutputs, atc, null, null, null);
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
            GlobalSystemConfiguration? globalSystem = null)
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

                var cncPipe = new CNCPipe();
                
                // Wait for CNCPipe to be constructed
                while (!cncPipe.IsConstructed())
                {
                    System.Threading.Thread.Sleep(10);
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
                var cncPipe = new CNCPipe();
                
                // Wait for CNCPipe to be constructed
                while (!cncPipe.IsConstructed())
                {
                    System.Threading.Thread.Sleep(10);
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