namespace HavenCNCServer.Models
{
    /// <summary>
    /// IO events that can be monitored or triggered
    /// </summary>
    public enum IOEvent
    {
        /// <summary>
        /// X-axis limit switch
        /// </summary>
        XLimit,

        /// <summary>
        /// Y-axis limit switch
        /// </summary>
        YLimit,

        /// <summary>
        /// Z-axis limit switch
        /// </summary>
        ZLimit,

        /// <summary>
        /// Tool touch pad sensor
        /// </summary>
        ToolTouchPad,

        /// <summary>
        /// Material touch pad sensor
        /// </summary>
        MaterialTouchPad
    }

    /// <summary>
    /// Directions for machine movement
    /// </summary>
    public enum MoveDirection
    {
        /// <summary>
        /// Move in positive X direction
        /// </summary>
        XPositive,

        /// <summary>
        /// Move in negative X direction
        /// </summary>
        XNegative,

        /// <summary>
        /// Move in positive Y direction
        /// </summary>
        YPositive,

        /// <summary>
        /// Move in negative Y direction
        /// </summary>
        YNegative,

        /// <summary>
        /// Move in positive Z direction
        /// </summary>
        ZPositive,

        /// <summary>
        /// Move in negative Z direction
        /// </summary>
        ZNegative
    }

    /// <summary>
    /// Types of movement commands
    /// </summary>
    public enum MoveType
    {
        /// <summary>
        /// Relative movement from current position
        /// </summary>
        Relative,

        /// <summary>
        /// Absolute movement to specific coordinates
        /// </summary>
        Absolute
    }
}
