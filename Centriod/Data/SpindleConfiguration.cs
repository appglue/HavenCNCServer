namespace HavenCNCServer.Centriod.Data
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
            /// Enable spindle speed scaling (Parameter 78 bit 4)
            /// </summary>
            public bool? SpindleScalingEnabled { get; set; }
            
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
            /// Minimum Rigid Tapping RPM (Parameter 68)
            /// </summary>
            public int? MinimumRigidTappingRPM { get; set; }
            
            /// <summary>
            /// Duration For Min. Rigid Tapping RPM in seconds (Parameter 69)
            /// </summary>
            public double? DurationForMinRigidTappingRPM { get; set; }
            
            /// <summary>
            /// Spindle Drift in degrees (Parameter 82)
            /// </summary>
            public int? SpindleDrift { get; set; }
            
            /// <summary>
            /// Spindle Accel/Decel Time
            /// </summary>
            public int? SpindleAccelDecelTime { get; set; }
            
            /// <summary>
            /// M Func To Run At Bottom Of Hole G84 Tapping
            /// </summary>
            public string? MFuncBottomHoleG84 { get; set; }
            
            /// <summary>
            /// M Func To Run At Top Of Hole For G74 Counter Tapping
            /// </summary>
            public string? MFuncTopHoleG74Counter { get; set; }
            
            /// <summary>
            /// M Func To Run At Bottom Of Hole G74 Tapping (Left Hand Taps)
            /// </summary>
            public string? MFuncBottomHoleG74LeftHand { get; set; }
            
            /// <summary>
            /// M Func To Run At Top Of Hole For G84 Counter Tapping
            /// </summary>
            public string? MFuncTopHoleG84Counter { get; set; }
            
            /// <summary>
            /// Rigid Tapping Z Axis Sync Distance (Parameter 241)
            /// </summary>
            public int? RigidTappingZAxisSyncDistance { get; set; }
            
            /// <summary>
            /// Allow Spindle Override (Parameter 36 bit 2)
            /// </summary>
            public bool? AllowSpindleOverride { get; set; }
            
            /// <summary>
            /// Do Not Wait For Index Pulse (Parameter 36 bit 1)
            /// </summary>
            public bool? DoNotWaitForIndexPulse { get; set; }
            
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