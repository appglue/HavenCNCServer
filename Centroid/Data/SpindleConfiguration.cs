namespace HavenCNCServer.Centroid.Data
{
    /// <summary>
    /// Represents spindle configuration parameters
    /// </summary>
    public class SpindleConfiguration
    {
        /// <summary>
        /// Maximum spindle speed in high range
        /// </summary>
        public int? MaxSpeed { get; set; }

        /// <summary>
        /// Minimum spindle speed in high range
        /// </summary>
        public int? MinSpeed { get; set; }

        /// <summary>
        /// Spindle OK delay in seconds
        /// </summary>
        public double? OkDelay { get; set; }

        // ── Fields below are not currently used in setup ──────────────────────
        // public int? EncoderCounts { get; set; }
        // public int? SpindleAxis { get; set; }
        // public double? LowGearRatio { get; set; }
        // public double? MediumGearRatio { get; set; }
        // public double? HighGearRatio { get; set; }
        // public int? AnalogRange { get; set; }
        // public double? FanDelay { get; set; }
        // public bool? EncoderEnabled { get; set; }
        // public bool? RigidTappingEnabled { get; set; }
        // public bool? RtgDisplay { get; set; }
        // public bool? SpindleScalingEnabled { get; set; }
        // public bool? SecondSpindleEnabled { get; set; }
        // public double? DecelTime { get; set; }
        // public double? RigidTappingSlowSpeed { get; set; }
        // public double? RigidTappingSlowTime { get; set; }
        // public double? ThreadingTappingAccelDecelDistance { get; set; }
        // public int? MinimumRigidTappingRPM { get; set; }
        // public double? DurationForMinRigidTappingRPM { get; set; }
        // public int? SpindleDrift { get; set; }
        // public int? SpindleAccelDecelTime { get; set; }
        // public string? MFuncBottomHoleG84 { get; set; }
        // public string? MFuncTopHoleG74Counter { get; set; }
        // public string? MFuncBottomHoleG74LeftHand { get; set; }
        // public string? MFuncTopHoleG84Counter { get; set; }
        // public int? RigidTappingZAxisSyncDistance { get; set; }
        // public bool? AllowSpindleOverride { get; set; }
        // public bool? DoNotWaitForIndexPulse { get; set; }
        // public double? SsvCycleTime { get; set; }
        // public double? SsvAmount { get; set; }
        // public double? FrvCycleTime { get; set; }
    }
}