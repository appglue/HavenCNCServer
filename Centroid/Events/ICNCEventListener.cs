namespace HavenCNCServer.Centroid.Events
{
    /// <summary>
    /// Interface for listening to CNC events
    /// </summary>
    public interface ICNCEventListener
    {
        /// <summary>
        /// Called when a CNC event is received
        /// </summary>
        /// <param name="centroidEvent">The CNC event that was received</param>
        void EventReceived(ICentroidEvent centroidEvent);
    }
}