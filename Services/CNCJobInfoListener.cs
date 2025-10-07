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
                    // Get CNC connection
                    var cncPipe = CNCConnectionManager.GetCNCPipe();
                    if (cncPipe == null)
                    {
                        LogError("Cannot start JOB_INFO listener: No CNC connection", "JobInfo");
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

                    // Start the background listener task - don't wait for it to avoid blocking UI
                    _listenerTask = Task.Run(async () => await ListenerLoop(_cancellationTokenSource.Token));

                    _isListening = true;
                    LogSuccess("CNC JOB_INFO listener started successfully", "JobInfo");
                    LogInfo("Monitoring for job execution updates (line numbers, program names, etc.)", "JobInfo");

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
                }
            }
        }

        /// <summary>
        /// Main listener loop that polls for CNC messages
        /// </summary>
        private static async Task ListenerLoop(CancellationToken cancellationToken)
        {
            LogInfo("JOB_INFO listener loop started", "JobInfo");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // Get current CNC connection
                        var cncPipe = CNCConnectionManager.GetCNCPipe();
                        if (cncPipe == null)
                        {
                            LogWarning("CNC connection lost, stopping JOB_INFO listener", "JobInfo");
                            break;
                        }

                        // Try to get unhandled messages
                        while (cncPipe.TryPopUnhandledMessage(out CNCPipe.InboundComm.CommPacket packet))
                        {
                            // Check if this is a JOB_INFO message - check all available types
                            try
                            {
                                // Log all message types for debugging
                                LogDebug($"Received message type: {packet.CommunicationType}", "JobInfo");
                                
                                // Check for job-related communication types
                                var commType = packet.CommunicationType.ToString();
                                if (commType.Contains("JOB") || commType.Contains("job") || 
                                    commType.Contains("Job") || commType.Contains("INFO"))
                                {
                                    ProcessJobInfoMessage(packet);
                                }
                            }
                            catch (Exception msgEx)
                            {
                                LogError($"Error processing message: {msgEx.Message}", "JobInfo");
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
        /// Process a JOB_INFO message and extract relevant data
        /// </summary>
        private static void ProcessJobInfoMessage(CNCPipe.InboundComm.CommPacket packet)
        {
            try
            {
                // Extract job info data from the packet
                var jobInfo = new JobInfoData
                {
                    Timestamp = DateTime.Now,
                    // Try different property access patterns - use reflection to be safe
                    LineNumber = GetPacketProperty<int>(packet, "LineNumber", 0),
                    StackLevel = GetPacketProperty<int>(packet, "StackLevel", 0),
                    Message = GetPacketProperty<string>(packet, "Message", "") ?? "",
                    CommunicationType = packet.CommunicationType.ToString()
                };

                // Log the job info to debug window
                LogInfo($"🔧 JOB INFO UPDATE", "JobInfo");
                LogInfo($"  📍 Line: {jobInfo.LineNumber}", "JobInfo");
                LogInfo($"  📊 Stack Level: {jobInfo.StackLevel}", "JobInfo");
                
                if (!string.IsNullOrWhiteSpace(jobInfo.Message))
                {
                    LogInfo($"  📄 Program: {jobInfo.Message}", "JobInfo");
                }

                LogInfo($"  ⏰ Time: {jobInfo.Timestamp:HH:mm:ss.fff}", "JobInfo");

                // Fire the event for any subscribers
                JobInfoReceived?.Invoke(jobInfo);
            }
            catch (Exception ex)
            {
                LogError($"Error processing JOB_INFO message: {ex.Message}", "JobInfo");
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
                if (!IsListening && CNCConnectionManager.IsConnected)
                {
                    LogInfo("Auto-starting JOB_INFO listener for CNC connection", "JobInfo");
                    StartListening();
                }
            }
            catch (Exception ex)
            {
                LogError($"Error auto-starting JOB_INFO listener: {ex.Message}", "JobInfo");
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