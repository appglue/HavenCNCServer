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
            /// PWM S command range: true = 0-1000, false = 0-100 (parameter 815, bit 1)
            /// </summary>
            public bool? SCommandRange1000 { get; set; }
            
            /// <summary>
            /// PWM floor value (minimum duty cycle percentage)
            /// </summary>
            public double? Floor { get; set; }
            
            /// <summary>
            /// Only apply floor during PWM velocity modulation moves (parameter 815, bit 2)
            /// </summary>
            public bool? OnlyApplyFloorDuringVelocityMoves { get; set; }
            
            /// <summary>
            /// Inverse PWM output (parameter 815, bit 0)
            /// </summary>
            public bool? InverseOutput { get; set; }
            
            /// <summary>
            /// Laser cooling fan delay timer in seconds (parameter 998)
            /// </summary>
            public double? LaserCoolingFanDelayTimer { get; set; }
            
            /// <summary>
            /// Velocity scaling factor
            /// </summary>
            public double? VelocityScaling { get; set; }
            
            /// <summary>
            /// Whether PWM signal is inverted (legacy property - use InverseOutput instead)
            /// </summary>
            public bool? IsInverted { get; set; }
            
            /// <summary>
            /// Inverse enable bit (legacy property - use InverseOutput instead)
            /// </summary>
            public bool? InverseEnabled { get; set; }
            
            /// <summary>
            /// Velocity 100% mode (legacy property - use SCommandRange1000 instead)
            /// </summary>
            public bool? Velocity100 { get; set; }
        }
    }
}
