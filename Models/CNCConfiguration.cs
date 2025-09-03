using System.Collections.Generic;

namespace HavenCNCServer.Models
{
    /// <summary>
    /// Configuration for a Centroid input
    /// </summary>
    public class CentroidInput
    {
        /// <summary>
        /// Input number/identifier
        /// </summary>
        public int InputNumber { get; set; } = 0;

        /// <summary>
        /// Human-readable name for the input
        /// </summary>
        public string InputName { get; set; } = string.Empty;

        /// <summary>
        /// Whether the input is normally open (true) or normally closed (false)
        /// </summary>
        public bool NormallyOpen { get; set; } = true;
    }

    /// <summary>
    /// Configuration for a Centroid output
    /// </summary>
    public class CentroidOutput
    {
        /// <summary>
        /// Output number/identifier
        /// </summary>
        public int OutputNumber { get; set; } = 0;

        /// <summary>
        /// Human-readable name for the output
        /// </summary>
        public string OutputName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Configuration for a machine axis
    /// </summary>
    public class AxisConfig
    {
        /// <summary>
        /// Name of the axis (e.g., "X", "Y", "Z", "A")
        /// </summary>
        public string AxisName { get; set; } = string.Empty;

        /// <summary>
        /// Numeric identifier for the axis
        /// </summary>
        public int AxisNumber { get; set; } = 0;

        /// <summary>
        /// Number of pulses per revolution for the stepper motor
        /// </summary>
        public int PulsesPerRevolution { get; set; } = 1000;

        /// <summary>
        /// Number of revolutions per inch of travel
        /// </summary>
        public double RevolutionsPerInch { get; set; } = 5.0;

        /// <summary>
        /// Maximum speed in positive direction
        /// </summary>
        public double MaxSpeedPositive { get; set; } = 100.0;

        /// <summary>
        /// Maximum speed in negative direction
        /// </summary>
        public double MaxSpeedNegative { get; set; } = 100.0;

        /// <summary>
        /// Fast speed setting
        /// </summary>
        public double FastSpeed { get; set; } = 50.0;

        /// <summary>
        /// Normal speed setting
        /// </summary>
        public double Speed { get; set; } = 25.0;

        /// <summary>
        /// Homing speed
        /// </summary>
        public double HomingSpeed { get; set; } = 10.0;

        /// <summary>
        /// Whether homing direction is positive (true) or negative (false)
        /// </summary>
        public bool HomingDirectionPositive { get; set; } = true;

        /// <summary>
        /// Axis number this axis is paired with (-1 if not paired)
        /// </summary>
        public int PairedWithAxis { get; set; } = -1;
    }

    /// <summary>
    /// Complete Centroid machine settings configuration
    /// </summary>
    public class CentroidSettings
    {
        /// <summary>
        /// Minimum travel distance for X axis
        /// </summary>
        public double XMinTravel { get; set; } = -12.0;

        /// <summary>
        /// Maximum travel distance for X axis
        /// </summary>
        public double XMaxTravel { get; set; } = 12.0;

        /// <summary>
        /// Minimum travel distance for Y axis
        /// </summary>
        public double YMinTravel { get; set; } = -8.0;

        /// <summary>
        /// Maximum travel distance for Y axis
        /// </summary>
        public double YMaxTravel { get; set; } = 8.0;

        /// <summary>
        /// Minimum travel distance for Z axis
        /// </summary>
        public double ZMinTravel { get; set; } = -4.0;

        /// <summary>
        /// Maximum travel distance for Z axis
        /// </summary>
        public double ZMaxTravel { get; set; } = 4.0;

        /// <summary>
        /// Configuration for all machine axes
        /// </summary>
        public List<AxisConfig> Axes { get; set; } = new List<AxisConfig>();

        /// <summary>
        /// Configuration for all machine inputs
        /// </summary>
        public List<CentroidInput> Input { get; set; } = new List<CentroidInput>();

        /// <summary>
        /// Configuration for all machine outputs
        /// </summary>
        public List<CentroidOutput> Output { get; set; } = new List<CentroidOutput>();
    }
}
