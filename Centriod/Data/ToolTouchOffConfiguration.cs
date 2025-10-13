namespace HavenCNCServer.Centriod.Data
{
    /// <summary>
    /// Tool touch off type enumeration
    /// </summary>
    public enum ToolTouchOffType
    {
        /// <summary>
        /// Standard tool touch off
        /// </summary>
        Standard = 0,
        
        /// <summary>
        /// Enhanced tool touch off
        /// </summary>
        Enhanced = 1
    }

    /// <summary>
    /// Tool touch off input state when triggered
    /// </summary>
    public enum ToolTouchOffInputState
    {
        /// <summary>
        /// Normally Open - input opens when tool touches off
        /// </summary>
        Open = 0,
        
        /// <summary>
        /// Normally Closed - input closes when tool touches off
        /// </summary>
        Closed = 1
    }

    /// <summary>
    /// Fixed location mode for tool touch off
    /// </summary>
    public enum FixedLocationMode
    {
        /// <summary>
        /// Moveable tool touch off device
        /// </summary>
        Moveable = 0,
        
        /// <summary>
        /// Fixed location tool touch off device
        /// </summary>
        Fixed = 3
    }

    /// <summary>
    /// Represents tool touch off configuration
    /// </summary>
    public class ToolTouchOffConfiguration
    {
            /// <summary>
            /// Touch Off Tool PLC input number (Parameter 44 for Mill, 244 for Lathe)
            /// </summary>
            public int? TouchOffToolPLCInput { get; set; }
            
            /// <summary>
            /// Tool Touch Off type (Standard/Enhanced) (Parameter 405)
            /// </summary>
            public ToolTouchOffType? ToolTouchOffType { get; set; }
            
            /// <summary>
            /// Input state when tool touch off is triggered (Parameter 407)
            /// </summary>
            public ToolTouchOffInputState? InputStateWhenTriggered { get; set; }
            
            /// <summary>
            /// Display warning to verify that Tool Touch Off is functioning properly
            /// </summary>
            public bool? DisplayWarningToVerify { get; set; }
            
            /// <summary>
            /// Inhibit spindle when detect is on (Green) (Parameter 257)
            /// </summary>
            public bool? InhibitSpindleWhenDetectOn { get; set; }
            
            /// <summary>
            /// Probe protection enabled (Parameter 43 bit field)
            /// </summary>
            public bool? ProbeProtectionEnabled { get; set; }
            
            /// <summary>
            /// Use fixed tool touch off (TT) location (Parameter 17)
            /// </summary>
            public bool? UseFixedLocation { get; set; }
            
            /// <summary>
            /// Fixed Tool Touch Off X position (machine coordinates) - G30 P3 X
            /// </summary>
            public double? FixedLocationX { get; set; }
            
            /// <summary>
            /// Fixed Tool Touch Off Y position (machine coordinates) - G30 P3 Y
            /// </summary>
            public double? FixedLocationY { get; set; }
            
            /// <summary>
            /// Z Clearance Height for Auto moves to TT (machine coordinates) - G30 P3 Z
            /// </summary>
            public double? ZClearanceHeight { get; set; }
            
            /// <summary>
            /// Subtract height of TT when setting Tool Height Offsets (Parameter 3 bit 1)
            /// </summary>
            public bool? SubtractHeightWhenSettingOffsets { get; set; }
            
            /// <summary>
            /// TT Height - Height of touch-off device (Parameter 71)
            /// </summary>
            public double? TTHeight { get; set; }
            
            /// <summary>
            /// Tool Touch Off Detect Input number for detection/protection (Parameter 257)
            /// </summary>
            public int? DetectInputNumber { get; set; }
        }
}