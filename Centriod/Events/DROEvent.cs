using System;

namespace HavenCNCServer.Centriod.Events
{
    /// <summary>
    /// Event containing Digital Readout (DRO) position information for all axes
    /// </summary>
    public class DROEvent : ICentroidEvent
    {
        /// <summary>
        /// Timestamp when the DRO update occurred
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Position value for Axis 1 (typically X axis)
        /// </summary>
        public double Axis1 { get; set; }
        
        /// <summary>
        /// Position value for Axis 2 (typically Y axis)
        /// </summary>
        public double Axis2 { get; set; }
        
        /// <summary>
        /// Position value for Axis 3 (typically Z axis)
        /// </summary>
        public double Axis3 { get; set; }
        
        /// <summary>
        /// Position value for Axis 4 (typically A axis)
        /// </summary>
        public double Axis4 { get; set; }
        
        /// <summary>
        /// Position value for Axis 5 (typically B axis)
        /// </summary>
        public double Axis5 { get; set; }
        
        /// <summary>
        /// Position value for Axis 6 (typically C axis)
        /// </summary>
        public double Axis6 { get; set; }
        
        /// <summary>
        /// Position value for Axis 7
        /// </summary>
        public double Axis7 { get; set; }
        
        /// <summary>
        /// Position value for Axis 8
        /// </summary>
        public double Axis8 { get; set; }
        
        /// <summary>
        /// Message associated with the DRO update
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}