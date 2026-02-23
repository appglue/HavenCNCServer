namespace HavenCNCServer.Centroid.Data
{
    /// <summary>
    /// Wireless MPG device type — maps to Parameter 411 (MPG_TYPE_PARM).
    /// Use None to disable the wireless MPG (sets P218 = 0).
    /// </summary>
    public enum WirelessMpgType
    {
        /// <summary>No wireless MPG — disables USB MPG by zeroing P218</summary>
        None = -1,

        /// <summary>Centroid CWP-4 wireless pendant (P411 = 0)</summary>
        CWP4 = 0,

        /// <summary>Centroid WMPG-4 wireless MPG (P411 = 1)</summary>
        WMPG4 = 1,

        /// <summary>Centroid WMPG-6 wireless MPG for 6-axis machines (P411 = 2)</summary>
        WMPG6 = 2,

        /// <summary>Centroid WMPG-4 for plasma machines (P411 = 3)</summary>
        WMPG4Plasma = 3
    }

    /// <summary>
    /// MPG jog response performance mode — maps to Parameter 855 (MPG_PERFORMANCE_MODE_PARAM).
    /// </summary>
    public enum MpgPerformanceMode
    {
        /// <summary>Smooth response — prioritizes smooth motion (P855 = 0)</summary>
        SmoothResponse = 0,

        /// <summary>Balanced response — compromise between smooth and quick (P855 = 1)</summary>
        BalancedResponse = 1,

        /// <summary>Quick response — prioritizes fast reaction time (P855 = 2)</summary>
        QuickResponse = 2
    }

    /// <summary>
    /// Wireless MPG and MPG performance configuration.
    /// Controls Parameter 411 (device type), Parameter 218 (active axes bitmask),
    /// and Parameter 855 (jog performance mode).
    /// </summary>
    public class MpgConfiguration
    {
        /// <summary>
        /// Wireless MPG device type.
        /// Set to None to disable. Otherwise, sets P411 and P218.
        /// </summary>
        public WirelessMpgType WirelessMpgType { get; set; } = WirelessMpgType.None;

        /// <summary>
        /// MPG jog response performance mode. Controls Parameter 855.
        /// </summary>
        public MpgPerformanceMode Performance { get; set; } = MpgPerformanceMode.SmoothResponse;
    }
}
