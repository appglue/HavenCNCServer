namespace HavenCNCServer.Centroid.Events
{
    /// <summary>
    /// Interface for events that can serialize themselves for SignalR transmission
    /// All properties should be included without transformation
    /// </summary>
    public interface ISignalRSerializable
    {
        /// <summary>
        /// Serialize the event to an object that includes all properties for JSON transmission
        /// Should return the object itself or a complete representation
        /// </summary>
        /// <returns>Object containing all event data for SignalR clients</returns>
        object ToSignalRData();
    }
}