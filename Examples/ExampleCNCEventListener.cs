using HavenCNCServer.Services;
using HavenCNCServer.Centriod.Events;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Examples
{
    /// <summary>
    /// Example implementation of ICNCEventListener to demonstrate usage
    /// </summary>
    public class ExampleCNCEventListener : ICNCEventListener
    {
        private readonly string _name;

        /// <summary>
        /// Creates a new example event listener with the specified name
        /// </summary>
        /// <param name="name">Name for the listener (used in log messages)</param>
        public ExampleCNCEventListener(string name)
        {
            _name = name;
        }

        /// <summary>
        /// Handles received CNC events and logs them with different formats based on event type
        /// </summary>
        /// <param name="centroidEvent">The CNC event that was received</param>
        public void EventReceived(ICentroidEvent centroidEvent)
        {
            switch (centroidEvent)
            {
                case JobInfoEvent jobInfo:
                    LogInfo($"[{_name}] Job Info: Line {jobInfo.LineNumber}, Job: {jobInfo.JobName}, Message: {jobInfo.Message}", "EventListener");
                    break;

                case DROEvent droEvent:
                    LogInfo($"[{_name}] DRO Position: X:{droEvent.Axis1:F4}, Y:{droEvent.Axis2:F4}, Z:{droEvent.Axis3:F4}, A:{droEvent.Axis4:F4}, B:{droEvent.Axis5:F4}, C:{droEvent.Axis6:F4}, U:{droEvent.Axis7:F4}, V:{droEvent.Axis8:F4}", "EventListener");
                    break;

                case MessageEvent messageEvent:
                    LogInfo($"[{_name}] Message ({messageEvent.EventType}): {messageEvent.Message}", "EventListener");
                    break;

                default:
                    LogInfo($"[{_name}] Unknown event type: {centroidEvent.GetType().Name} - {centroidEvent.Message}", "EventListener");
                    break;
            }
        }
    }

    /// <summary>
    /// Example usage and setup for CNC event listeners
    /// </summary>
    public static class CNCEventListenerExample
    {
        /// <summary>
        /// Example of how to register event listeners
        /// </summary>
        public static void SetupExampleListeners()
        {
            // Create example listeners
            var machineMonitor = new ExampleCNCEventListener("MachineMonitor");
            var jobTracker = new ExampleCNCEventListener("JobTracker");
            var positionTracker = new ExampleCNCEventListener("PositionTracker");

            // Register listeners with the CNCJobInfoListener
            CNCJobInfoListener.AddListener(machineMonitor);
            CNCJobInfoListener.AddListener(jobTracker);
            CNCJobInfoListener.AddListener(positionTracker);

            LogInfo("Example CNC event listeners registered", "EventListener");
        }

        /// <summary>
        /// Example of how to remove event listeners
        /// </summary>
        public static void CleanupExampleListeners()
        {
            // Clear all listeners (or remove specific ones with RemoveListener)
            CNCJobInfoListener.ClearAllListeners();
            LogInfo("All CNC event listeners removed", "EventListener");
        }
    }
}