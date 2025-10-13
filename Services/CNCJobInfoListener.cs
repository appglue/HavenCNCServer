using CentroidAPI;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Services
{
    /// <summary>
    /// Base interface for all Centroid CNC events
    /// </summary>
    public interface ICentroidEvent
    {
        /// <summary>
        /// Timestamp when the event occurred
        /// </summary>
        DateTime Timestamp { get; set; }
        /// <summary>
        /// Message associated with the event
        /// </summary>
        string Message { get; set;}
    }

    /// <summary>
    /// Event containing job execution information
    /// </summary>
    public class JobInfoEvent : ICentroidEvent
    {
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
        /// Name of the currently running job
        /// </summary>
        public string JobName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Event containing Digital Readout (DRO) position information for all axes
    /// </summary>
    public class DROEvent : ICentroidEvent
    {
        /// <summary>
        /// Timestamp when the DRO update occurred
        /// </summary>
        public DateTime Timestamp { get; set; }
        /// <summary>
        /// Position value for Axis 1 (typically X axis)
        /// </summary>
        public double Axis1 { get; set; }
        /// <summary>
        /// Position value for Axis 2 (typically Y axis)
        /// </summary>
        public double Axis2 { get; set; }
        /// <summary>
        /// Position value for Axis 3 (typically Z axis)
        /// </summary>
        public double Axis3 { get; set; }
        /// <summary>
        /// Position value for Axis 4 (typically A axis)
        /// </summary>
        public double Axis4 { get; set; }
        /// <summary>
        /// Position value for Axis 5 (typically B axis)
        /// </summary>
        public double Axis5 { get; set; }
        /// <summary>
        /// Position value for Axis 6 (typically C axis)
        /// </summary>
        public double Axis6 { get; set; }
        /// <summary>
        /// Position value for Axis 7
        /// </summary>
        public double Axis7 { get; set; }
        /// <summary>
        /// Position value for Axis 8
        /// </summary>
        public double Axis8 { get; set; }
        /// <summary>
        /// Message associated with the DRO update
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Types of CNC message events based on Centroid error code ranges
    /// </summary>
    public enum MessageEventType
    {
        // System startup/shutdown (100-299)
        /// <summary>System startup error</summary>
        StartupError,
        /// <summary>System exit or shutdown message</summary>
        ExitMessage,
        
        // Status messages (300-399)
        /// <summary>General status message</summary>
        StatusMessage,
        /// <summary>Job started notification</summary>
        JobStarted,
        /// <summary>Job completed successfully</summary>
        JobCompleted,
        /// <summary>Job was cancelled or aborted</summary>
        JobCancelled,
        
        // Faults and abnormal stops (400-499)
        /// <summary>General system fault</summary>
        SystemFault,
        /// <summary>Axis-specific fault or error</summary>
        AxisFault,
        /// <summary>Limit switch error</summary>
        LimitError,
        /// <summary>Probe-related error</summary>
        ProbeError,
        /// <summary>Communication error with CNC system</summary>
        CommunicationError,
        
        // Syntax errors (500-599)
        /// <summary>G-code syntax error</summary>
        SyntaxError,
        /// <summary>G-code programming error</summary>
        GCodeError,
        /// <summary>Parameter value error</summary>
        ParameterError,
        
        // Cutter compensation errors (600-699)
        /// <summary>Cutter compensation calculation error</summary>
        CutterCompensationError,
        
        // Parameter setting errors (700-799)
        /// <summary>Parameter setting or configuration error</summary>
        ParameterSettingError,
        
        // Canned cycle errors (800-899)
        /// <summary>Canned cycle (drill, tap, etc.) error</summary>
        CannedCycleError,
        
        // Miscellaneous errors (900-999)
        /// <summary>Miscellaneous system error</summary>
        MiscellaneousError,
        
        // Scaling/mirroring errors (1000-1099)
        /// <summary>Scaling or mirroring operation error</summary>
        ScalingError,
        
        // Configuration messages (111, 444, 555, etc.)
        /// <summary>Configuration change notification</summary>
        ConfigurationChange,
        
        // Default/unknown
        /// <summary>Unknown or unclassified message type</summary>
        Unknown
    }

    /// <summary>
    /// Event containing a CNC message with error code and classification
    /// </summary>
    public class MessageEvent : ICentroidEvent {
        /// <summary>
        /// Timestamp when the message event occurred
        /// </summary>
        public DateTime Timestamp { get; set; }
        /// <summary>
        /// Numeric error or message code from the CNC system
        /// </summary>
        public int EventCode { get; set; }
        /// <summary>
        /// Message text content
        /// </summary>
        public string Message { get; set; } = string.Empty;
        /// <summary>
        /// Classified type of the message event
        /// </summary>
        public MessageEventType EventType { get; set; }
    }

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

    /// <summary>
    /// Stored message with timestamp for message history
    /// </summary>
    public class StoredMessage
    {
        /// <summary>
        /// Timestamp when the message was stored
        /// </summary>
        public DateTime Timestamp { get; set; }
        /// <summary>
        /// The CNC event that was stored
        /// </summary>
        public ICentroidEvent Event { get; set; } = null!;
        /// <summary>
        /// Type of communication that generated this message
        /// </summary>
        public string CommunicationType { get; set; } = string.Empty;
        /// <summary>
        /// Timestamp in milliseconds since Unix epoch for time-based filtering
        /// </summary>
        public long TimestampMs => ((DateTimeOffset)Timestamp).ToUnixTimeMilliseconds();

        /// <summary>
        /// Creates a new stored message with the current timestamp
        /// </summary>
        /// <param name="centroidEvent">The CNC event to store</param>
        /// <param name="commType">The communication type</param>
        public StoredMessage(ICentroidEvent centroidEvent, string commType)
        {
            Timestamp = DateTime.Now;
            Event = centroidEvent;
            CommunicationType = commType;
        }
    }

    /// <summary>
    /// Service for listening to CNC JOB_INFO messages and outputting them to debug logging
    /// </summary>
    public static class CNCJobInfoListener
    {
        private static bool _isListening = false;
        private static readonly object _lock = new object();
        private static CNCPipe? _currentCNCPipe = null;
        
        // File logging for detailed listener data
        private static StreamWriter? _logWriter;
        private static int _messageCount = 0;
        private static int _lastReportedCount = 0;
        private static DateTime _sessionStartTime = DateTime.Now;
        
        // Duplicate message detection
        private static Dictionary<string, string> _lastMessageHashes = new Dictionary<string, string>();
        private static Dictionary<string, int> _duplicateCounters = new Dictionary<string, int>();
        
        // DRO position tracking
        private static double[]? _lastDroPositions = null;
        private static int _droSamePositionSkipCount = 0;

        // Event listener management
        private static readonly List<ICNCEventListener> _eventListeners = new List<ICNCEventListener>();
        private static readonly object _listenersLock = new object();

        // Message storage for recent messages (most recent first)
        private static readonly List<StoredMessage> _storedMessages = new List<StoredMessage>();
        private static readonly object _storedMessagesLock = new object();
        private static readonly int MaxStoredMessages = 1000;

        /// <summary>
        /// Helper method to determine if a message type represents an error condition
        /// </summary>
        /// <param name="messageType">The message event type to check</param>
        /// <returns>True if the message type represents an error, false otherwise</returns>
        public static bool IsErrorMessage(MessageEventType messageType)
        {
            return messageType switch
            {
                // Critical errors that stop operation
                MessageEventType.SystemFault or
                MessageEventType.AxisFault or
                MessageEventType.LimitError or
                MessageEventType.ProbeError or
                MessageEventType.CommunicationError or
                MessageEventType.StartupError => true,
                
                // All other message types are not considered errors
                _ => false
            };
        }

        /// <summary>
        /// Event fired when JOB_INFO message is received
        /// </summary>
        public static event Action<JobInfoData>? JobInfoReceived;

        /// <summary>
        /// Gets whether the listener is currently active
        /// </summary>
        public static bool IsListening
        {
            get
            {
                lock (_lock)
                {
                    return _isListening;
                }
            }
        }

        /// <summary>
        /// Add an event listener for CNC events
        /// </summary>
        /// <param name="listener">The event listener to add</param>
        public static void AddListener(ICNCEventListener listener)
        {
            if (listener == null)
                throw new ArgumentNullException(nameof(listener));

            lock (_listenersLock)
            {
                if (!_eventListeners.Contains(listener))
                {
                    _eventListeners.Add(listener);
                    LogInfo($"Added CNC event listener: {listener.GetType().Name}", "JobInfo");
                }
                else
                {
                    LogWarning($"Event listener already exists: {listener.GetType().Name}", "JobInfo");
                }
            }
        }

        /// <summary>
        /// Remove an event listener for CNC events
        /// </summary>
        /// <param name="listener">The event listener to remove</param>
        /// <returns>True if the listener was removed, false if it wasn't found</returns>
        public static bool RemoveListener(ICNCEventListener listener)
        {
            if (listener == null)
                return false;

            lock (_listenersLock)
            {
                bool removed = _eventListeners.Remove(listener);
                if (removed)
                {
                    LogInfo($"Removed CNC event listener: {listener.GetType().Name}", "JobInfo");
                }
                else
                {
                    LogWarning($"Event listener not found for removal: {listener.GetType().Name}", "JobInfo");
                }
                return removed;
            }
        }

        /// <summary>
        /// Remove all event listeners
        /// </summary>
        public static void ClearAllListeners()
        {
            lock (_listenersLock)
            {
                int count = _eventListeners.Count;
                _eventListeners.Clear();
                LogInfo($"Cleared all CNC event listeners ({count} listeners removed)", "JobInfo");
            }
        }

        /// <summary>
        /// Get the number of registered event listeners
        /// </summary>
        /// <returns>Number of active listeners</returns>
        public static int GetListenerCount()
        {
            lock (_listenersLock)
            {
                return _eventListeners.Count;
            }
        }

        /// <summary>
        /// Get all stored messages (most recent first)
        /// </summary>
        /// <returns>List of stored messages with most recent at index 0</returns>
        public static List<StoredMessage> GetStoredMessages()
        {
            lock (_storedMessagesLock)
            {
                // Return a copy to prevent external modification
                return new List<StoredMessage>(_storedMessages);
            }
        }

        /// <summary>
        /// Get stored messages within the specified time cutoff
        /// </summary>
        /// <param name="timeCutoffMs">Time cutoff in milliseconds from now (e.g., 5000 for last 5 seconds)</param>
        /// <returns>List of stored messages within the time cutoff (most recent first)</returns>
        public static List<StoredMessage> GetRecentMessages(long timeCutoffMs)
        {
            var cutoffTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() - timeCutoffMs;
            
            lock (_storedMessagesLock)
            {
                return _storedMessages
                    .Where(msg => msg.TimestampMs >= cutoffTime)
                    .ToList(); // Already ordered most recent first
            }
        }

        /// <summary>
        /// Get stored messages of a specific type within the time cutoff
        /// </summary>
        /// <typeparam name="T">Type of event to filter by</typeparam>
        /// <param name="timeCutoffMs">Time cutoff in milliseconds from now</param>
        /// <returns>List of matching stored messages (most recent first)</returns>
        public static List<StoredMessage> GetRecentMessagesByType<T>(long timeCutoffMs) where T : ICentroidEvent
        {
            var cutoffTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() - timeCutoffMs;
            
            lock (_storedMessagesLock)
            {
                return _storedMessages
                    .Where(msg => msg.TimestampMs >= cutoffTime && msg.Event is T)
                    .ToList();
            }
        }

        /// <summary>
        /// Get stored messages by communication type within the time cutoff
        /// </summary>
        /// <param name="timeCutoffMs">Time cutoff in milliseconds from now</param>
        /// <param name="communicationType">Communication type to filter by (e.g., "DRO_UPDATE", "MESSAGE_WINDOW_MESSAGE")</param>
        /// <returns>List of matching stored messages (most recent first)</returns>
        public static List<StoredMessage> GetRecentMessagesByCommunicationType(long timeCutoffMs, string communicationType)
        {
            var cutoffTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() - timeCutoffMs;
            
            lock (_storedMessagesLock)
            {
                return _storedMessages
                    .Where(msg => msg.TimestampMs >= cutoffTime && 
                                  string.Equals(msg.CommunicationType, communicationType, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        /// <summary>
        /// Get the count of stored messages
        /// </summary>
        /// <returns>Number of messages currently stored</returns>
        public static int GetStoredMessageCount()
        {
            lock (_storedMessagesLock)
            {
                return _storedMessages.Count;
            }
        }

        /// <summary>
        /// Clear all stored messages
        /// </summary>
        public static void ClearStoredMessages()
        {
            lock (_storedMessagesLock)
            {
                var count = _storedMessages.Count;
                _storedMessages.Clear();
                LogInfo($"Cleared {count} stored messages", "JobInfo");
            }
        }

        /// <summary>
        /// Initialize file logging for detailed listener data
        /// </summary>
        private static void InitializeFileLogging()
        {
            try
            {
                // Use configured log directory, fallback to application directory
                var logsDir = SettingsManager.Settings.Files.JobListenerLogsDirectory;
                
                // If the configured directory is relative or doesn't exist, make it absolute
                if (!Path.IsPathRooted(logsDir))
                {
                    var appDir = AppDomain.CurrentDomain.BaseDirectory;
                    logsDir = Path.Combine(appDir, logsDir);
                }
                
                Directory.CreateDirectory(logsDir);
                
                // Create main summary log
                var logFileName = $"JobListener_{DateTime.Now:yyyyMMdd_HHmmss}.log";
                var logFilePath = Path.Combine(logsDir, logFileName);
                
                _logWriter = new StreamWriter(logFilePath, true);
                _logWriter.AutoFlush = true;
                
                _logWriter.WriteLine($"=== Job Listener Session Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                _logWriter.WriteLine($"Log file: {logFilePath}");
                _logWriter.WriteLine("");
                
                LogInfo($"Job listener detailed logging to: {logFilePath}", "JobInfo");
            }
            catch (Exception ex)
            {
                LogError($"Failed to initialize job listener file logging: {ex.Message}", "JobInfo");
            }
        }

        /// <summary>
        /// Log message to file with timestamp
        /// </summary>
        private static void LogToFile(string message)
        {
            try
            {
                _logWriter?.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
            }
            catch (Exception ex)
            {
                LogError($"Error writing to job listener log file: {ex.Message}", "JobInfo");
            }
        }

        /// <summary>
        /// Notify all registered event listeners of a CNC event
        /// </summary>
        /// <param name="centroidEvent">The event to notify listeners about</param>
        private static void NotifyListeners(ICentroidEvent centroidEvent)
        {
            lock (_listenersLock)
            {
                foreach (var listener in _eventListeners.ToList()) // ToList() to avoid collection modified exceptions
                {
                    try
                    {
                        listener.EventReceived(centroidEvent);
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error notifying event listener {listener.GetType().Name}: {ex.Message}", "JobInfo");
                    }
                }
            }
        }

        /// <summary>
        /// Store a CNC event in the message history
        /// </summary>
        /// <param name="centroidEvent">The event to store</param>
        /// <param name="communicationType">The communication type</param>
        private static void StoreMessage(ICentroidEvent centroidEvent, string communicationType)
        {
            lock (_storedMessagesLock)
            {
                // Create stored message
                var storedMessage = new StoredMessage(centroidEvent, communicationType);
                
                // Add at the beginning (most recent first)
                _storedMessages.Insert(0, storedMessage);
                
                // Trim to maximum size if needed
                if (_storedMessages.Count > MaxStoredMessages)
                {
                    var removed = _storedMessages.Count - MaxStoredMessages;
                    _storedMessages.RemoveRange(MaxStoredMessages, removed);
                    
                    // Log trimming occasionally to avoid spam
                    if (removed > 0 && (_messageCount % 500) == 0)
                    {
                        LogDebug($"Trimmed {removed} old messages from storage (keeping last {MaxStoredMessages})", "JobInfo");
                    }
                }
            }
        }

        /// <summary>
        /// Classify a CNC message based on error codes and content according to Centroid documentation
        /// </summary>
        /// <param name="message">The message to classify</param>
        /// <returns>Tuple of (EventCode, EventType)</returns>
        private static (int eventCode, MessageEventType eventType) ClassifyMessage(string message)
        {
            var eventCode = ExtractErrorCode(message);
            var eventType = ClassifyByErrorCode(eventCode, message);
            return (eventCode, eventType);
        }

        /// <summary>
        /// Extract numeric error code from message text
        /// </summary>
        /// <param name="message">Message text</param>
        /// <returns>Error code or 0 if not found</returns>
        private static int ExtractErrorCode(string message)
        {
            // Look for patterns like "Error 123" or "123:" or message starting with digits
            var patterns = new[]
            {
                @"(?:Error|Code|Fault)\s*(\d+)", // "Error 123", "Code 123", "Fault 123"
                @"^(\d{3,4})\s*:", // "123:" at start of message
                @"^(\d{3,4})\s+", // "123 " at start of message
                @"\b(\d{3,4})\b" // Any 3-4 digit number
            };

            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(message, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int code))
                {
                    return code;
                }
            }

            return 0; // No code found
        }

        /// <summary>
        /// Classify message type based on error code ranges from Centroid documentation
        /// </summary>
        /// <param name="errorCode">Numeric error code</param>
        /// <param name="message">Original message for fallback classification</param>
        /// <returns>Message event type</returns>
        private static MessageEventType ClassifyByErrorCode(int errorCode, string message)
        {
            // Classify based on Centroid error code ranges
            if (errorCode == 0)
            {
                return ClassifyByContent(message);
            }
            
            // Configuration messages (special codes)
            if (errorCode == 111 || errorCode == 444 || errorCode == 555 || errorCode == 556 || 
                errorCode == 777 || errorCode == 888 || errorCode == 999)
            {
                return MessageEventType.ConfigurationChange;
            }
            
            // Startup errors and messages (100-199)
            if (errorCode >= 102 && errorCode <= 106)
            {
                return MessageEventType.StartupError;
            }
            if (errorCode == 199)
            {
                return MessageEventType.StatusMessage; // "CNC started"
            }
            
            // Exit messages (200-299)
            if (errorCode >= 201 && errorCode <= 204 || errorCode == 222)
            {
                return errorCode == 222 ? MessageEventType.StatusMessage : MessageEventType.ExitMessage;
            }
            
            // Status messages (300-399)
            if (errorCode >= 301 && errorCode <= 347)
            {
                return ClassifyStatusMessage(errorCode);
            }
            
            // Faults and abnormal stops (400-499)
            if (errorCode >= 401 && errorCode <= 490)
            {
                return ClassifyFaultMessage(errorCode);
            }
            
            // Syntax errors (500-599)
            if (errorCode >= 501 && errorCode <= 552)
            {
                return MessageEventType.SyntaxError;
            }
            
            // Cutter compensation errors (600-699)
            if (errorCode >= 601 && errorCode <= 608)
            {
                return MessageEventType.CutterCompensationError;
            }
            
            // Parameter setting errors (700-799)
            if (errorCode >= 701 && errorCode <= 705)
            {
                return MessageEventType.ParameterSettingError;
            }
            
            // Canned cycle errors (800-899)
            if (errorCode >= 801 && errorCode <= 807)
            {
                return MessageEventType.CannedCycleError;
            }
            
            // Miscellaneous errors (900-999)
            if (errorCode >= 901 && errorCode <= 949)
            {
                return MessageEventType.MiscellaneousError;
            }
            
            // Scaling/mirroring errors (1000-1199)
            if (errorCode >= 1001 && errorCode <= 1199)
            {
                return MessageEventType.ScalingError;
            }
            
            // Unknown error code
            return MessageEventType.Unknown;
        }

        /// <summary>
        /// Classify status messages (300-399 range) into more specific types
        /// </summary>
        private static MessageEventType ClassifyStatusMessage(int errorCode)
        {
            switch (errorCode)
            {
                case 301:
                    return MessageEventType.StatusMessage;    // "Stopped"
                case 302:
                    return MessageEventType.StatusMessage;    // "Moving..."
                case 303:
                    return MessageEventType.StatusMessage;    // "Paused..."
                case 304:
                    return MessageEventType.StatusMessage;    // "MDI..."
                case 305:
                    return MessageEventType.StatusMessage;    // "Processing..."
                case 306:
                    return MessageEventType.JobCompleted;     // "Job Finished"
                case 307:
                    return MessageEventType.JobCancelled;     // "Operator abort: job canceled"
                case 338:
                    return MessageEventType.JobCancelled;     // "Job Cancelled"
                default:
                    // Various probing errors (318-337)
                    if (errorCode >= 318 && errorCode <= 337)
                    {
                        return MessageEventType.ProbeError;
                    }
                    // Various cancellation reasons (320-330) - note overlap with probe range
                    if (errorCode >= 320 && errorCode <= 330)
                    {
                        return MessageEventType.SystemFault;
                    }
                    return MessageEventType.StatusMessage;
            }
        }

        /// <summary>
        /// Classify fault messages (400-499 range) into more specific types  
        /// </summary>
        private static MessageEventType ClassifyFaultMessage(int errorCode)
        {
            switch (errorCode)
            {
                case 401:
                    return MessageEventType.SystemFault;      // "PLC failure detected"
                case 404:
                    return MessageEventType.SystemFault;      // "Spindle drive fault detected" 
                case 405:
                    return MessageEventType.SystemFault;      // "Lubricant level low"
                case 406:
                    return MessageEventType.SystemFault;      // "Emergency Stop detected"
                case 407:
                    return MessageEventType.LimitError;       // "limit (#) tripped"
                default:
                    // Various axis faults (409-447)
                    if (errorCode >= 409 && errorCode <= 447)
                    {
                        return MessageEventType.AxisFault;
                    }
                    // Communication errors (452-453)
                    if (errorCode >= 452 && errorCode <= 453)
                    {
                        return MessageEventType.CommunicationError;
                    }
                    // Various system faults (449-460)
                    if (errorCode >= 449 && errorCode <= 460)
                    {
                        return MessageEventType.SystemFault;
                    }
                    return MessageEventType.SystemFault;
            }
        }

        /// <summary>
        /// Fallback classification based on message content when no error code is found
        /// </summary>
        private static MessageEventType ClassifyByContent(string message)
        {
            var lower = message.ToLower();
            
            // Job-related keywords
            if (lower.Contains("job") && (lower.Contains("start") || lower.Contains("begin")))
                return MessageEventType.JobStarted;
            if (lower.Contains("job") && (lower.Contains("finish") || lower.Contains("complete") || lower.Contains("done")))
                return MessageEventType.JobCompleted;  
            if (lower.Contains("job") && (lower.Contains("cancel") || lower.Contains("abort")))
                return MessageEventType.JobCancelled;
                
            // Error keywords
            if (lower.Contains("limit"))
                return MessageEventType.LimitError;
            if (lower.Contains("probe"))
                return MessageEventType.ProbeError;
            if (lower.Contains("axis") && lower.Contains("fault"))
                return MessageEventType.AxisFault;
            if (lower.Contains("syntax") || lower.Contains("invalid"))
                return MessageEventType.SyntaxError;
            if (lower.Contains("parameter") && lower.Contains("error"))
                return MessageEventType.ParameterError;
            if (lower.Contains("compensation"))
                return MessageEventType.CutterCompensationError;
                
            // Generic classifications
            if (lower.Contains("error") || lower.Contains("fault"))
                return MessageEventType.SystemFault;
            if (lower.Contains("config") || lower.Contains("modified"))
                return MessageEventType.ConfigurationChange;
                
            return MessageEventType.StatusMessage; // Default for unclassified messages
        }

        /// <summary>
        /// Process DRO_UPDATE message - Contains position data
        /// Returns true if positions haven't changed (should skip ALL logging)
        /// </summary>
        private static bool ProcessDroUpdateMessage(CNCPipe.InboundComm.CommPacket packet)
        {
            // According to API docs: "Positions - New location of dro"
            var positions = GetPacketProperty<double[]?>(packet, "Positions", null);
            
            // Check if positions are the same as last time
            if (positions != null && _lastDroPositions != null)
            {
                if (positions.Length == _lastDroPositions.Length)
                {
                    bool positionsMatch = true;
                    for (int i = 0; i < positions.Length; i++)
                    {
                        // Use a small tolerance for floating point comparison
                        if (Math.Abs(positions[i] - _lastDroPositions[i]) > 0.000001)
                        {
                            positionsMatch = false;
                            break;
                        }
                    }
                    
                    if (positionsMatch)
                    {
                        _droSamePositionSkipCount++;
                        
                        // Silently skip unchanged positions - no logging at all
                        // Only log milestone counts to track how many we're skipping
                        if (_droSamePositionSkipCount == 1)
                        {
                            LogToFile($"DRO positions unchanged - starting to skip identical position updates...");
                        }
                        else if (_droSamePositionSkipCount % 500 == 0)
                        {
                            LogToFile($"DRO positions unchanged - skipped {_droSamePositionSkipCount} identical updates");
                        }
                        
                        return true; // Skip ALL logging for this message
                    }
                    else
                    {
                        // Positions changed, reset skip counter and log the change
                        if (_droSamePositionSkipCount > 0)
                        {
                            LogToFile($"--- DRO Position CHANGED after {_droSamePositionSkipCount} unchanged updates ---");
                            _droSamePositionSkipCount = 0;
                        }
                    }
                }
            }
            
            // Store current positions for next comparison
            if (positions != null)
            {
                _lastDroPositions = new double[positions.Length];
                Array.Copy(positions, _lastDroPositions, positions.Length);

                // Notify listeners of DRO position change
                var droEvent = new DROEvent
                {
                    Timestamp = DateTime.Now,
                    Axis1 = positions.Length > 0 ? positions[0] : 0.0,
                    Axis2 = positions.Length > 1 ? positions[1] : 0.0,
                    Axis3 = positions.Length > 2 ? positions[2] : 0.0,
                    Axis4 = positions.Length > 3 ? positions[3] : 0.0,
                    Axis5 = positions.Length > 4 ? positions[4] : 0.0,
                    Axis6 = positions.Length > 5 ? positions[5] : 0.0,
                    Axis7 = positions.Length > 6 ? positions[6] : 0.0,
                    Axis8 = positions.Length > 7 ? positions[7] : 0.0,
                    Message = $"DRO positions updated: {string.Join(", ", positions.Select(p => p.ToString("F4")))}"
                };
                
                // Store the DRO event in message history
                StoreMessage(droEvent, "DRO_UPDATE");
                
                // Notify listeners
                NotifyListeners(droEvent);
            }
            
            // This will only be called when positions have actually changed
            // The logging will be handled by the main message processing logic
            
            return false; // Don't skip logging - positions have changed
        }

        /// <summary>
        /// Process shutdown messages (CNC12_SHUT_DOWN or PC_SHUT_DOWN)
        /// </summary>
        private static void ProcessShutdownMessage(CNCPipe.InboundComm.CommPacket packet, string shutdownType)
        {
            LogToFile($"    {shutdownType} Shutdown Event:");
            
            // According to API docs: "Flag - true if shutting down, false otherwise"
            var flag = GetPacketProperty<bool>(packet, "Flag", false);
            LogToFile($"    Shutting Down: {flag}");
            
            // Also try other common flag names
            var isShuttingDown = GetPacketProperty<bool>(packet, "IsShuttingDown", false);
            var shutdown = GetPacketProperty<bool>(packet, "Shutdown", false);
            
            if (isShuttingDown) LogToFile($"    IsShuttingDown: {isShuttingDown}");
            if (shutdown) LogToFile($"    Shutdown: {shutdown}");
        }

        /// <summary>
        /// Process MESSAGE_WINDOW_MESSAGE - Contains message text
        /// </summary>
        private static void ProcessMessageWindowMessage(CNCPipe.InboundComm.CommPacket packet)
        {
            LogToFile($"    Message Window Event:");
            
            // According to API docs: "Message - the added message"
            var message = GetPacketProperty<string>(packet, "Message", "");
            if (!string.IsNullOrWhiteSpace(message))
            {
                LogToFile($"    Message: {message}");
            }
            
            // Try other common message property names
            var text = GetPacketProperty<string>(packet, "Text", "");
            var content = GetPacketProperty<string>(packet, "Content", "");
            
            if (!string.IsNullOrWhiteSpace(text)) LogToFile($"    Text: {text}");
            if (!string.IsNullOrWhiteSpace(content)) LogToFile($"    Content: {content}");

            // Notify listeners of message event
            var finalMessage = message;
            if (string.IsNullOrWhiteSpace(finalMessage)) finalMessage = text;
            if (string.IsNullOrWhiteSpace(finalMessage)) finalMessage = content;
            
            if (!string.IsNullOrWhiteSpace(finalMessage))
            {
                // Extract error code and determine event type based on Centroid documentation
                var (eventCode, eventType) = ClassifyMessage(finalMessage);

                var messageEvent = new MessageEvent
                {
                    Timestamp = DateTime.Now,
                    EventCode = eventCode,
                    Message = finalMessage,
                    EventType = eventType
                };
                
                // Store the message event in history
                StoreMessage(messageEvent, "MESSAGE_WINDOW_MESSAGE");
                
                // Notify listeners
                NotifyListeners(messageEvent);
            }
        }

        /// <summary>
        /// Process JOB_INFO message - Contains job execution details
        /// </summary>
        private static void ProcessJobInfoSpecific(CNCPipe.InboundComm.CommPacket packet)
        {
            LogToFile($"    Job Info Update:");
            
            // According to API docs: "LineNumber - Current executing line number"
            var lineNumber = GetPacketProperty<int>(packet, "LineNumber", 0);
            if (lineNumber > 0)
            {
                LogToFile($"    Line Number: {lineNumber}");
            }
            
            // According to API docs: "StackLevel - Reported stack level"
            var stackLevel = GetPacketProperty<int>(packet, "StackLevel", 0);
            if (stackLevel > 0)
            {
                LogToFile($"    Stack Level: {stackLevel}");
            }
            
            // According to API docs: "Message - The current running job"
            var message = GetPacketProperty<string>(packet, "Message", "");
            if (!string.IsNullOrWhiteSpace(message))
            {
                LogToFile($"    Current Job: {message}");
            }

            // Notify listeners of job info event
            var jobInfoEvent = new JobInfoEvent
            {
                Timestamp = DateTime.Now,
                LineNumber = lineNumber,
                StackLevel = stackLevel,
                Message = message ?? string.Empty,
                JobName = message ?? string.Empty // Use the message as job name for now
            };
            
            // Store the job info event in history
            StoreMessage(jobInfoEvent, "JOB_INFO");
            
            // Notify listeners
            NotifyListeners(jobInfoEvent);
        }

        /// <summary>
        /// Process unknown message types
        /// </summary>
        private static void ProcessUnknownMessage(CNCPipe.InboundComm.CommPacket packet, string commType)
        {
            LogToFile($"    Unknown Communication Type: {commType}");
            LogToFile($"    This message type is not documented in the API reference");
        }



        /// <summary>
        /// Report message count summary to main log
        /// </summary>
        private static void ReportMessageCount()
        {
            var newMessages = _messageCount - _lastReportedCount;
            if (newMessages >= 100)
            {
                var elapsed = DateTime.Now - _sessionStartTime;
                var rate = _messageCount / elapsed.TotalMinutes;
                
                LogInfo($"📊 Job Listener Stats: {_messageCount} total messages ({newMessages} new), {rate:F1}/min", "JobInfo");
                _lastReportedCount = _messageCount;
            }
        }

        /// <summary>
        /// Start listening for JOB_INFO messages from CNC12
        /// </summary>
        /// <returns>True if listener started successfully, false otherwise</returns>
        public static bool StartListening()
        {
            lock (_lock)
            {
                if (_isListening)
                {
                    LogWarning("CNC JOB_INFO listener is already running", "JobInfo");
                    return true;
                }

                try
                {
                    // Check if CNC is connected
                    if (!CNCConnectionManager.IsConnected)
                    {
                        LogWarning("Cannot start JOB_INFO listener: CNC not connected", "JobInfo");
                        return false;
                    }

                    // Get CNC connection - this should work if IsConnected is true
                    var cncPipe = CNCConnectionManager.GetCNCPipe();
                    if (cncPipe == null)
                    {
                        LogWarning("Cannot start JOB_INFO listener: CNCPipe is null despite connection", "JobInfo");
                        return false;
                    }

                    // Configure inbound communications to send JOB_INFO messages  
                    // Note: ChangeJobInfoType may not be available - we'll rely on default settings
                    try
                    {
                        // Try to configure if the method exists - this is optional
                        // var result = cncPipe.inbound_communications.ChangeJobInfoType(...);
                        LogInfo("Using default JOB_INFO configuration", "JobInfo");
                    }
                    catch (Exception ex)
                    {
                        LogWarning($"Could not configure JOB_INFO type: {ex.Message}", "JobInfo");
                        // Continue anyway - default settings should work
                    }

                    // Store reference to current CNC pipe
                    _currentCNCPipe = cncPipe;

                    // Subscribe to MessageReceived event
                    cncPipe.MessageReceived += OnMessageReceived;
                    
                    // Start listening for messages from CNC12
                    cncPipe.StartListening();
                    LogSuccess("Started CNC12 event-driven message listening", "JobInfo");

                    // Initialize file logging for detailed messages
                    InitializeFileLogging();
                    
                    // Reset counters
                    _messageCount = 0;
                    _lastReportedCount = 0;
                    _sessionStartTime = DateTime.Now;

                    _isListening = true;
                    LogSuccess("CNC JOB_INFO listener started successfully", "JobInfo");
                    LogInfo("Detailed messages will be logged to file, summaries every 100 messages", "JobInfo");

                    return true;
                }
                catch (Exception ex)
                {
                    LogError($"Failed to start CNC JOB_INFO listener: {ex.Message}", "JobInfo");
                    return false;
                }
            }
        }

        /// <summary>
        /// Stop listening for JOB_INFO messages
        /// </summary>
        public static void StopListening()
        {
            lock (_lock)
            {
                if (!_isListening)
                {
                    LogInfo("CNC JOB_INFO listener is not running", "JobInfo");
                    return;
                }

                try
                {
                    // Unsubscribe from MessageReceived event
                    if (_currentCNCPipe != null)
                    {
                        _currentCNCPipe.MessageReceived -= OnMessageReceived;
                        _currentCNCPipe.StopListening();
                        LogSuccess("Stopped CNC12 event-driven message listening", "JobInfo");
                        _currentCNCPipe = null;
                    }

                    _isListening = false;
                    LogSuccess("CNC JOB_INFO listener stopped", "JobInfo");
                }
                catch (Exception ex)
                {
                    LogError($"Error stopping CNC JOB_INFO listener: {ex.Message}", "JobInfo");
                    _isListening = false; // Force stop even if there was an error
                }
                finally
                {
                    // Clean up resources
                    _currentCNCPipe = null;
                    
                    // Close file logging
                    if (_logWriter != null)
                    {
                        _logWriter.WriteLine($"=== Job Listener Session Ended: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                        _logWriter.WriteLine($"Total messages processed: {_messageCount}");
                        _logWriter.WriteLine("");
                        _logWriter.Close();
                        _logWriter = null;
                    }
                    
                    // Clear duplicate tracking
                    _lastMessageHashes.Clear();
                    _duplicateCounters.Clear();
                    
                    // Clear DRO position tracking
                    _lastDroPositions = null;
                    _droSamePositionSkipCount = 0;
                    
                    // Clear stored messages
                    lock (_storedMessagesLock)
                    {
                        _storedMessages.Clear();
                        LogInfo("Cleared stored messages on listener stop", "JobInfo");
                    }
                }
            }
        }

        static string _lastPacketHash = "";
        static int _sameObjectSkipCount = 0;
        
        /// <summary>
        /// Event handler for CNC MessageReceived events
        /// </summary>
        private static void OnMessageReceived(object? sender, CentroidAPI.MessageReceivedEventArgs e)
        {
            try
            {
                // Access the communication packet from event args
                var packet = e.Data;
                
                // Check if this packet is identical to the last one (since CommPacket is a struct)
                var packetHash = CalculatePacketHash(packet);
                if (_lastPacketHash == packetHash && !string.IsNullOrEmpty(_lastPacketHash))
                {
                    _sameObjectSkipCount++;
                    return; // Skip identical packets
                }

                _lastPacketHash = packetHash;
                _messageCount++;

                try
                {
                    // Process message with duplicate detection first
                    var commType = packet.CommunicationType.ToString();
                    bool isDuplicate = ProcessMessageWithDuplicateDetection(commType, packet);

                    // Report count summary to main log every 100 messages
                    ReportMessageCount();
                }
                catch (Exception msgEx)
                {
                    LogToFile($"ERROR processing message #{_messageCount}: {msgEx.Message}");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error in CNC MessageReceived handler: {ex.Message}", "JobInfo");
            }
        }

        /// <summary>
        /// Process a CNC message with duplicate detection - returns true if duplicate
        /// </summary>
        private static bool ProcessMessageWithDuplicateDetection(string commType, CNCPipe.InboundComm.CommPacket packet)
        {
            try
            {
                // Calculate hash of all packet data for duplicate detection
                var packetHash = CalculatePacketHash(packet);
                
                // Special handling for DRO_UPDATE - check positions first before any logging
                if (commType == "DRO_UPDATE")
                {
                    bool shouldSkipDroLogging = ProcessDroUpdateMessage(packet);
                    if (shouldSkipDroLogging)
                    {
                        return false; // Skip all logging for unchanged DRO positions
                    }
                }
                
                // Check if this is a duplicate message
                if (_lastMessageHashes.ContainsKey(commType))
                {
                    if (_lastMessageHashes[commType] == packetHash)
                    {
                        _duplicateCounters[commType] = _duplicateCounters.GetValueOrDefault(commType, 0) + 1;
                        
                        // Log only the duplicate notification - single line format
                        LogToFile($"Message #{_messageCount} {commType} (duplicate #{_duplicateCounters[commType]})");
                        return true; // Skip all other logging for duplicates
                    }
                    else
                    {
                        // Reset duplicate counter when we get a different message
                        if (_duplicateCounters.ContainsKey(commType) && _duplicateCounters[commType] > 0)
                        {
                            LogToFile($"--- End of {_duplicateCounters[commType]} duplicate messages ---");
                            _duplicateCounters[commType] = 0;
                        }
                    }
                }
                
                // Store this message hash as the latest for this type
                _lastMessageHashes[commType] = packetHash;
                
                // Log full details for non-duplicate messages
                LogToFile($"Message #{_messageCount}: {commType}");
                LogToFile($"  === {commType} Message #{_messageCount} ===");
                
                // Handle each communication type according to API documentation
                bool shouldSkipRestOfLogging = false;
                switch (commType)
                {
                    case "DRO_UPDATE":
                        // Already processed above, now log the position details since we got here (positions changed)
                        var positions = GetPacketProperty<double[]?>(packet, "Positions", null);
                        LogToFile($"    DRO Position Update:");
                        
                        if (positions != null)
                        {
                            LogToFile($"    Positions: {ExpandValue(positions)}");
                        }
                        
                        // Try common position property names
                        var xPos = GetPacketProperty<double>(packet, "X", double.NaN);
                        var yPos = GetPacketProperty<double>(packet, "Y", double.NaN);
                        var zPos = GetPacketProperty<double>(packet, "Z", double.NaN);
                        
                        if (!double.IsNaN(xPos)) LogToFile($"    X: {xPos}");
                        if (!double.IsNaN(yPos)) LogToFile($"    Y: {yPos}");
                        if (!double.IsNaN(zPos)) LogToFile($"    Z: {zPos}");
                        break;
                        
                    case "CNC12_SHUT_DOWN":
                        ProcessShutdownMessage(packet, "CNC12");
                        break;
                        
                    case "PC_SHUT_DOWN":
                        ProcessShutdownMessage(packet, "PC");
                        break;
                        
                    case "MESSAGE_WINDOW_MESSAGE":
                        ProcessMessageWindowMessage(packet);
                        break;
                        
                    case "JOB_INFO":
                        ProcessJobInfoSpecific(packet);
                        break;
                        
                    default:
                        ProcessUnknownMessage(packet, commType);
                        break;
                }
                
                // Skip remaining logging if positions haven't changed for DRO messages
                if (shouldSkipRestOfLogging)
                {
                    return false; // Not a duplicate, but skip detailed logging
                }
                
                // Extract and log basic data for job info events
                var jobInfo = new JobInfoData
                {
                    Timestamp = DateTime.Now,
                    LineNumber = GetPacketProperty<int>(packet, "LineNumber", 0),
                    StackLevel = GetPacketProperty<int>(packet, "StackLevel", 0),
                    Message = GetPacketProperty<string>(packet, "Message", "") ?? "",
                    CommunicationType = commType
                };

                // Log summary info
                if (jobInfo.LineNumber > 0 && jobInfo.LineNumber < 1000000)
                    LogToFile($"  Line: {jobInfo.LineNumber}");
                    
                if (jobInfo.StackLevel > 0 && jobInfo.StackLevel < 1000)
                    LogToFile($"  Stack: {jobInfo.StackLevel}");
                    
                if (!string.IsNullOrWhiteSpace(jobInfo.Message))
                    LogToFile($"  Message: {jobInfo.Message}");

                // Always log all properties for debugging (only for non-duplicates)
                // LogToFile($"  --- All Properties/Fields ---");
                // LogAllPacketPropertiesToFile(packet);
                LogToFile($""); // Empty line for readability

                // Fire the event for any subscribers
                JobInfoReceived?.Invoke(jobInfo);
                
                return false; // Not a duplicate
            }
            catch (Exception ex)
            {
                LogToFile($"ERROR processing {commType} message: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Simplified processing for important messages that should go to main log
        /// Only logs significant job-related messages to avoid spam
        /// </summary>
        private static void ProcessJobInfoMessage(CNCPipe.InboundComm.CommPacket packet)
        {
            try
            {
                var commType = packet.CommunicationType.ToString();
                
                // Only log important job-related message types to main log
                if (commType.Contains("JOB") || commType.Contains("PROGRAM") || 
                    commType.Contains("LINE") || commType.Contains("ERROR") ||
                    commType.Contains("DRO") || commType.Contains("POSITION"))
                {
                    var jobInfo = new JobInfoData
                    {
                        Timestamp = DateTime.Now,
                        LineNumber = GetPacketProperty<int>(packet, "LineNumber", 0),
                        StackLevel = GetPacketProperty<int>(packet, "StackLevel", 0),
                        Message = GetPacketProperty<string>(packet, "Message", "") ?? "",
                        CommunicationType = commType
                    };

                    // Log only important notifications to main log
                    LogInfo($"🔧 IMPORTANT: {commType}", "JobInfo");
                    
                    if (jobInfo.LineNumber > 0)
                        LogInfo($"  Line: {jobInfo.LineNumber}", "JobInfo");
                        
                    if (!string.IsNullOrWhiteSpace(jobInfo.Message))
                        LogInfo($"  Message: {jobInfo.Message}", "JobInfo");

                    // Fire the event for any subscribers
                    JobInfoReceived?.Invoke(jobInfo);
                }
                // All other messages are only logged to file (handled by ProcessJobInfoMessageToFile)
            }
            catch (Exception ex)
            {
                LogError($"Error processing important CNC message: {ex.Message}", "JobInfo");
            }
        }

        /// <summary>
        /// Log all available properties from a CommPacket to file
        /// </summary>
        private static void LogAllPacketPropertiesToFile(CNCPipe.InboundComm.CommPacket packet)
        {
            try
            {
                var packetType = packet.GetType();
                LogToFile($"    Packet Type: {packetType.FullName}");
                
                // Get all properties
                var properties = packetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                LogToFile($"    Found {properties.Length} properties");
                
                if (properties.Length == 0)
                {
                    LogToFile($"    No properties found - trying fields instead");
                    var fields = packetType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                    LogToFile($"    Found {fields.Length} fields");
                    
                    foreach (var field in fields)
                    {
                        try
                        {
                            var value = field.GetValue(packet);
                            var expandedValue = ExpandValue(value);
                            LogToFile($"    Field {field.Name}: {expandedValue}");
                        }
                        catch (Exception fieldEx)
                        {
                            LogToFile($"    Field {field.Name}: <error: {fieldEx.Message}>");
                        }
                    }
                }
                else
                {
                    foreach (var prop in properties)
                    {
                        try
                        {
                            if (prop.CanRead)
                            {
                                var value = prop.GetValue(packet);
                                var expandedValue = ExpandValue(value);
                                LogToFile($"    Prop {prop.Name}: {expandedValue} (Type: {prop.PropertyType.Name})");
                            }
                            else
                            {
                                LogToFile($"    Prop {prop.Name}: <not readable>");
                            }
                        }
                        catch (Exception propEx)
                        {
                            LogToFile($"    Prop {prop.Name}: <error: {propEx.Message}>");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogToFile($"  ERROR logging packet properties: {ex.Message}");
            }
        }

        /// <summary>
        /// Log all available properties from a CommPacket for debugging (legacy method for main log)
        /// </summary>
        private static void LogAllPacketProperties(CNCPipe.InboundComm.CommPacket packet)
        {
            try
            {
                var packetType = packet.GetType();
                
                // Get all properties
                var properties = packetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in properties)
                {
                    try
                    {
                        if (prop.CanRead)
                        {
                            var value = prop.GetValue(packet);
                            if (value != null)
                            {
                                LogInfo($"  🔹 {prop.Name}: {value}", "JobInfo");
                            }
                        }
                    }
                    catch (Exception propEx)
                    {
                        LogDebug($"  ⚠️ {prop.Name}: <error reading: {propEx.Message}>", "JobInfo");
                    }
                }
                
                // Get all fields
                var fields = packetType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    try
                    {
                        var value = field.GetValue(packet);
                        if (value != null)
                        {
                            LogInfo($"  🔸 {field.Name}: {value}", "JobInfo");
                        }
                    }
                    catch (Exception fieldEx)
                    {
                        LogDebug($"  ⚠️ {field.Name}: <error reading: {fieldEx.Message}>", "JobInfo");
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Error logging packet properties: {ex.Message}", "JobInfo");
            }
        }

        /// <summary>
        /// Calculate a hash of all packet properties and values for duplicate detection
        /// </summary>
        private static string CalculatePacketHash(CNCPipe.InboundComm.CommPacket packet)
        {
            try
            {
                var hashBuilder = new System.Text.StringBuilder();
                var packetType = packet.GetType();
                
                // Add packet type to hash
                hashBuilder.Append(packetType.FullName);
                hashBuilder.Append("|");
                
                // Get all properties and their values
                var properties = packetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in properties.OrderBy(p => p.Name)) // Sort for consistent ordering
                {
                    try
                    {
                        if (prop.CanRead)
                        {
                            var value = prop.GetValue(packet);
                            hashBuilder.Append($"{prop.Name}:");
                            
                            if (value == null)
                            {
                                hashBuilder.Append("NULL");
                            }
                            else if (value.GetType().IsArray)
                            {
                                var array = (Array)value;
                                hashBuilder.Append("[");
                                for (int i = 0; i < array.Length; i++)
                                {
                                    if (i > 0) hashBuilder.Append(",");
                                    var element = array.GetValue(i);
                                    hashBuilder.Append(element?.ToString() ?? "NULL");
                                }
                                hashBuilder.Append("]");
                            }
                            else if (value is System.Collections.IEnumerable enumerable && !(value is string))
                            {
                                hashBuilder.Append("[");
                                bool first = true;
                                foreach (var item in enumerable)
                                {
                                    if (!first) hashBuilder.Append(",");
                                    hashBuilder.Append(item?.ToString() ?? "NULL");
                                    first = false;
                                }
                                hashBuilder.Append("]");
                            }
                            else
                            {
                                hashBuilder.Append(value.ToString());
                            }
                            hashBuilder.Append("|");
                        }
                    }
                    catch (Exception)
                    {
                        // Skip properties that can't be read
                        hashBuilder.Append($"{prop.Name}:ERROR|");
                    }
                }
                
                // Also include fields
                var fields = packetType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (var field in fields.OrderBy(f => f.Name))
                {
                    try
                    {
                        var value = field.GetValue(packet);
                        hashBuilder.Append($"{field.Name}:");
                        
                        if (value == null)
                        {
                            hashBuilder.Append("NULL");
                        }
                        else if (value.GetType().IsArray)
                        {
                            var array = (Array)value;
                            hashBuilder.Append("[");
                            for (int i = 0; i < array.Length; i++)
                            {
                                if (i > 0) hashBuilder.Append(",");
                                var element = array.GetValue(i);
                                hashBuilder.Append(element?.ToString() ?? "NULL");
                            }
                            hashBuilder.Append("]");
                        }
                        else if (value is System.Collections.IEnumerable enumerable && !(value is string))
                        {
                            hashBuilder.Append("[");
                            bool first = true;
                            foreach (var item in enumerable)
                            {
                                if (!first) hashBuilder.Append(",");
                                hashBuilder.Append(item?.ToString() ?? "NULL");
                                first = false;
                            }
                            hashBuilder.Append("]");
                        }
                        else
                        {
                            hashBuilder.Append(value.ToString());
                        }
                        hashBuilder.Append("|");
                    }
                    catch (Exception)
                    {
                        // Skip fields that can't be read
                        hashBuilder.Append($"{field.Name}:ERROR|");
                    }
                }
                
                return hashBuilder.ToString();
            }
            catch (Exception ex)
            {
                return $"HASH_ERROR:{ex.Message}";
            }
        }

        /// <summary>
        /// Expand a value for logging, showing array contents and nested objects
        /// </summary>
        private static string ExpandValue(object? value)
        {
            try
            {
                if (value == null)
                    return "<null>";
                
                var type = value.GetType();
                
                // Handle arrays
                if (type.IsArray)
                {
                    var array = (Array)value;
                    if (array.Length == 0)
                        return "[] (empty array)";
                    
                    var elementType = type.GetElementType()?.Name ?? "?";
                    var items = new List<string>();
                    
                    // Limit to first 20 elements to avoid excessive output
                    var maxElements = Math.Min(array.Length, 20);
                    for (int i = 0; i < maxElements; i++)
                    {
                        var element = array.GetValue(i);
                        items.Add(element?.ToString() ?? "<null>");
                    }
                    
                    var result = $"[{string.Join(", ", items)}]";
                    if (array.Length > maxElements)
                        result += $" ... ({array.Length - maxElements} more)";
                    
                    return $"{result} ({elementType}[{array.Length}])";
                }
                
                // Handle collections (List, IEnumerable, etc.)
                if (value is System.Collections.IEnumerable enumerable && !(value is string))
                {
                    var items = new List<string>();
                    int count = 0;
                    int maxItems = 20;
                    
                    foreach (var item in enumerable)
                    {
                        if (count >= maxItems)
                        {
                            items.Add("...");
                            break;
                        }
                        items.Add(item?.ToString() ?? "<null>");
                        count++;
                    }
                    
                    return $"[{string.Join(", ", items)}] (Collection with {count}{(count >= maxItems ? "+" : "")} items)";
                }
                
                // For simple types, just return the string representation
                return value.ToString() ?? "<null>";
            }
            catch (Exception ex)
            {
                return $"<error expanding value: {ex.Message}>";
            }
        }

        /// <summary>
        /// Safely get a property value from the CommPacket using reflection
        /// </summary>
        private static T GetPacketProperty<T>(CNCPipe.InboundComm.CommPacket packet, string propertyName, T defaultValue)
        {
            try
            {
                var packetType = packet.GetType();
                
                // Try property first
                var property = packetType.GetProperty(propertyName);
                if (property != null && property.CanRead)
                {
                    var value = property.GetValue(packet);
                    if (value is T typedValue)
                        return typedValue;
                    if (value != null && typeof(T).IsAssignableFrom(value.GetType()))
                        return (T)value;
                }
                
                // Try field if property doesn't exist
                var field = packetType.GetField(propertyName);
                if (field != null)
                {
                    var value = field.GetValue(packet);
                    if (value is T typedValue)
                        return typedValue;
                    if (value != null && typeof(T).IsAssignableFrom(value.GetType()))
                        return (T)value;
                }
                
                return defaultValue;
            }
            catch (Exception ex)
            {
                LogDebug($"Could not get property {propertyName}: {ex.Message}", "JobInfo");
                return defaultValue;
            }
        }

        /// <summary>
        /// Toggle the listener on/off
        /// </summary>
        /// <returns>True if now listening, false if stopped</returns>
        public static bool ToggleListening()
        {
            if (IsListening)
            {
                StopListening();
                return false;
            }
            else
            {
                return StartListening();
            }
        }

        /// <summary>
        /// Start listening automatically if CNC is connected and listener is not already running
        /// </summary>
        public static void AutoStartIfConnected()
        {
            try
            {
                LogInfo($"🔍 Checking auto-start conditions...", "JobInfo");
                LogInfo($"  - Listener running: {IsListening}", "JobInfo");
                LogInfo($"  - CNC connected: {CNCConnectionManager.IsConnected}", "JobInfo");
                
                if (!IsListening && CNCConnectionManager.IsConnected)
                {
                    LogInfo("✅ Auto-starting JOB_INFO listener for CNC connection", "JobInfo");
                    var result = StartListening();
                    LogInfo($"✅ Auto-start result: {result}", "JobInfo");
                }
                else if (IsListening)
                {
                    LogInfo("ℹ️ Listener already running, no action needed", "JobInfo");
                }
                else if (!CNCConnectionManager.IsConnected)
                {
                    LogInfo("⚠️ CNC not connected, cannot start listener", "JobInfo");
                }
            }
            catch (Exception ex)
            {
                LogError($"❌ Error auto-starting JOB_INFO listener: {ex.Message}", "JobInfo");
            }
        }
    }

    /// <summary>
    /// Data structure containing JOB_INFO message details
    /// </summary>
    public class JobInfoData
    {
        /// <summary>
        /// Timestamp when the message was received
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Current executing line number
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Reported stack level
        /// </summary>
        public int StackLevel { get; set; }

        /// <summary>
        /// The current running job/program name
        /// </summary>
        public string Message { get; set; } = "";

        /// <summary>
        /// Type of communication message
        /// </summary>
        public string CommunicationType { get; set; } = "";

        /// <summary>
        /// String representation of the job info
        /// </summary>
        public override string ToString()
        {
            return $"[{Timestamp:HH:mm:ss.fff}] Line {LineNumber}, Stack {StackLevel}: {Message}";
        }
    }
}