using CentroidAPI;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Services
{
    /// <summary>
    /// Service for listening to CNC JOB_INFO messages and outputting them to debug logging
    /// </summary>
    public static class CNCJobInfoListener
    {
        private static bool _isListening = false;
        private static CancellationTokenSource? _cancellationTokenSource;
        private static Task? _listenerTask;
        private static readonly object _lock = new object();
        
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

                    // Start listening for messages from CNC12
                    cncPipe.StartListening();
                    LogSuccess("Started CNC12 message listening", "JobInfo");

                    // Create cancellation token for the listener task
                    _cancellationTokenSource = new CancellationTokenSource();

                    // Initialize file logging for detailed messages
                    InitializeFileLogging();
                    
                    // Reset counters
                    _messageCount = 0;
                    _lastReportedCount = 0;
                    _sessionStartTime = DateTime.Now;

                    // Start the background listener task - don't wait for it to avoid blocking UI
                    _listenerTask = Task.Run(async () => await ListenerLoop(_cancellationTokenSource.Token));

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
                    // Cancel the listener task
                    _cancellationTokenSource?.Cancel();

                    // Stop CNC listening
                    var cncPipe = CNCConnectionManager.GetCNCPipe();
                    if (cncPipe != null)
                    {
                        cncPipe.StopListening();
                        LogSuccess("Stopped CNC12 message listening", "JobInfo");
                    }

                    // Wait for the listener task to complete (with timeout)
                    if (_listenerTask != null)
                    {
                        try
                        {
                            _listenerTask.Wait(TimeSpan.FromSeconds(5));
                        }
                        catch (AggregateException ex) when (ex.InnerException is OperationCanceledException)
                        {
                            // Expected when cancelling
                        }
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
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = null;
                    _listenerTask = null;
                    
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
                }
            }
        }

        static string _lastPacketHash = "";
        static int _sameObjectSkipCount = 0;
        static int _lastReportedSkipCount = 0;
        
        /// <summary>
        /// Main listener loop that polls for CNC messages
        /// </summary>
        private static async Task ListenerLoop(CancellationToken cancellationToken)
        {
            LogInfo("🚀 JOB_INFO listener loop started", "JobInfo");
            int heartbeatCounter = 0;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // Periodic heartbeat logging every 60 seconds (1200 cycles * 50ms)
                        heartbeatCounter++;
                        if (heartbeatCounter % 1200 == 0)
                        {
                            LogInfo($"💓 Job Listener active - {_messageCount} messages processed", "JobInfo");
                            
                            // Report same object detection stats
                            if (_sameObjectSkipCount > _lastReportedSkipCount)
                            {
                                var newSkips = _sameObjectSkipCount - _lastReportedSkipCount;
                                LogInfo($"🔄 Same object skipped {newSkips} times (total: {_sameObjectSkipCount})", "JobInfo");
                                _lastReportedSkipCount = _sameObjectSkipCount;
                            }
                        }

                        // Get current CNC connection
                        var cncPipe = CNCConnectionManager.GetCNCPipe();
                        if (cncPipe == null)
                        {
                            LogWarning("🔌 CNC connection lost, waiting for reconnection...", "JobInfo");
                            // Wait a bit and check again instead of stopping immediately
                            await Task.Delay(2000, cancellationToken);
                            continue;
                        }

                        // Try to get unhandled messages
                        while (cncPipe.TryPopUnhandledMessage(out CNCPipe.InboundComm.CommPacket packet))
                        {
                            // Check if this packet is identical to the last one (since CommPacket is a struct)
                            var packetHash = CalculatePacketHash(packet);
                            if (_lastPacketHash == packetHash && !string.IsNullOrEmpty(_lastPacketHash))
                            {
                                _sameObjectSkipCount++;
                                
                                // Silently skip identical packets without logging
                                // if (_sameObjectSkipCount <= 5)
                                // {
                                //     LogToFile($"⚠️ IDENTICAL PACKET detected - skipping #{_sameObjectSkipCount} (Type: {packet.CommunicationType})");
                                // }
                                // else if (_sameObjectSkipCount % 100 == 0)
                                // {
                                //     // Log every 100th identical packet to track frequency
                                //     LogToFile($"⚠️ IDENTICAL PACKET #{_sameObjectSkipCount} (Type: {packet.CommunicationType})");
                                // }
                                continue;
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

                        // Small delay to prevent excessive CPU usage
                        await Task.Delay(50, cancellationToken); // Poll every 50ms
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when cancelling
                        break;
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error in JOB_INFO listener loop: {ex.Message}", "JobInfo");
                        // Continue the loop - don't let a single error stop the listener
                        await Task.Delay(1000, cancellationToken); // Wait a bit longer on error
                    }
                }
            }
            finally
            {
                LogInfo("JOB_INFO listener loop ended", "JobInfo");
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