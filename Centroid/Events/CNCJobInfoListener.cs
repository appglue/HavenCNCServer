using CentroidAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HavenCNCServer.Services;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Centroid.Events
{
    /// <summary>
    /// Service for listening to CNC JOB_INFO messages and outputting them to debug logging
    /// </summary>
    public static class CNCJobInfoListener
    {
        private static bool _isListening = false;
        private static readonly object _lock = new object();
        private static CNCPipe? _currentCNCPipe = null;
        private static bool _isMonitoringStarted = false;
        private static Thread? _monitoringThread = null;
        private static CancellationTokenSource? _cancellationTokenSource = null;
        private static bool _isShuttingDown = false; // Add shutdown flag
        private static bool _hasLoggedDisconnectedState = false; // Track if we've logged disconnection

        // File logging for detailed listener data
        private static StreamWriter? _logWriter;
        private static int _messageCount = 0;
        private static int _lastReportedCount = 0;
        private static DateTime _sessionStartTime = DateTime.Now;

        // Duplicate message detection
        private static Dictionary<string, string> _lastMessageHashes = new Dictionary<string, string>();
        private static Dictionary<string, int> _duplicateCounters = new Dictionary<string, int>();

        // Event listener management
        private static readonly List<ICNCEventListener> _eventListeners = new List<ICNCEventListener>();
        private static readonly object _listenersLock = new object();

        // Message storage for recent messages (most recent first)
        private static readonly List<StoredMessage> _storedMessages = new List<StoredMessage>();
        private static readonly object _storedMessagesLock = new object();
        private static readonly int MaxStoredMessages = 1000;

        /// <summary>
        /// Start the CNC job listener with automatic connection monitoring
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        public static void Start(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                // Check if shutdown is in progress
                if (_isShuttingDown)
                {
                    LogWarning("Cannot start CNC job listener - shutdown in progress", "JobInfo");
                    return;
                }

                // Check if monitoring is already started
                if (_isMonitoringStarted)
                {
                    LogWarning("CNC job listener monitoring is already running", "JobInfo");
                    return;
                }

                // Mark monitoring as started
                _isMonitoringStarted = true;
                // Use the provided cancellation token or create a linked one
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            }

            // Start monitoring thread
            _monitoringThread = new Thread(() =>
            {
                try
                {
                    LogInfo("Starting background job listener monitoring...", "JobInfo");

                    // Continue monitoring and retry if connection is lost
                    while (_isMonitoringStarted && !_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        try
                        {
                            // Check for cancellation
                            _cancellationTokenSource.Token.ThrowIfCancellationRequested();

                            // Check if we need to establish or re-establish connection
                            if (!CNCConnectionManager.IsConnected || !IsListening)
                            {
                                if (!CNCConnectionManager.IsConnected)
                                {
                                    LogInfo("CNC not connected - attempting to establish connection...", "JobInfo");

                                    try
                                    {
                                        var pipe = CNCConnectionManager.GetOrCreateCNCPipe();
                                        if (pipe != null)
                                        {
                                            // IsConstructed check is handled internally by CNCConnectionManager
                                            LogSuccess("CNC pipe reconnected", "JobInfo");
                                        }
                                    }
                                    catch (Exception connEx)
                                    {
                                        LogWarning($"CNC reconnection failed: {connEx.Message}", "JobInfo");
                                    }
                                }

                                // Try to start job listener if connected but not running
                                if (CNCConnectionManager.IsConnected && !IsListening)
                                {
                                    LogInfo("Starting job listener...", "JobInfo");
                                    StartListening();
                                }
                            }

                            // Check every 1 second with cancellation checks for faster shutdown response
                            try
                            {
                                // Use much shorter interval for more responsive cancellation (200ms chunks)
                                for (int i = 0; i < 25; i++) // 25 x 200ms = 5 seconds total
                                {
                                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                                        break;

                                    if (_cancellationTokenSource.Token.WaitHandle.WaitOne(200))
                                    {
                                        // Cancellation requested
                                        break;
                                    }
                                }
                            }
                            catch (ObjectDisposedException)
                            {
                                // Token source was disposed, exit gracefully
                                break;
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            LogInfo("Monitoring thread was cancelled", "JobInfo");
                            break;
                        }
                        catch (ThreadAbortException)
                        {
                            break;
                        }
                        catch (ThreadInterruptedException)
                        {
                            LogInfo("Monitoring thread interrupted", "JobInfo");
                            break;
                        }
                        catch (Exception ex)
                        {
                            LogError($"Error in job listener monitoring: {ex.Message}", "JobInfo");
                            Thread.Sleep(5000); // Wait a bit on error
                        }
                    }

                    LogInfo("Job listener monitoring stopped", "JobInfo");
                }
                catch (Exception ex)
                {
                    LogError($"❌ Error in job listener monitoring thread: {ex.Message}", "JobInfo");
                }
                finally
                {
                    // Reset monitoring state when thread exits
                    lock (_lock)
                    {
                        _isMonitoringStarted = false;
                        _monitoringThread = null;

                        // Dispose cancellation token source
                        _cancellationTokenSource?.Dispose();
                        _cancellationTokenSource = null;
                    }
                }
            })
            {
                IsBackground = true,
                Name = "CNCJobInfoListener-Monitor"
            };

            _monitoringThread.Start();
        }

        /// <summary>
        /// Start listening for JOB_INFO messages from CNC12
        /// </summary>
        /// <returns>True if listener started successfully, false otherwise</returns>
        public static bool StartListening()
        {
            lock (_lock)
            {
                // Check if shutdown is in progress
                if (_isShuttingDown)
                {
                    LogWarning("Cannot start JOB_INFO listener - shutdown in progress", "JobInfo");
                    return false;
                }

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

                    // Configure DRO to send MACHINE coordinates (not work coordinates)
                    try
                    {
                        var droResult = cncPipe.inbound_communications.ChangeDroType(CentroidAPI.CNCPipe.Dro.DroCoordinates.DRO_MACHINE);
                        if (droResult == CNCPipe.ReturnCode.SUCCESS)
                        {
                            LogInfo("✓ DRO events configured to send MACHINE coordinates", "JobInfo");
                        }
                        else
                        {
                            LogWarning($"Failed to configure DRO coordinate type: {droResult}", "JobInfo");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogWarning($"Could not configure DRO coordinate type: {ex.Message}", "JobInfo");
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
                    LogToFile("Detailed messages will be logged to file, summaries every 100 messages");

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
        /// Stop the CNC job listener monitoring and message listening
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        public static void Stop(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                // Set shutdown flag immediately to prevent any new operations
                _isShuttingDown = true;

                // Early exit if nothing is running
                if (!_isMonitoringStarted && _monitoringThread == null)
                {
                    LogInfo("CNC JOB_INFO listener is not running", "JobInfo");
                    StopListeningInternal();
                    return;
                }

                // Stop the monitoring thread if it's running
                if (_isMonitoringStarted && _monitoringThread != null)
                {
                    // Mark monitoring as stopped
                    _isMonitoringStarted = false;

                    // Cancel the monitoring operations
                    _cancellationTokenSource?.Cancel();

                    try
                    {
                        // For immediate shutdown - don't wait at all if CNC isn't connected
                        if (!CNCConnectionManager.IsConnected)
                        {
                            _monitoringThread = null;
                        }
                        else if (_monitoringThread.IsAlive)
                        {
                            // Even if connected, use very short timeout (25ms)
                            if (!_monitoringThread.Join(25))
                            {
                                // Don't interrupt, just abandon
                                _monitoringThread = null;
                            }
                            else
                            {
                                _monitoringThread = null;
                            }
                        }
                        else
                        {
                            _monitoringThread = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error stopping monitoring thread: {ex.Message}", "JobInfo");
                        _monitoringThread = null;
                    }
                    finally
                    {
                        // Dispose cancellation token source
                        _cancellationTokenSource?.Dispose();
                        _cancellationTokenSource = null;
                    }
                }

                // Also stop the message listener - call the internal method to avoid double locking
                StopListeningInternal();
            }
        }

        /// <summary>
        /// Stop listening for JOB_INFO messages
        /// </summary>
        public static void StopListening()
        {
            lock (_lock)
            {
                StopListeningInternal();
            }
        }

        /// <summary>
        /// Internal method to stop listening - assumes lock is already held
        /// </summary>
        private static void StopListeningInternal()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            if (!_isListening)
            {
                LogInfo("CNC JOB_INFO listener is not running", "JobInfo");
                return;
            }

            try
            {
                LogInfo("🔌 Stopping CNC pipe message listening...", "JobInfo");

                // Unsubscribe from MessageReceived event
                if (_currentCNCPipe != null)
                {
                    var unsubStopwatch = System.Diagnostics.Stopwatch.StartNew();
                    _currentCNCPipe.MessageReceived -= OnMessageReceived;
                    LogInfo($"📤 Event unsubscribed ({unsubStopwatch.ElapsedMilliseconds}ms)", "JobInfo");

                    var stopStopwatch = System.Diagnostics.Stopwatch.StartNew();
                    _currentCNCPipe.StopListening();
                    LogInfo($"⏹️ CNCPipe.StopListening() completed ({stopStopwatch.ElapsedMilliseconds}ms)", "JobInfo");

                    LogSuccess($"Stopped CNC12 event-driven message listening (total: {stopwatch.ElapsedMilliseconds}ms)", "JobInfo");
                    _currentCNCPipe = null;
                }

                _isListening = false;
                LogSuccess($"CNC JOB_INFO listener stopped ({stopwatch.ElapsedMilliseconds}ms)", "JobInfo");
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

                // Reset event-specific tracking
                DROEvent.ResetTracking();
                JobInfoEvent.ResetTracking();

                // Clear stored messages
                lock (_storedMessagesLock)
                {
                    _storedMessages.Clear();
                    LogToFile("Cleared stored messages on listener stop");
                }
            }
        }


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
                MessageEventType.StartupError or
                MessageEventType.MiscellaneousError => true,  // Includes travel exceeded (907) and other serious errors

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
        /// Gets whether the monitoring thread is currently running
        /// </summary>
        public static bool IsMonitoringStarted
        {
            get
            {
                lock (_lock)
                {
                    return _isMonitoringStarted;
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
        /// Push a custom event to all registered listeners
        /// </summary>
        /// <param name="customEvent">The custom event to push to listeners</param>
        public static void PushCustomEvent(ICentroidEvent customEvent)
        {
            if (customEvent == null)
            {
                LogWarning("Attempted to push null custom event", "JobInfo");
                return;
            }

            try
            {
                // Set timestamp if not already set
                if (customEvent.Timestamp == default)
                {
                    customEvent.Timestamp = DateTime.Now;
                }

                // Notify all listeners
                NotifyListeners(customEvent);

                // Store the event in message history
                StoreMessage(customEvent, "CustomEvent");

                LogDebug($"Pushed custom event: {customEvent.GetType().Name} - {customEvent.Message}", "JobInfo");
            }
            catch (Exception ex)
            {
                LogError($"Error pushing custom event {customEvent.GetType().Name}: {ex.Message}", "JobInfo");
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
                LogToFile($"Cleared {count} stored messages");
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

            // If CNC12 is shutting down, reset connection state and trigger reconnection
            if (flag || isShuttingDown || shutdown)
            {
                LogWarning($"🔴 {shutdownType} is shutting down - resetting connection state", "CNCJobInfo");

                // Disconnect - this will automatically trigger SignalR notification via ConnectionStatusChanged event
                CNCConnectionManager.Disconnect();

                // Attempt to reconnect after a delay
                _ = Task.Run(async () =>
                {
                    await Task.Delay(2000); // Wait 2 seconds before attempting reconnection
                    LogInfo($"🔄 Attempting to reconnect after {shutdownType} shutdown...", "CNCJobInfo");

                    try
                    {
                        var connected = await CNCConnectionManager.ConnectAsync();
                        if (connected)
                        {
                            LogSuccess($"✅ Reconnected to CNC after {shutdownType} shutdown", "CNCJobInfo");
                        }
                        else
                        {
                            LogWarning($"⚠️ Failed to reconnect after {shutdownType} shutdown - will retry on next API call", "CNCJobInfo");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error during reconnection attempt: {ex.Message}", "CNCJobInfo");
                    }
                });
            }
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

                LogToFile($"📊 Job Listener Stats: {_messageCount} total messages ({newMessages} new), {rate:F1}/min");

                _lastReportedCount = _messageCount;
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
                // CRITICAL: Guard against callbacks during shutdown
                // Check shutdown flag first to prevent processing events on disposed COM objects
                if (_isShuttingDown)
                {
                    return; // Silently ignore all events during shutdown
                }

                // Early exit: Ignore all messages when CNC12 is not connected to prevent event flood
                if (!CNCConnectionManager.IsConnected)
                {
                    // Log only once when entering disconnected state
                    if (!_hasLoggedDisconnectedState)
                    {
                        LogWarning($"⚠️ CNC12 disconnected - Ignoring all incoming messages to prevent event flood", "JobInfo");
                        _hasLoggedDisconnectedState = true;
                    }
                    return; // Skip all message processing when disconnected
                }

                // Reset the disconnected state flag when we receive messages while connected
                if (_hasLoggedDisconnectedState)
                {
                    LogSuccess($"✅ CNC12 reconnected - Resuming message processing", "JobInfo");
                    _hasLoggedDisconnectedState = false;
                }

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
                        // Use DROEvent's processing method
                        var (shouldSkip, droEvent) = DROEvent.ProcessMessage(packet, LogToFile, StoreMessage, NotifyListeners);
                        if (shouldSkip)
                        {
                            return false; // Skip all logging for unchanged DRO positions
                        }

                        // Log position details since positions changed
                        if (droEvent != null)
                        {
                            LogToFile($"    DRO Position Update:");
                            LogToFile($"    Positions: X:{droEvent.Axis1:F4}, Y:{droEvent.Axis2:F4}, Z:{droEvent.Axis3:F4}");

                            // Also log to main UI with coordinates
                            LogInfo($"📍 DRO: X:{droEvent.Axis1:F4} Y:{droEvent.Axis2:F4} Z:{droEvent.Axis3:F4}", "JobInfo");
                        }
                        break;

                    case "CNC12_SHUT_DOWN":
                        ProcessShutdownMessage(packet, "CNC12");
                        break;

                    case "PC_SHUT_DOWN":
                        ProcessShutdownMessage(packet, "PC");
                        break;

                    case "MESSAGE_WINDOW_MESSAGE":
                        // Use MessageEvent's processing method
                        MessageEvent.ProcessMessage(packet, LogToFile, StoreMessage, NotifyListeners);
                        break;

                    case "JOB_INFO":
                        // Use JobInfoEvent's processing method
                        JobInfoEvent.ProcessMessage(packet, LogToFile, StoreMessage, NotifyListeners);
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
                LogToFile($"  --- All Properties/Fields ---");
                LogAllPacketPropertiesToFile(packet);
                LogToFile($""); // Empty line for readability

                // Fire the event for any subscribers
                try
                {
                    JobInfoReceived?.Invoke(jobInfo);
                }
                catch (Exception eventEx)
                {
                    LogError($"Error in JobInfoReceived event handler: {eventEx.Message}", "JobInfo");
                }

                return false; // Not a duplicate
            }
            catch (Exception ex)
            {
                LogToFile($"ERROR processing {commType} message: {ex.Message}");
                return false;
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
        /// Log all properties and fields of a CommPacket to file
        /// </summary>
        private static void LogAllPacketPropertiesToFile(CNCPipe.InboundComm.CommPacket packet)
        {
            try
            {
                var packetType = packet.GetType();

                // Log all properties
                var properties = packetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in properties.OrderBy(p => p.Name))
                {
                    try
                    {
                        if (prop.CanRead)
                        {
                            var value = prop.GetValue(packet);
                            var valueStr = FormatPacketValue(value);
                            LogToFile($"    {prop.Name} ({prop.PropertyType.Name}): {valueStr}");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogToFile($"    {prop.Name}: ERROR - {ex.Message}");
                    }
                }

                // Log all fields
                var fields = packetType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (var field in fields.OrderBy(f => f.Name))
                {
                    try
                    {
                        var value = field.GetValue(packet);
                        var valueStr = FormatPacketValue(value);
                        LogToFile($"    {field.Name} ({field.FieldType.Name}): {valueStr}");
                    }
                    catch (Exception ex)
                    {
                        LogToFile($"    {field.Name}: ERROR - {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogToFile($"  ERROR logging packet properties: {ex.Message}");
            }
        }

        /// <summary>
        /// Format a packet value for logging
        /// </summary>
        private static string FormatPacketValue(object? value)
        {
            if (value == null)
                return "NULL";

            if (value.GetType().IsArray)
            {
                var array = (Array)value;
                var items = new List<string>();
                for (int i = 0; i < Math.Min(array.Length, 20); i++) // Limit to first 20 items
                {
                    var element = array.GetValue(i);
                    items.Add(element?.ToString() ?? "NULL");
                }
                var result = $"[{string.Join(", ", items)}]";
                if (array.Length > 20)
                    result += $" ... ({array.Length} items total)";
                return result;
            }

            if (value is System.Collections.IEnumerable enumerable && !(value is string))
            {
                var items = new List<string>();
                int count = 0;
                foreach (var item in enumerable)
                {
                    if (count >= 20) break; // Limit to first 20 items
                    items.Add(item?.ToString() ?? "NULL");
                    count++;
                }
                return $"[{string.Join(", ", items)}]" + (count >= 20 ? " ..." : "");
            }

            return value.ToString() ?? "NULL";
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
    }
}