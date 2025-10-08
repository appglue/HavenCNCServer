namespace HavenCNCServer.Models
{
public static partial class CentroidConfigUtil
    {
        /// <summary>
        /// Represents ATC (Automatic Tool Changer) configuration
        /// </summary>
        public class ATCConfiguration
        {
            /// <summary>
            /// ATC type
            /// </summary>
            public ATCType Type { get; set; } = ATCType.None;
            
            /// <summary>
            /// Maximum number of tool positions
            /// </summary>
            public int MaxBins { get; set; }
            
            /// <summary>
            /// Tool change position X coordinate
            /// </summary>
            public double ChangePositionX { get; set; }
            
            /// <summary>
            /// Tool change position Y coordinate
            /// </summary>
            public double ChangePositionY { get; set; }
            
            /// <summary>
            /// Tool change position Z coordinate
            /// </summary>
            public double ChangePositionZ { get; set; }
            
            /// <summary>
            /// Time delay to start (turret systems)
            /// </summary>
            public double TimeDelayToStart { get; set; }
            
            /// <summary>
            /// Time to reverse (turret systems)
            /// </summary>
            public double TimeToReverse { get; set; }
            
            /// <summary>
            /// Time to fault (turret systems)
            /// </summary>
            public double TimeToFault { get; set; }
            
            /// <summary>
            /// Time per tool position (time-based turret)
            /// </summary>
            public double TimePerToolPosition { get; set; }
            
            /// <summary>
            /// Travel past distance (axis-driven turret)
            /// </summary>
            public double TravelPastDistance { get; set; }
            
            /// <summary>
            /// Travel behind distance (axis-driven turret)
            /// </summary>
            public double TravelBehindDistance { get; set; }
            
            /// <summary>
            /// Skip first count on reversal (carousel)
            /// </summary>
            public bool SkipFirstCountOnReversal { get; set; }
            
            /// <summary>
            /// Holding configuration for rack mount (0=Hole, 1=Fork)
            /// </summary>
            public int HoldingConfiguration { get; set; }
            
            /// <summary>
            /// Tool length measurement method (0=Fixed position, 1=Surface plate)
            /// </summary>
            public int ToolLengthMethod { get; set; }
        }
    }
}
