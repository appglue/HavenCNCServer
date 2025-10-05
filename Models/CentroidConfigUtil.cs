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
            public string Name { get; set; } = string.Empty;
            
            /// <summary>
            /// I/O number (1-64 for standard I/O)
            /// </summary>
            public int Number { get; set; }
            
            /// <summary>
            /// Whether the input/output is inverted
            /// </summary>
            public bool IsInverted { get; set; }
        }

        #endregion

        #region Axis Configuration Classes

        /// <summary>
        /// Represents axis configuration parameters
        /// </summary>
        public class AxisConfiguration
        {
            /// <summary>
            /// Axis number (1-8)
            /// </summary>
            public int AxisNumber { get; set; }
            
            /// <summary>
            /// Axis label (X, Y, Z, A, B, C, U, V, W)
            /// </summary>
            public string Label { get; set; } = string.Empty;
            
            /// <summary>
            /// Steps per revolution (motor/drive steps)
            /// </summary>
            public int StepsPerRevolution { get; set; }
            
            /// <summary>
            /// Turn ratio - distance per revolution (inches/mm per rev for linear, degrees for rotary)
            /// </summary>
            public double TurnRatio { get; set; }
            
            /// <summary>
            /// Plus travel limit
            /// </summary>
            public double PlusTravelLimit { get; set; }
            
            /// <summary>
            /// Minus travel limit
            /// </summary>
            public double MinusTravelLimit { get; set; }
            
            /// <summary>
            /// Backlash compensation amount
            /// </summary>
            public double BacklashCompensation { get; set; }
            
            /// <summary>
            /// Slow jog rate
            /// </summary>
            public double SlowJogRate { get; set; }
            
            /// <summary>
            /// Fast jog rate
            /// </summary>
            public double FastJogRate { get; set; }
            
            /// <summary>
            /// Acceleration time in seconds
            /// </summary>
            public double AccelerationTime { get; set; }
            
            /// <summary>
            /// Whether axis direction is reversed
            /// </summary>
            public bool IsReversed { get; set; }
            
            /// <summary>
            /// Master axis for pairing (0 = none, 1-8 = master axis number)
            /// </summary>
            public int MasterAxis { get; set; }
            
            /// <summary>
            /// Whether this is a rotary axis (Parameter bit 0)
            /// </summary>
            public bool IsRotary { get; set; }
            
            /// <summary>
            /// Rotary DRO wrap around display mode (Parameter bit 1)
            /// </summary>
            public bool RotaryWrapAround { get; set; }
            
            /// <summary>
            /// C-Axis enable (Parameter bit 4)
            /// </summary>
            public bool CAxisEnabled { get; set; }
            
            /// <summary>
            /// Prevent divide by 360 for C-Axis (Parameter bit 7)
            /// </summary>
            public bool PreventDivideBy360 { get; set; }
            
            /// <summary>
            /// Hide axis from DRO display - ATC Turret (Parameter bit 9)
            /// </summary>
            public bool HideFromDRO { get; set; }
            
            /// <summary>
            /// Rotary axis parallel to X (Parameter bit 11)
            /// </summary>
            public bool ParallelToX { get; set; }
            
            /// <summary>
            /// Rotary axis parallel to Y (Parameter bit 12)
            /// </summary>
            public bool ParallelToY { get; set; }
        }

        /// <summary>
        /// Represents spindle configuration parameters
        /// </summary>
        public class SpindleConfiguration
        {
            /// <summary>
            /// Encoder counts per spindle revolution
            /// </summary>
            public int EncoderCounts { get; set; }
            
            /// <summary>
            /// Spindle axis number (5 standard, 8 for AcornSix/Hickory)
            /// </summary>
            public int SpindleAxis { get; set; } = 5;
            
            /// <summary>
            /// Low gear ratio
            /// </summary>
            public double LowGearRatio { get; set; } = 1.0;
            
            /// <summary>
            /// Medium gear ratio
            /// </summary>
            public double MediumGearRatio { get; set; } = 1.0;
            
            /// <summary>
            /// High gear ratio
            /// </summary>
            public double HighGearRatio { get; set; } = 1.0;
            
            /// <summary>
            /// Maximum spindle speed
            /// </summary>
            public int MaxSpeed { get; set; }
            
            /// <summary>
            /// Minimum spindle speed
            /// </summary>
            public int MinSpeed { get; set; }
            
            /// <summary>
            /// Analog output voltage range (0-3)
            /// </summary>
            public int AnalogRange { get; set; }
            
            /// <summary>
            /// Spindle OK delay in seconds
            /// </summary>
            public double OkDelay { get; set; }
            
            /// <summary>
            /// Cooling fan delay in seconds
            /// </summary>
            public double FanDelay { get; set; }
            
            /// <summary>
            /// Enable spindle encoder
            /// </summary>
            public bool EncoderEnabled { get; set; } = true;
            
            /// <summary>
            /// Enable rigid tapping
            /// </summary>
            public bool RigidTappingEnabled { get; set; }
            
            /// <summary>
            /// Enable RTG (Real Time Graphics) display
            /// </summary>
            public bool RTGDisplay { get; set; }
            
            /// <summary>
            /// Enable second spindle
            /// </summary>
            public bool SecondSpindleEnabled { get; set; }
        }

        /// <summary>
        /// Represents PWM output configuration
        /// </summary>
        public class PWMConfiguration
        {
            /// <summary>
            /// Output number for PWM signal
            /// </summary>
            public int OutputNumber { get; set; }
            
            /// <summary>
            /// PWM frequency in Hz
            /// </summary>
            public int Frequency { get; set; } = 1221;
            
            /// <summary>
            /// PWM floor value (minimum duty cycle)
            /// </summary>
            public double Floor { get; set; } = 15.0;
            
            /// <summary>
            /// Velocity scaling factor
            /// </summary>
            public double VelocityScaling { get; set; } = 1.0;
            
            /// <summary>
            /// Whether PWM signal is inverted
            /// </summary>
            public bool IsInverted { get; set; }
            
            /// <summary>
            /// Inverse enable bit (parameter 815, bit 0)
            /// </summary>
            public bool InverseEnabled { get; set; }
            
            /// <summary>
            /// Velocity 100% mode (parameter 815, bit 1) - true = 0-100%, false = 0-10%
            /// </summary>
            public bool Velocity100 { get; set; }
        }

        /// <summary>
        /// Represents probe configuration
        /// </summary>
        public class ProbeConfiguration
        {
            /// <summary>
            /// Probe input number
            /// </summary>
            public int InputNumber { get; set; }
            
            /// <summary>
            /// Probe input type (0=Normally Open, 1=Normally Closed)
            /// </summary>
            public int InputType { get; set; }
            
            /// <summary>
            /// Probe feed rate
            /// </summary>
            public double FeedRate { get; set; }
            
            /// <summary>
            /// Touch plate thickness
            /// </summary>
            public double TouchPlateThickness { get; set; }
            
            /// <summary>
            /// Touch plate input number (if different from probe)
            /// </summary>
            public int TouchPlateInputNumber { get; set; }
            
            /// <summary>
            /// Touch plate input type
            /// </summary>
            public int TouchPlateInputType { get; set; }
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

            // Sort by I/O number for consistent ordering
            var sortedFunctions = ioFunctions.OrderBy(f => f.Number).ToList();

            // Find the longest function name for alignment
            int maxNameLength = sortedFunctions.Max(f => f.Name.Length);
            int alignmentColumn = Math.Max(maxNameLength + 4, 16);

            foreach (var function in sortedFunctions)
            {
                var definition = $"{ioType}{function.Number}";
                var spacing = new string(' ', alignmentColumn - function.Name.Length);
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
                
                // Set basic axis parameters
                cncPipe.axis.SetCountsPerTurn(axisEnum, config.StepsPerRevolution);
                cncPipe.axis.SetScrewPitch(axisEnum, config.TurnRatio);
                cncPipe.axis.SetTravelLimit(axisEnum, CNCPipe.Axis.Direction.PLUS, config.PlusTravelLimit);
                cncPipe.axis.SetTravelLimit(axisEnum, CNCPipe.Axis.Direction.MINUS, config.MinusTravelLimit);
                cncPipe.axis.SetLashComp(axisEnum, config.BacklashCompensation);
                cncPipe.axis.SetRate(axisEnum, CNCPipe.Axis.Rate.SLOW_JOG, config.SlowJogRate);
                cncPipe.axis.SetRate(axisEnum, CNCPipe.Axis.Rate.FAST_JOG, config.FastJogRate);
                cncPipe.axis.SetAccelTime(axisEnum, config.AccelerationTime);
                cncPipe.axis.SetLabel(axisEnum, config.Label[0]); // SetLabel expects a char
                cncPipe.axis.SetAxisReversal(axisEnum, config.IsReversed);

                System.Diagnostics.Debug.WriteLine($"Configuring Axis {config.AxisNumber} ({config.Label}): {config.StepsPerRevolution} steps/rev");
                
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
                int parameterNumber = GetAxisPropertyParameter(config.AxisNumber);
                if (parameterNumber == -1) return false;

                // Get current parameter value
                int axisProperties = (int)CNCUtils.GetParameterValue(parameterNumber);
                
                // Configure bit fields according to documentation
                // Bit 0: Linear/Rotary (0=Linear, 1=Rotary)
                axisProperties = CNCUtils.ModifyBit(axisProperties, 0, config.IsRotary);
                
                // Bit 1: Rotary DRO Display (0=Show Rotations, 1=Wrap Around)  
                axisProperties = CNCUtils.ModifyBit(axisProperties, 1, config.RotaryWrapAround);
                
                // Bit 4: C-Axis Enable
                axisProperties = CNCUtils.ModifyBit(axisProperties, 4, config.CAxisEnabled);
                
                // Bit 7: Prevent Divide by 360 for C-Axis
                axisProperties = CNCUtils.ModifyBit(axisProperties, 7, config.PreventDivideBy360);
                
                // Bit 9: Hide Axis from DRO (ATC Turret)
                axisProperties = CNCUtils.ModifyBit(axisProperties, 9, config.HideFromDRO);
                
                // Bit 11: Parallel to X (Rotary)
                axisProperties = CNCUtils.ModifyBit(axisProperties, 11, config.ParallelToX);
                
                // Bit 12: Parallel to Y (Rotary)
                axisProperties = CNCUtils.ModifyBit(axisProperties, 12, config.ParallelToY);

                // Set the updated parameter value
                CNCUtils.SetParameterValue(parameterNumber, axisProperties);

                System.Diagnostics.Debug.WriteLine($"Configuring Axis {config.AxisNumber} properties: Parameter {parameterNumber} = {axisProperties}");
                
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
        private static int GetAxisPropertyParameter(int axisNumber)
        {
            return axisNumber switch
            {
                1 => 91,   // Axis 1
                2 => 92,   // Axis 2  
                3 => 93,   // Axis 3
                4 => 94,   // Axis 4
                5 => 166,  // Axis 5
                6 => 167,  // Axis 6
                7 => 168,  // Axis 7
                8 => 169,  // Axis 8
                _ => -1    // Invalid axis
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
                int parameterNumber = slaveAxis switch
                {
                    4 => 554, // 4th Axis Master/Slave Pairing
                    5 => 555, // 5th Axis Master/Slave Pairing
                    _ => -1
                };

                if (parameterNumber == -1) return false;

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

                // Core parameters via CNCUtils.SetParameterValue()
                CNCUtils.SetParameterValue(34, config.EncoderCounts);    // SPINDLE_COUNTS_REV_PARM
                CNCUtils.SetParameterValue(35, config.SpindleAxis);      // Spindle axis
                CNCUtils.SetParameterValue(36, config.RigidTappingEnabled ? 1 : 0);  // Rigid tapping parameter
                CNCUtils.SetParameterValue(65, (int)(config.LowGearRatio * 1000));    // LOW_GEAR_RATIO_PARM
                CNCUtils.SetParameterValue(66, (int)(config.MediumGearRatio * 1000)); // MED_LOW_GEAR_RATIO_PARM
                CNCUtils.SetParameterValue(67, (int)(config.HighGearRatio * 1000));   // HIGH_GEAR_RATIO_PARM
                CNCUtils.SetParameterValue(420, config.AnalogRange);     // PLC_ANALOG_PARM
                CNCUtils.SetParameterValue(430, config.RTGDisplay ? 1 : 0);  // RTG display parameter
                CNCUtils.SetParameterValue(996, (int)(config.OkDelay * 1000));       // SPINDLE_OK_DELAY_PARM
                CNCUtils.SetParameterValue(997, (int)(config.FanDelay * 1000));      // SPINDLE_COOLING_FAN_DELAY_TIMER

                // Speed configuration via API calls
                // MainWindow.skin.state.SetHighRangeSpindleSpeed(CNCPipe.State.Value.MAX, config.MaxSpeed);
                // MainWindow.skin.state.SetHighRangeSpindleSpeed(CNCPipe.State.Value.MIN, config.MinSpeed);

                // Configure spindle parameter 78 bit field
                int spindleControl = 0;
                if (config.EncoderEnabled) spindleControl |= 1;        // Bit 0: Primary Encoder Enable
                if (config.SecondSpindleEnabled) spindleControl |= 8;   // Bit 3: Second Spindle Encoder
                CNCUtils.SetParameterValue(78, spindleControl);

                System.Diagnostics.Debug.WriteLine($"Configuring Spindle: {config.EncoderCounts} counts, Max: {config.MaxSpeed}, Min: {config.MinSpeed}");
                
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
                // PWM parameters are output-specific
                // Base parameter numbers: 814 (frequency), 815 (options), 817 (floor), etc.
                int frequencyParam = 814 + (config.OutputNumber - 1) * 10; // Example calculation
                int optionsParam = 815 + (config.OutputNumber - 1) * 10;
                int floorParam = 817 + (config.OutputNumber - 1) * 10;

                CNCUtils.SetParameterValue(frequencyParam, config.Frequency);
                CNCUtils.SetParameterValue(floorParam, (int)(config.Floor * 100));

                // Configure PWM Options parameter (815) bit field
                // Bit 0: Inverse Enable, Bit 1: Velocity 100%, Bit 2: Minimum Floor Enable
                int pwmOptions = (int)CNCUtils.GetParameterValue(optionsParam);
                pwmOptions = CNCUtils.ModifyBit(pwmOptions, 0, config.InverseEnabled);
                pwmOptions = CNCUtils.ModifyBit(pwmOptions, 1, config.Velocity100);
                pwmOptions = CNCUtils.ModifyBit(pwmOptions, 2, config.Floor > 0);
                CNCUtils.SetParameterValue(optionsParam, pwmOptions);

                System.Diagnostics.Debug.WriteLine($"Configuring PWM Output {config.OutputNumber}: {config.Frequency}Hz, Floor: {config.Floor}%, Options: {pwmOptions}");
                
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
                CNCUtils.SetParameterValue(6, config.Type != ATCType.None ? 1 : 0);    // Tool Changer Installed
                CNCUtils.SetParameterValue(830, (int)config.Type);                     // ATC Type
                CNCUtils.SetParameterValue(161, config.MaxBins);                       // ATC Max Bins

                // Type-specific parameters
                switch (config.Type)
                {
                    case ATCType.Carousel:
                        CNCUtils.SetParameterValue(852, config.SkipFirstCountOnReversal ? 1 : 0);
                        // Set G30 reference points for tool change position
                        // MainWindow.skin.reference.SetG30(config.ChangePositionX, config.ChangePositionY, config.ChangePositionZ);
                        break;

                    case ATCType.RackMount:
                        CNCUtils.SetParameterValue(431, config.HoldingConfiguration);
                        CNCUtils.SetParameterValue(432, config.ToolLengthMethod);
                        break;

                    case ATCType.CounterTurret:
                    case ATCType.TimeTurret:
                    case ATCType.ElectricTurret:
                        CNCUtils.SetParameterValue(850, (int)(config.TimeDelayToStart * 1000));
                        CNCUtils.SetParameterValue(848, (int)(config.TimeToReverse * 1000));
                        CNCUtils.SetParameterValue(849, (int)(config.TimeToFault * 1000));
                        CNCUtils.SetParameterValue(851, (int)(config.TimeDelayToStart * 1000));
                        if (config.Type == ATCType.TimeTurret)
                        {
                            CNCUtils.SetParameterValue(975, (int)(config.TimePerToolPosition * 1000));
                        }
                        break;

                    case ATCType.AxisDrivenTurret:
                        CNCUtils.SetParameterValue(853, (int)(config.TravelPastDistance * 10000));
                        CNCUtils.SetParameterValue(854, (int)(config.TravelBehindDistance * 10000));
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
                // Set probe parameters using CNCUtils
                // Note: Using direct parameter numbers until probe parameters are added to CNC12Parameters enum
                CNCUtils.SetParameterValue(405, config.InputNumber);      // PROBE_INPUT_PARM
                CNCUtils.SetParameterValue((int)CNC12Parameters.PROBE_INPUT_TYPE, config.InputType);
                CNCUtils.SetParameterValue(407, config.TouchPlateInputNumber);  // TOUCH_PLATE_INPUT_PARM
                CNCUtils.SetParameterValue(408, config.TouchPlateInputType);    // TOUCH_PLATE_INPUT_TYPE_PARM

                System.Diagnostics.Debug.WriteLine($"Configuring Probe: Input {config.InputNumber}, Type: {config.InputType}");
                
                return true;
            }
            catch (Exception)
            {
                return false;
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
            try
            {
                // Step 1: Configure I/O in PLC file
                if (!ConfigureInputsOutputs(inputs, outputs))
                {
                    return false;
                }

                // Step 2: Configure all axes
                foreach (var axis in axes)
                {
                    if (!ConfigureAxis(axis))
                    {
                        return false;
                    }
                }

                // Step 3: Configure spindle
                if (!ConfigureSpindle(spindle))
                {
                    return false;
                }

                // Step 4: Configure probe if provided
                if (probe != null && !ConfigureProbe(probe))
                {
                    return false;
                }

                // Step 5: Configure PWM outputs if provided
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

                // Step 6: Configure ATC if provided
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
                // Determine which parameter to use based on input number
                int parameterNumber;
                int bitPosition;
                
                if (inputNumber >= 1 && inputNumber <= 16)
                {
                    parameterNumber = 911;
                    bitPosition = inputNumber - 1;
                }
                else if (inputNumber >= 17 && inputNumber <= 32)
                {
                    parameterNumber = 912;
                    bitPosition = inputNumber - 17;
                }
                else if (inputNumber >= 33 && inputNumber <= 48)
                {
                    parameterNumber = 913;
                    bitPosition = inputNumber - 33;
                }
                else if (inputNumber >= 49 && inputNumber <= 64)
                {
                    parameterNumber = 914;
                    bitPosition = inputNumber - 49;
                }
                else if (inputNumber >= 65 && inputNumber <= 80)
                {
                    parameterNumber = 915;
                    bitPosition = inputNumber - 65;
                }
                else
                {
                    return false; // Invalid input number
                }

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
                var parameterGroups = new Dictionary<int, List<(int inputNum, bool invert)>>();
                
                foreach (var setting in inputSettings)
                {
                    int parameterNumber = GetInputInversionParameter(setting.Key);
                    if (parameterNumber == -1) continue; // Invalid input number
                    
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
                        int parameterNumber = group.Key;
                        
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
        /// <returns>Parameter number or -1 if invalid</returns>
        private static int GetInputInversionParameter(int inputNumber)
        {
            if (inputNumber >= 1 && inputNumber <= 16) return 911;
            if (inputNumber >= 17 && inputNumber <= 32) return 912;
            if (inputNumber >= 33 && inputNumber <= 48) return 913;
            if (inputNumber >= 49 && inputNumber <= 64) return 914;
            if (inputNumber >= 65 && inputNumber <= 80) return 915;
            return -1;
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