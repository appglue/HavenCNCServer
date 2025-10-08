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

    }
}
