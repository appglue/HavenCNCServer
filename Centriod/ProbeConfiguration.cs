namespace HavenCNCServer.Models
{
public static partial class CentroidConfigUtil
    {
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

    }
}
