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
        }

    }
}
