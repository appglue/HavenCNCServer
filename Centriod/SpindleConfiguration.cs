namespace HavenCNCServer.Models
{
public static partial class CentroidConfigUtil
    {
        /// <summary>
        /// Represents spindle configuration parameters
        /// </summary>
        public class SpindleConfiguration
        {
            /// <summary>
            /// Encoder counts per spindle revolution
            /// </summary>
            public int? EncoderCounts { get; set; }
            
            /// <summary>
            /// Spindle axis number (5 standard, 8 for AcornSix/Hickory)
            /// </summary>
            public int? SpindleAxis { get; set; }
            
            /// <summary>
            /// Low gear ratio
            /// </summary>
            public double? LowGearRatio { get; set; }
            
            /// <summary>
            /// Medium gear ratio
            /// </summary>
            public double? MediumGearRatio { get; set; }
            
            /// <summary>
            /// High gear ratio
            /// </summary>
            public double? HighGearRatio { get; set; }
            
            /// <summary>
            /// Maximum spindle speed
            /// </summary>
            public int? MaxSpeed { get; set; }
            
            /// <summary>
            /// Minimum spindle speed
            /// </summary>
            public int? MinSpeed { get; set; }
            
            /// <summary>
            /// Analog output voltage range (0-3)
            /// </summary>
            public int? AnalogRange { get; set; }
            
            /// <summary>
            /// Spindle OK delay in seconds
            /// </summary>
            public double? OkDelay { get; set; }
            
            /// <summary>
            /// Cooling fan delay in seconds
            /// </summary>
            public double? FanDelay { get; set; }
            
            /// <summary>
            /// Enable spindle encoder
            /// </summary>
            public bool? EncoderEnabled { get; set; }
            
            /// <summary>
            /// Enable rigid tapping
            /// </summary>
            public bool? RigidTappingEnabled { get; set; }
            
            /// <summary>
            /// Enable RTG (Real Time Graphics) display
            /// </summary>
            public bool? RTGDisplay { get; set; }
            
            /// <summary>
            /// Enable second spindle
            /// </summary>
            public bool? SecondSpindleEnabled { get; set; }
            
            /// <summary>
            /// Spindle deceleration time in seconds
            /// </summary>
            public double? DecelTime { get; set; }
            
            /// <summary>
            /// Rigid tapping slow spindle speed
            /// </summary>
            public double? RigidTappingSlowSpeed { get; set; }
            
            /// <summary>
            /// Rigid tapping slow spindle time
            /// </summary>
            public double? RigidTappingSlowTime { get; set; }
            
            /// <summary>
            /// Threading and tapping acceleration/deceleration distance
            /// </summary>
            public double? ThreadingTappingAccelDecelDistance { get; set; }
            
            /// <summary>
            /// SSV (Spindle Speed Variation) cycle time
            /// </summary>
            public double? SSVCycleTime { get; set; }
            
            /// <summary>
            /// SSV (Spindle Speed Variation) amount percentage
            /// </summary>
            public double? SSVAmount { get; set; }
            
            /// <summary>
            /// FRV (Feed Rate Variation) cycle time
            /// </summary>
            public double? FRVCycleTime { get; set; }
        }

    }
}
