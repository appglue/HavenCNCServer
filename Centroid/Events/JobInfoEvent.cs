using System;
using CentroidAPI;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Centroid.Events
{
    /// <summary>
    /// Event containing job execution information
    /// </summary>
    public class JobInfoEvent : ICentroidEvent
    {
        // Static fields for duplicate detection
        private static string _lastJobInfoHash = string.Empty;
        private static int _jobInfoDuplicateSkipCount = 0;

        /// <summary>
        /// Timestamp when the job info event occurred
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Current executing line number in the G-code program
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Current stack level for nested programs or subroutines
        /// </summary>
        public int StackLevel { get; set; }

        /// <summary>
        /// Message associated with the job info event
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Type of message/event
        /// </summary>
        public string MessageType { get; set; } = "JobInfoEvent";

        /// <summary>
        /// Name of the currently running job
        /// </summary>
        public string JobName { get; set; } = string.Empty;

        /// <summary>
        /// Process JOB_INFO message - Contains job execution details
        /// Returns null if this is a duplicate that should be skipped
        /// </summary>
        public static JobInfoEvent? ProcessMessage(CNCPipe.InboundComm.CommPacket packet, System.Action<string> logToFile, System.Action<ICentroidEvent, string> storeMessage, System.Action<ICentroidEvent> notifyListeners)
        {
            // Check for job info duplicates first (ignoring movement/position changes)
            var jobInfoHash = CalculateJobInfoHash(packet);
            if (_lastJobInfoHash == jobInfoHash && !string.IsNullOrEmpty(_lastJobInfoHash))
            {
                _jobInfoDuplicateSkipCount++;

                // Log first few occurrences, then periodically
                if (_jobInfoDuplicateSkipCount <= 3)
                {
                    logToFile($"    Job Info unchanged - skipping duplicate #{_jobInfoDuplicateSkipCount} (movement only)");
                }
                else if (_jobInfoDuplicateSkipCount % 50 == 0)
                {
                    logToFile($"    Job Info unchanged - skipped {_jobInfoDuplicateSkipCount} duplicates (movement only)");
                }

                // Skip creating event and notifying listeners for job info duplicates
                return null;
            }

            // Job info has changed, reset skip counter and process normally
            if (_jobInfoDuplicateSkipCount > 0)
            {
                logToFile($"    --- Job Info CHANGED after {_jobInfoDuplicateSkipCount} movement-only updates ---");
                _jobInfoDuplicateSkipCount = 0;
            }

            // Store current job info hash for next comparison
            _lastJobInfoHash = jobInfoHash;

            logToFile($"    Job Info Update:");

            // According to API docs: "LineNumber - Current executing line number"
            var lineNumber = GetPacketProperty<int>(packet, "LineNumber", 0);
            if (lineNumber > 0)
            {
                logToFile($"    Line Number: {lineNumber}");
            }

            // According to API docs: "StackLevel - Reported stack level"
            var stackLevel = GetPacketProperty<int>(packet, "StackLevel", 0);
            if (stackLevel > 0)
            {
                logToFile($"    Stack Level: {stackLevel}");
            }

            // According to API docs: "Message - The current running job"
            var message = GetPacketProperty<string>(packet, "Message", "");
            if (!string.IsNullOrWhiteSpace(message))
            {
                logToFile($"    Current Job: {message}");
            }

            // Create and notify listeners of job info event only when job data actually changes
            var cncTimestamp = GetCNCTimestamp(packet);
            var jobInfoEvent = new JobInfoEvent
            {
                Timestamp = cncTimestamp,
                LineNumber = lineNumber,
                StackLevel = stackLevel,
                Message = message ?? string.Empty,
                JobName = message ?? string.Empty // Use the message as job name for now
            };

            // Log with CNC timestamp
            var timestampSource = cncTimestamp != DateTime.Now ? "CNC" : "Server";
            logToFile($"    Timestamp: {cncTimestamp:yyyy-MM-dd HH:mm:ss.fff} ({timestampSource})");

            // Store the job info event in history
            storeMessage(jobInfoEvent, "JOB_INFO");

            // Notify listeners
            notifyListeners(jobInfoEvent);

            // PHASE 2: Publish to CNCEventBus (new channel-based architecture)
            CNCEventBus.Instance.PublishMessage(jobInfoEvent);

            return jobInfoEvent;
        }

        /// <summary>
        /// Extract CNC timestamp from packet (CentroidAPI v5.40+)
        /// </summary>
        private static DateTime GetCNCTimestamp(CNCPipe.InboundComm.CommPacket packet)
        {
            try
            {
                // Try TimestampTicks first (CentroidAPI v5.40+)
                var ticks = GetPacketProperty<long>(packet, "TimestampTicks", 0);
                if (ticks > 0)
                {
                    return new DateTime(ticks);
                }

                // Try Timestamp property
                var timestamp = GetPacketProperty<DateTime>(packet, "Timestamp", DateTime.MinValue);
                if (timestamp != DateTime.MinValue)
                {
                    return timestamp;
                }

                // Fallback to server time
                return DateTime.Now;
            }
            catch
            {
                return DateTime.Now;
            }
        }

        /// <summary>
        /// Calculate a hash of only job-specific data (excludes position/movement data)
        /// </summary>
        private static string CalculateJobInfoHash(CNCPipe.InboundComm.CommPacket packet)
        {
            try
            {
                var hashBuilder = new System.Text.StringBuilder();

                // Only include job-specific properties, not position/movement data
                var lineNumber = GetPacketProperty<int>(packet, "LineNumber", 0);
                var stackLevel = GetPacketProperty<int>(packet, "StackLevel", 0);
                var message = GetPacketProperty<string>(packet, "Message", "");

                // Build hash from job-specific data only
                hashBuilder.Append($"LineNumber:{lineNumber}|");
                hashBuilder.Append($"StackLevel:{stackLevel}|");
                hashBuilder.Append($"Message:{message ?? ""}|");

                return hashBuilder.ToString();
            }
            catch (Exception ex)
            {
                return $"JOB_HASH_ERROR:{ex.Message}";
            }
        }

        /// <summary>
        /// Reset static tracking data (used when listener stops)
        /// </summary>
        public static void ResetTracking()
        {
            _lastJobInfoHash = string.Empty;
            _jobInfoDuplicateSkipCount = 0;
        }

        /// <summary>
        /// Safely get a property value from the CommPacket using reflection
        /// </summary>
        private static T GetPacketProperty<T>(CNCPipe.InboundComm.CommPacket packet, string propertyName, T defaultValue)
        {
            try
            {
                var packetType = packet.GetType();
                var property = packetType.GetProperty(propertyName);

                if (property != null && property.CanRead)
                {
                    var value = property.GetValue(packet);
                    if (value is T typedValue)
                        return typedValue;

                    // Try to convert the value if it's not the exact type
                    if (value != null)
                    {
                        try
                        {
                            return (T)Convert.ChangeType(value, typeof(T));
                        }
                        catch
                        {
                            // Conversion failed, return default
                        }
                    }
                }

                // Also try fields if property not found
                var field = packetType.GetField(propertyName);
                if (field != null)
                {
                    var value = field.GetValue(packet);
                    if (value is T typedValue)
                        return typedValue;

                    if (value != null)
                    {
                        try
                        {
                            return (T)Convert.ChangeType(value, typeof(T));
                        }
                        catch
                        {
                            // Conversion failed, return default
                        }
                    }
                }

                return defaultValue;
            }
            catch (Exception ex)
            {
                LogDebug($"Error getting packet property '{propertyName}': {ex.Message}", "JobInfoEvent");
                return defaultValue;
            }
        }
    }
}