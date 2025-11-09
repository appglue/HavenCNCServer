namespace HavenCNCServer.Centroid.Data
{
    /// <summary>
    /// ATC types supported by the system
    /// </summary>
    public enum ATCType
    {
        /// <summary>No automatic tool changer</summary>
        None = 0,
        /// <summary>Rotating carousel ATC</summary>
        Carousel = 1,
        /// <summary>Lathe counter-rotating turret</summary>
        CounterTurret = 2,
        /// <summary>Gray code position sensing (type 1)</summary>
        GreyCode1 = 3,
        /// <summary>Gray code position sensing (type 2)</summary>
        GreyCode2 = 4,
        /// <summary>Time-based turret positioning</summary>
        TimeTurret = 5,
        /// <summary>Servo axis driven turret</summary>
        AxisDrivenTurret = 6,
        /// <summary>Fixed position rack system</summary>
        RackMount = 7,
        /// <summary>Electric motor driven turret</summary>
        ElectricTurret = 8
    }
}
