namespace HavenCNCServer.Centroid.Data
{
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
        public string? AxisType { get; set; }

        /// <summary>
        /// Steps per revolution (motor/drive steps)
        /// </summary>
        public int? StepsPerRevolution { get; set; }

        /// <summary>
        /// Turn ratio - distance per revolution (inches/mm per rev for linear, degrees for rotary)
        /// </summary>
        public double? OverallTurnsRatio { get; set; }

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
        public double? LashCompensation { get; set; }

        /// <summary>
        /// Slow jog rate
        /// </summary>
        public double? SlowJogRate { get; set; }

        /// <summary>
        /// Fast jog rate
        /// </summary>
        public double? FastJogRate { get; set; }

        /// <summary>
        /// Maximum rate for axis movement
        /// </summary>
        public double? MaxRate { get; set; }

        /// <summary>
        /// Fast jog rate in plus direction
        /// </summary>
        public double? FastJogPlusDirection { get; set; }

        /// <summary>
        /// Fast jog rate in minus direction
        /// </summary>
        public double? FastJogMinusDirection { get; set; }

        /// <summary>
        /// Acceleration time in seconds
        /// </summary>
        public double? AccelDecel { get; set; }

        /// <summary>
        /// Homing feedrate - the feedrate that the machine will run at to home each axis
        /// </summary>
        public double? HomingFeedrate { get; set; }

        /// <summary>
        /// Drive enable delay in milliseconds
        /// </summary>
        public int? DriveEnableDelay { get; set; }

        /// <summary>
        /// Whether axis direction is reversed
        /// </summary>
        public bool? DirectionReversal { get; set; }

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

        /// <summary>
        /// Invert step signal for this axis (Parameter 961 - 4-bit nibble per axis)
        /// </summary>
        public bool? StepSignalInverted { get; set; }

        /// <summary>
        /// Invert direction signal for this axis (Parameter 961 - 4-bit nibble per axis)
        /// </summary>
        public bool? DirectionSignalInverted { get; set; }

        /// <summary>
        /// Invert enable signal for this axis (Parameter 961 - 4-bit nibble per axis)
        /// </summary>
        public bool? EnableSignalInverted { get; set; }
    }
}