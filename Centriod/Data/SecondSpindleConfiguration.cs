namespace HavenCNCServer.Centriod.Data
{
    /// <summary>
    /// Represents second spindle configuration
    /// </summary>
    public class SecondSpindleConfiguration
    {
            /// <summary>
            /// Enable second spindle
            /// </summary>
            public bool? Enabled { get; set; }
            
            /// <summary>
            /// Second spindle maximum speed
            /// </summary>
            public int? MaxSpeed { get; set; }
            
            /// <summary>
            /// Second spindle minimum speed
            /// </summary>
            public int? MinSpeed { get; set; }
            
            /// <summary>
            /// Second spindle encoder counts per revolution
            /// </summary>
        public int? EncoderCounts { get; set; }
    }
}