namespace HavenCNCServer.Centriod.Data
{
    /// <summary>
    /// Represents system hardware detection and capabilities
    /// </summary>
    public class SystemHardwareInfo
    {
            /// <summary>
            /// System type (Acorn, AcornSix, Hickory)
            /// </summary>
            public string? SystemType { get; set; }
            
            /// <summary>
            /// Number of base I/O points
            /// </summary>
            public int BaseInputs { get; set; }
            
            /// <summary>
            /// Number of base I/O points
            /// </summary>
            public int BaseOutputs { get; set; }
            
            /// <summary>
            /// Number of expansion boards detected
            /// </summary>
            public int ExpansionBoards { get; set; }
            
            /// <summary>
            /// Total available inputs
            /// </summary>
            public int TotalInputs { get; set; }
            
            /// <summary>
            /// Total available outputs
            /// </summary>
            public int TotalOutputs { get; set; }
            
            /// <summary>
            /// Available input numbers
            /// </summary>
            public List<int> AvailableInputs { get; set; } = new List<int>();
            
        /// <summary>
        /// Available output numbers
        /// </summary>
        public List<int> AvailableOutputs { get; set; } = new List<int>();
    }
}