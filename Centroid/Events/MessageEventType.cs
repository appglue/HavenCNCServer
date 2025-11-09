namespace HavenCNCServer.Centroid.Events
{
    /// <summary>
    /// Types of CNC message events based on Centroid error code ranges
    /// </summary>
    public enum MessageEventType
    {
        // System startup/shutdown (100-299)
        /// <summary>System startup error</summary>
        StartupError,
        /// <summary>System exit or shutdown message</summary>
        ExitMessage,

        // Status messages (300-399)
        /// <summary>General status message</summary>
        StatusMessage,
        /// <summary>Job started notification</summary>
        JobStarted,
        /// <summary>Job completed successfully</summary>
        JobCompleted,
        /// <summary>Job was cancelled or aborted</summary>
        JobCancelled,

        // Faults and abnormal stops (400-499)
        /// <summary>General system fault</summary>
        SystemFault,
        /// <summary>Axis-specific fault or error</summary>
        AxisFault,
        /// <summary>Limit switch error</summary>
        LimitError,
        /// <summary>Probe-related error</summary>
        ProbeError,
        /// <summary>Communication error with CNC system</summary>
        CommunicationError,

        // Syntax errors (500-599)
        /// <summary>G-code syntax error</summary>
        SyntaxError,
        /// <summary>G-code programming error</summary>
        GCodeError,
        /// <summary>Parameter value error</summary>
        ParameterError,

        // Cutter compensation errors (600-699)
        /// <summary>Cutter compensation calculation error</summary>
        CutterCompensationError,

        // Parameter setting errors (700-799)
        /// <summary>Parameter setting or configuration error</summary>
        ParameterSettingError,

        // Canned cycle errors (800-899)
        /// <summary>Canned cycle (drill, tap, etc.) error</summary>
        CannedCycleError,

        // Miscellaneous errors (900-999)
        /// <summary>Miscellaneous system error</summary>
        MiscellaneousError,

        // Scaling/mirroring errors (1000-1099)
        /// <summary>Scaling or mirroring operation error</summary>
        ScalingError,

        // Configuration messages (111, 444, 555, etc.)
        /// <summary>Configuration change notification</summary>
        ConfigurationChange,

        // Default/unknown
        /// <summary>Unknown or unclassified message type</summary>
        Unknown
    }
}