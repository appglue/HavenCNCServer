using System.Collections.Generic;
using HavenCNCServer.Centriod.Data;

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
        /// Configuration for all machine axes - uses the full AxisConfiguration model
        /// </summary>
        public List<AxisConfiguration> Axes { get; set; } = new List<AxisConfiguration>();

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
