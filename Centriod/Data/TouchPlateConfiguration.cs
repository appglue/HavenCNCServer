namespace HavenCNCServer.Models
{
public static partial class CentroidConfigUtil
    {
        /// <summary>
        /// Represents touch plate configuration system
        /// </summary>
        public class TouchPlateConfiguration
        {
            /// <summary>
            /// Touch plate input number
            /// </summary>
            public int? InputNumber { get; set; }
            
            /// <summary>
            /// Touch plate detection input
            /// </summary>
            public int? DetectInput { get; set; }
            
            /// <summary>
            /// Touch plate input type (0=Normally Open, 1=Normally Closed)
            /// </summary>
            public int? InputType { get; set; }
            
            /// <summary>
            /// Wall height dimension
            /// </summary>
            public double? WallHeight { get; set; }
            
            /// <summary>
            /// Wall thickness dimension
            /// </summary>
            public double? WallThickness { get; set; }
            
            /// <summary>
            /// Internal diameter
            /// </summary>
            public double? InternalDiameter { get; set; }
            
            /// <summary>
            /// Maximum search distance
            /// </summary>
            public double? MaxDistance { get; set; }
            
            /// <summary>
            /// Retract distance after touch
            /// </summary>
            public double? RetractDistance { get; set; }
            
            /// <summary>
            /// Fast probing rate
            /// </summary>
            public double? FastRate { get; set; }
            
            /// <summary>
            /// Slow probing rate
            /// </summary>
            public double? SlowRate { get; set; }
            
            /// <summary>
            /// Inside touch mode enabled
            /// </summary>
            public bool? InsideTouch { get; set; }
            
            /// <summary>
            /// Bore operations enabled
            /// </summary>
            public bool? BoreEnabled { get; set; }
            
            /// <summary>
            /// Surface plate mode
            /// </summary>
            public bool? SurfacePlate { get; set; }
        }

    }
}
