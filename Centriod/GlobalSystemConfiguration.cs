using HavenCNCServer.CentriodAPI;

namespace HavenCNCServer.Models
{
public static partial class CentroidConfigUtil
    {
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
        }

    }
}
