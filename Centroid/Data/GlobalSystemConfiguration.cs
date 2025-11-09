namespace HavenCNCServer.Centroid.Data
{
    /// <summary>
    /// Tool height offset measurement method
    /// </summary>
    public enum ToolHeightMeasurementMethod
    {
        /// <summary>
        /// Tool heights measured from machine Z home position
        /// </summary>
        ZHomeEqualsZRef = 0,

        /// <summary>
        /// Tool heights measured against a standard reference tool
        /// </summary>
        ReferenceTool = 1
    }

    /// <summary>
    /// Represents global system configuration settings
    /// </summary>
    public class GlobalSystemConfiguration
    {
        /// <summary>
        /// Maximum number of steps that can be pulsed per second
        /// Global step frequency setting for all axes
        /// </summary>
        public StepFrequency? StepFrequency { get; set; }

        /// <summary>
        /// Axis Motor Drive fault delay time (milliseconds)
        /// Global drive fault delay for all axes
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

        /// <summary>
        /// Divider for charge pump frequency. 0 = turned off, 96 = default (12.5kHz)
        /// Formula: 1,200,000 / divider = frequency in Hz
        /// </summary>
        public int? ChargePumpDivider { get; set; }

        /// <summary>
        /// Tool height offset measurement method
        /// </summary>
        public ToolHeightMeasurementMethod? ToolHeightMeasurementMethod { get; set; }
    }
}