namespace HavenCNCServer.CentriodAPI
{
    /// <summary>
    /// Supported step frequencies for the stepper pulse rate
    /// These are the only valid values supported by the Centroid system
    /// </summary>
    public enum StepFrequency
    {
        /// <summary>
        /// 100,000 steps per second
        /// </summary>
        _100000 = 100000,
        
        /// <summary>
        /// 200,000 steps per second (default)
        /// </summary>
        _200000 = 200000,
        
        /// <summary>
        /// 240,000 steps per second
        /// </summary>
        _240000 = 240000,
        
        /// <summary>
        /// 300,000 steps per second
        /// </summary>
        _300000 = 300000,
        
        /// <summary>
        /// 400,000 steps per second
        /// </summary>
        _400000 = 400000
    }
}