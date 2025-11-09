namespace HavenCNCServer.Centroid.Data
{
    /// <summary>
    /// Represents an I/O function assignment
    /// </summary>
    public class IOFunction
    {
        /// <summary>
        /// Function name (e.g., "EStopOk", "SpindleEnable")
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// I/O number (1-64 for standard I/O)
        /// </summary>
        public int? Number { get; set; }

        /// <summary>
        /// Whether the input/output is inverted
        /// </summary>
        public bool? IsInverted { get; set; }
    }
}
