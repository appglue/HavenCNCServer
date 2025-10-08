namespace HavenCNCServer.Models
{
public static partial class CentroidConfigUtil
    {
        /// <summary>
        /// Represents PWM output configuration
        /// </summary>
        public class PWMConfiguration
        {
            /// <summary>
            /// Output number for PWM signal - Required
            /// </summary>
            public int OutputNumber { get; set; }
            
            /// <summary>
            /// PWM frequency in Hz
            /// </summary>
            public int? Frequency { get; set; }
            
            /// <summary>
            /// PWM floor value (minimum duty cycle)
            /// </summary>
            public double? Floor { get; set; }
            
            /// <summary>
            /// Velocity scaling factor
            /// </summary>
            public double? VelocityScaling { get; set; }
            
            /// <summary>
            /// Whether PWM signal is inverted
            /// </summary>
            public bool? IsInverted { get; set; }
            
            /// <summary>
            /// Inverse enable bit (parameter 815, bit 0)
            /// </summary>
            public bool? InverseEnabled { get; set; }
            
            /// <summary>
            /// Velocity 100% mode (parameter 815, bit 1) - true = 0-100%, false = 0-10%
            /// </summary>
            public bool? Velocity100 { get; set; }
        }
    }
}
