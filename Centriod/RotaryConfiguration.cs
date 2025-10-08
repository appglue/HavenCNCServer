using HavenCNCServer.CentriodAPI;

namespace HavenCNCServer.Models
{
    public static partial class CentroidConfigUtil
    {
        /// <summary>
        /// Represents global rotary axis configuration settings
        /// </summary>
        public class RotaryConfiguration
        {
            /// <summary>
            /// Rotary axis jog increment in degrees
            /// Parameter 41 - Sets the jog increment for rotary axes
            /// </summary>
            public double? JogIncrement { get; set; }
            
            /// <summary>
            /// Rotary DRO display type
            /// Per-axis setting via axis properties parameters (bit 1)
            /// 0 = Show Rotations, 1 = Wrap Around (0-360°)
            /// Note: This is configured per-axis, not globally
            /// </summary>
            public bool? RotaryDROWrapAround { get; set; }
            
            /// <summary>
            /// Slave rotary axis feedrate to a linear move feedrate on the same line
            /// Parameter 2 (CNC_COMPATIBILITY_PARM) bit 3
            /// </summary>
            public bool? SlaveRotaryFeedrateToLinear { get; set; }
            
            /// <summary>
            /// Rotary-only moves won't use a modal feedrate set by a prior rotary and non-rotary move
            /// Parameter 2 (CNC_COMPATIBILITY_PARM) bit 5
            /// Note: This feature has no effect for movement commands handled by G-code Smoothing (P220=1)
            /// </summary>
            public bool? PreventRotaryModalFeedrate { get; set; }
        }
    }
}