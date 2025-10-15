using HavenCNCServer.Services;
using HavenCNCServer.Centriod.Events;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Examples
{
    /// <summary>
    /// Example usage of the CNC message storage functionality
    /// </summary>
    public static class MessageStorageExample
    {
        /// <summary>
        /// Demonstrate various ways to retrieve stored CNC messages
        /// </summary>
        public static void DemonstrateMessageStorage()
        {
            LogInfo("=== CNC Message Storage Examples ===", "MessageStorage");

            // Example 1: Get all stored messages
            var allMessages = CNCJobInfoListener.GetStoredMessages();
            LogInfo($"Total stored messages: {allMessages.Count}", "MessageStorage");

            // Example 2: Get messages from the last 5 seconds
            var last5Seconds = CNCJobInfoListener.GetRecentMessages(5000);
            LogInfo($"Messages from last 5 seconds: {last5Seconds.Count}", "MessageStorage");

            // Example 3: Get messages from the last 30 seconds
            var last30Seconds = CNCJobInfoListener.GetRecentMessages(30000);
            LogInfo($"Messages from last 30 seconds: {last30Seconds.Count}", "MessageStorage");

            // Example 4: Get only DRO position update messages from last 10 seconds
            var recentDROEvents = CNCJobInfoListener.GetRecentMessagesByType<DROEvent>(10000);
            LogInfo($"Recent DRO position updates: {recentDROEvents.Count}", "MessageStorage");

            // Example 5: Get only message window messages from last 10 seconds
            var recentMessageEvents = CNCJobInfoListener.GetRecentMessagesByType<MessageEvent>(10000);
            LogInfo($"Recent message window messages: {recentMessageEvents.Count}", "MessageStorage");

            // Example 6: Get only job info events from last 10 seconds
            var recentJobEvents = CNCJobInfoListener.GetRecentMessagesByType<JobInfoEvent>(10000);
            LogInfo($"Recent job info events: {recentJobEvents.Count}", "MessageStorage");

            // Example 7: Get messages by communication type
            var droUpdates = CNCJobInfoListener.GetRecentMessagesByCommunicationType(10000, "DRO_UPDATE");
            LogInfo($"Recent DRO_UPDATE messages: {droUpdates.Count}", "MessageStorage");

            var messageWindowMessages = CNCJobInfoListener.GetRecentMessagesByCommunicationType(10000, "MESSAGE_WINDOW_MESSAGE");
            LogInfo($"Recent MESSAGE_WINDOW_MESSAGE messages: {messageWindowMessages.Count}", "MessageStorage");

            // Example 8: Display sample of recent messages with details
            if (last30Seconds.Count > 0)
            {
                LogInfo("Sample of recent messages:", "MessageStorage");
                foreach (var msg in last30Seconds.Take(5)) // Show first 5
                {
                    var eventType = msg.Event.GetType().Name;
                    var timeAgo = DateTime.Now - msg.Timestamp;
                    
                    LogInfo($"  [{msg.Timestamp:HH:mm:ss.fff}] ({timeAgo.TotalSeconds:F1}s ago)", "MessageStorage");
                    LogInfo($"    Type: {msg.CommunicationType} -> {eventType}", "MessageStorage");
                    LogInfo($"    Message: {msg.Event.Message}", "MessageStorage");
                    
                    // Show specific details based on event type
                    switch (msg.Event)
                    {
                        case DROEvent droEvent:
                            LogInfo($"    Coordinates: X={droEvent.Axis1:F4}, Y={droEvent.Axis2:F4}, Z={droEvent.Axis3:F4}", "MessageStorage");
                            break;
                        case MessageEvent messageEvent:
                            LogInfo($"    Event Type: {messageEvent.EventType}, Code: {messageEvent.EventCode}", "MessageStorage");
                            break;
                        case JobInfoEvent jobEvent:
                            LogInfo($"    Line: {jobEvent.LineNumber}, Job: {jobEvent.JobName}", "MessageStorage");
                            break;
                    }
                    LogInfo("", "MessageStorage"); // Empty line for readability
                }
            }

            // Example 9: Get storage statistics
            var totalCount = CNCJobInfoListener.GetStoredMessageCount();
            LogInfo($"Current storage count: {totalCount}/1000 messages", "MessageStorage");

            LogInfo("=== End of Message Storage Examples ===", "MessageStorage");
        }

        /// <summary>
        /// Example: Monitor for specific error conditions in recent messages
        /// </summary>
        public static void MonitorForErrors()
        {
            // Get messages from last 10 seconds
            var recentMessages = CNCJobInfoListener.GetRecentMessages(10000);
            
            // Check for error messages
            var errorMessages = recentMessages
                .Where(msg => msg.Event is MessageEvent msgEvent && 
                             (msgEvent.EventType == MessageEventType.SystemFault ||
                              msgEvent.EventType == MessageEventType.AxisFault ||
                              msgEvent.EventType == MessageEventType.LimitError))
                .ToList();

            if (errorMessages.Any())
            {
                LogWarning($"⚠️ Found {errorMessages.Count} error messages in the last 10 seconds!", "ErrorMonitor");
                foreach (var errorMsg in errorMessages)
                {
                    var msgEvent = (MessageEvent)errorMsg.Event;
                    LogError($"  [{errorMsg.Timestamp:HH:mm:ss}] {msgEvent.EventType}: {msgEvent.Message}", "ErrorMonitor");
                }
            }
            else
            {
                LogInfo("✅ No error messages found in recent history", "ErrorMonitor");
            }
        }

        /// <summary>
        /// Example: Track job progress by analyzing recent job events
        /// </summary>
        public static void TrackJobProgress()
        {
            // Get job-related events from last 60 seconds
            var recentJobEvents = CNCJobInfoListener.GetRecentMessagesByType<JobInfoEvent>(60000);
            var recentMessageEvents = CNCJobInfoListener.GetRecentMessagesByType<MessageEvent>(60000)
                .Where(msg => msg.Event is MessageEvent msgEvent && 
                             (msgEvent.EventType == MessageEventType.JobStarted ||
                              msgEvent.EventType == MessageEventType.JobCompleted ||
                              msgEvent.EventType == MessageEventType.JobCancelled))
                .ToList();

            if (recentJobEvents.Any() || recentMessageEvents.Any())
            {
                LogInfo("📊 Recent job activity:", "JobTracker");
                
                // Show job info events
                foreach (var jobEvent in recentJobEvents.Take(3))
                {
                    var jobInfo = (JobInfoEvent)jobEvent.Event;
                    LogInfo($"  [{jobEvent.Timestamp:HH:mm:ss}] Line {jobInfo.LineNumber}: {jobInfo.JobName}", "JobTracker");
                }

                // Show job lifecycle events
                foreach (var msgEvent in recentMessageEvents.Take(3))
                {
                    var msgInfo = (MessageEvent)msgEvent.Event;
                    LogInfo($"  [{msgEvent.Timestamp:HH:mm:ss}] {msgInfo.EventType}: {msgInfo.Message}", "JobTracker");
                }
            }
            else
            {
                LogInfo("ℹ️ No recent job activity detected", "JobTracker");
            }
        }
    }
}