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
}
