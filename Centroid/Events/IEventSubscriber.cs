using System;

namespace HavenCNCServer.Centroid.Events
{
    /// <summary>
    /// Interface for components that want to subscribe to CNC events from the event bus
    /// </summary>
    public interface IEventSubscriber
    {
        /// <summary>
        /// Receive position/DRO update (latest value only)
        /// </summary>
        void OnPositionUpdate(DROEvent position);

        /// <summary>
        /// Receive log message (all messages)
        /// </summary>
        void OnLogMessage(LogEvent log);

        /// <summary>
        /// Receive CNC message (all messages)
        /// </summary>
        void OnCNCMessage(ICentroidEvent message);

        /// <summary>
        /// Receive server status update (heartbeat, every 2 seconds)
        /// </summary>
        void OnServerStatus(ServerStatusEvent status);

        /// <summary>
        /// Specify which event types this subscriber wants to receive
        /// </summary>
        EventTypeFlags GetSubscribedEvents();
    }

    /// <summary>
    /// Flags to specify which event types a subscriber wants to receive
    /// </summary>
    [Flags]
    public enum EventTypeFlags
    {
        None = 0,
        Position = 1,
        Logs = 2,
        Messages = 4,
        ServerStatus = 8,
        All = Position | Logs | Messages | ServerStatus
    }
}
