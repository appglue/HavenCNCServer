using Microsoft.AspNetCore.SignalR;
using HavenCNCServer.Hubs;
using HavenCNCServer.Centroid.Events;
using HavenCNCServer.Centroid;
using HavenCNCServer.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Services
{
    /// <summary>
    /// Manages SignalR event listeners for CNC events
    /// SignalR itself is configured and managed by the ASP.NET Core pipeline in ApiStartup.cs
    /// </summary>
    public static class SignalRManager
    {
        private static IHubContext<CNCMessageHub>? _hubContext;
        private static bool _listenersSetup = false;
        private static readonly object _lock = new object();
        private static System.Threading.Timer? _heartbeatTimer;
        private static string? _cachedPlcVersion = null;

        /// <summary>
        /// Set up event listeners to forward CNC events to SignalR clients
        /// This should be called after the API server has started
        /// </summary>
        public static void SetupEventListeners()
        {
            LogInfo("SetupEventListeners() called", "SignalR");

            // Run asynchronously to avoid blocking
            _ = Task.Run(() =>
            {
                try
                {
                    LogInfo("Background task started, acquiring lock", "SignalR");

                    lock (_lock)
                    {
                        LogInfo("Lock acquired", "SignalR");

                        if (_listenersSetup)
                        {
                            LogInfo("SignalR event listeners already set up - exiting", "SignalR");
                            return;
                        }

                        LogInfo("Setting up SignalR event listeners...", "SignalR");

                        LogInfo("About to call ApiManager.GetHubContext()", "SignalR");

                        // Get hub context from the running API server
                        _hubContext = ApiManager.GetHubContext();

                        LogInfo($"GetHubContext() returned: {(_hubContext != null ? "valid context" : "NULL")}", "SignalR");

                        if (_hubContext != null)
                        {
                            LogInfo("Creating SignalREventListener instance", "SignalR");
                            var listener = new SignalREventListener(_hubContext);

                            LogInfo("Subscribing to CNCEventBus (new channel-based architecture)", "SignalR");
                            Console.WriteLine("[SignalRManager] About to subscribe SignalREventListener to CNCEventBus");
                            CNCEventBus.Instance.Subscribe(listener);
                            Console.WriteLine($"[SignalRManager] Subscribed! EventBus now has {CNCEventBus.Instance.GetSubscriberCount()} subscribers");

                            LogInfo("Subscribing to CNC connection status changes", "SignalR");
                            CNCConnectionManager.ConnectionStatusChanged += OnConnectionStatusChanged;

                            LogInfo("Starting heartbeat timer", "SignalR");
                            StartHeartbeatTimer();

                            LogInfo("Marking listeners as setup", "SignalR");
                            _listenersSetup = true;

                            LogSuccess("SignalR event listeners registered successfully", "SignalR");
                        }
                        else
                        {
                            LogWarning("SignalR hub context not available - event listeners not registered", "SignalR");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Exception in SetupEventListeners: {ex.Message}", "SignalR");
                    LogError($"Stack trace: {ex.StackTrace}", "SignalR");
                }
            });

            LogInfo("SetupEventListeners() returning (background task started)", "SignalR");
        }        /// <summary>
                 /// Get the current setup status
                 /// </summary>
        public static bool IsSetup
        {
            get
            {
                lock (_lock)
                {
                    return _listenersSetup;
                }
            }
        }

        /// <summary>
        /// Send a message to all connected clients
        /// </summary>
        /// <param name="messageType">Type of the message</param>
        /// <param name="data">Message data</param>
        public static async Task SendToAllAsync(string messageType, object data)
        {
            if (_hubContext == null)
            {
                LogWarning("Cannot send SignalR message - hub context not available", "SignalR");
                return;
            }

            try
            {
                await _hubContext.Clients.Group("CNCClients").SendAsync("ReceiveCNCMessage", new
                {
                    MessageType = messageType,
                    Timestamp = DateTime.UtcNow,
                    Data = data
                });
            }
            catch (Exception ex)
            {
                LogError($"Error sending SignalR message: {ex.Message}", "SignalR");
            }
        }

        /// <summary>
        /// Broadcast output state change to all connected SignalR clients
        /// </summary>
        /// <param name="outputNumber">The output number that changed</param>
        /// <param name="state">The new state (true = on/forced on, false = off/forced off)</param>
        /// <param name="forceState">The force state (ForcedOn, ForcedOff, or NotForced)</param>
        public static async Task BroadcastOutputStateChanged(int outputNumber, bool state, string forceState)
        {
            if (_hubContext == null)
            {
                LogWarning("Cannot broadcast output state change - hub context not available", "SignalR");
                return;
            }

            try
            {
                // Try to get monitored IO configuration to get name
                string outputName = $"OUT{outputNumber}";

                var monitoredConfig = Controllers.PLCController.LoadMonitoredIOConfiguration();
                if (monitoredConfig != null)
                {
                    var monitoredOutput = monitoredConfig.Outputs.FirstOrDefault(o => o.Number == outputNumber);
                    if (monitoredOutput != null)
                    {
                        outputName = monitoredOutput.Name;
                    }
                }

                // Format message as: "[name] is on/off"
                string onOff = state ? "on" : "off";
                string message = $"[{outputName}] is {onOff}";

                await _hubContext.Clients.Group("CNCClients").SendAsync("ReceiveCNCMessage", new
                {
                    EventType = "OutputStateChanged",
                    Timestamp = DateTime.UtcNow,
                    Data = new
                    {
                        OutputNumber = outputNumber,
                        State = state,
                        ForceState = forceState,
                        IsForced = forceState != "NotForced",
                        Name = outputName,
                        Message = message
                    }
                });

                LogInfo($"Broadcasted output state change: {message} ({forceState})", "SignalR");
            }
            catch (Exception ex)
            {
                LogError($"Error broadcasting output state change: {ex.Message}", "SignalR");
            }
        }

        /// <summary>
        /// Broadcast the current machine position to all connected SignalR clients
        /// Useful when position changes outside of normal DRO updates (e.g., fixture point changes)
        /// </summary>
        /// <param name="reason">Optional reason for the position broadcast</param>
        public static async Task BroadcastCurrentPosition(string reason = "Position update")
        {
            if (_hubContext == null)
            {
                LogWarning("Cannot broadcast position - hub context not available", "SignalR");
                return;
            }

            try
            {
                // Get current position from the service
                var currentPosition = MachinePositionService.GetCurrentPosition();

                // Create a DRO event with current position
                var droEvent = new DROEvent
                {
                    Timestamp = DateTime.Now,
                    Axis1 = currentPosition.X,
                    Axis2 = currentPosition.Y,
                    Axis3 = currentPosition.Z,
                    Axis4 = currentPosition.A,
                    Axis5 = 0,
                    Axis6 = 0,
                    Axis7 = 0,
                    Axis8 = 0,
                    Message = reason,
                    MessageType = "DROEvent"
                };

                // Send to all clients as a DRO update
                await _hubContext.Clients.Group("CNCClients").SendAsync("DROUpdate", droEvent.ToSignalRData());

                LogInfo($"Broadcasted position: X={currentPosition.X:F4}, Y={currentPosition.Y:F4}, Z={currentPosition.Z:F4}, A={currentPosition.A:F4} - Reason: {reason}", "SignalR");
            }
            catch (Exception ex)
            {
                LogError($"Error broadcasting position: {ex.Message}", "SignalR");
            }
        }

        /// <summary>
        /// Handler for CNC connection status changes
        /// Broadcasts connection/disconnection events to all SignalR clients
        /// </summary>
        private static void OnConnectionStatusChanged(bool isConnected, string message)
        {
            if (_hubContext == null)
            {
                return;
            }

            // Run asynchronously to avoid blocking the connection manager
            _ = Task.Run(async () =>
            {
                try
                {
                    var statusMessage = new
                    {
                        IsConnected = isConnected,
                        Message = message,
                        Timestamp = DateTime.UtcNow,
                        Status = isConnected ? "Connected" : "Disconnected"
                    };

                    // Broadcast to all clients
                    await _hubContext.Clients.Group("CNCClients").SendAsync("ConnectionStatusChanged", statusMessage);

                    LogInfo($"Broadcasted connection status: {(isConnected ? "CONNECTED" : "DISCONNECTED")} - {message}", "SignalR");
                }
                catch (Exception ex)
                {
                    LogError($"Error broadcasting connection status: {ex.Message}", "SignalR");
                }
            });
        }

        /// <summary>
        /// Start the heartbeat timer that periodically sends server status updates to clients
        /// Sends connection status and position data every 2 seconds
        /// </summary>
        private static void StartHeartbeatTimer()
        {
            // Use 2 seconds for server status updates for better responsiveness
            int actualIntervalMs = 2000; // 2 seconds

            _heartbeatTimer = new System.Threading.Timer(
                callback: _ => SendHeartbeat(),
                state: null,
                dueTime: TimeSpan.FromSeconds(2), // First update after 2 seconds
                period: TimeSpan.FromMilliseconds(actualIntervalMs)
            );

            LogInfo($"Server status timer started with interval: {actualIntervalMs}ms (2 seconds for status updates with position)", "SignalR");
        }

        /// <summary>
        /// Send a server status message with system status and current position to all connected clients
        /// This provides periodic updates every 2 seconds with connection status and position data
        /// </summary>
        private static void SendHeartbeat()
        {
            if (_hubContext == null)
            {
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {

                    var isConnected = CNCConnectionManager.IsConnected;

                    // Check if a job is running
                    bool isJobRunning = Controllers.CNCProgramController.IsJobRunning();

                    // Check if API is restricted - always check when connected, independent of job status
                    bool isApiRestricted = false;
                    if (isConnected)
                    {
                        try
                        {
                            var cncPipe = CNCConnectionManager.GetCNCPipe();
                            if (cncPipe != null)
                            {
                                var result = cncPipe.state.IsAPIRestricted(out isApiRestricted);
                                if (result != CentroidAPI.CNCPipe.ReturnCode.SUCCESS)
                                {
                                    LogWarning($"Failed to check API restriction status: {result}", "SignalR");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogWarning($"Error checking API restriction: {ex.Message}", "SignalR");
                        }
                    }

                    // Only try to get current position if connected
                    object? position = null;
                    if (isConnected)
                    {
                        try
                        {
                            var currentPosition = MachinePositionService.GetCurrentPosition();
                            position = new
                            {
                                X = currentPosition.X,
                                Y = currentPosition.Y,
                                Z = currentPosition.Z,
                                A = currentPosition.A
                            };
                        }
                        catch (Exception ex)
                        {
                            // If we can't get position even when "connected", log it and send null
                            LogWarning($"Could not get position despite connection status: {ex.Message}", "SignalR");
                            position = null;
                        }
                    }

                    // Get incremental jog mode status (Output 1082)
                    bool isIncrementalJogMode = false;
                    if (isConnected)
                    {
                        try
                        {
                            var cncPipe = CNCConnectionManager.GetCNCPipe();
                            if (cncPipe != null)
                            {
                                var result = cncPipe.plc.GetOutputState(1082, out CentroidAPI.CNCPipe.Plc.IOState state);
                                if (result == CentroidAPI.CNCPipe.ReturnCode.SUCCESS)
                                {
                                    isIncrementalJogMode = (state == CentroidAPI.CNCPipe.Plc.IOState.IO_LOGICAL_1);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogWarning($"Could not get incremental jog mode status: {ex.Message}", "SignalR");
                        }
                    }

                    // Get fast jogging mode status (Output 1094)
                    bool isFastJogging = false;
                    if (isConnected)
                    {
                        try
                        {
                            var cncPipe = CNCConnectionManager.GetCNCPipe();
                            if (cncPipe != null)
                            {
                                var result = cncPipe.plc.GetOutputState(1094, out CentroidAPI.CNCPipe.Plc.IOState state);
                                if (result == CentroidAPI.CNCPipe.ReturnCode.SUCCESS)
                                {
                                    isFastJogging = (state == CentroidAPI.CNCPipe.Plc.IOState.IO_LOGICAL_1);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogWarning($"Could not get fast jogging status: {ex.Message}", "SignalR");
                        }
                    }

                    // Get homed status
                    bool isHomed = false;
                    if (isConnected)
                    {
                        try
                        {
                            var cncPipe = CNCConnectionManager.GetCNCPipe();
                            if (cncPipe != null)
                            {
                                // Check each axis home status using GetPcSystemVariableBit
                                cncPipe.plc.GetPcSystemVariableBit(CentroidAPI.PcToMpuSysVarBit.SV_HOME_SET_AXIS_1, out var state_axis_1);
                                cncPipe.plc.GetPcSystemVariableBit(CentroidAPI.PcToMpuSysVarBit.SV_HOME_SET_AXIS_1, out var state_axis_2);
                                cncPipe.plc.GetPcSystemVariableBit(CentroidAPI.PcToMpuSysVarBit.SV_HOME_SET_AXIS_1, out var state_axis_3);

                                if (state_axis_1 == CentroidAPI.CNCPipe.Plc.IOState.IO_LOGICAL_1 &&
                                    state_axis_2 == CentroidAPI.CNCPipe.Plc.IOState.IO_LOGICAL_1 &&
                                    state_axis_3 == CentroidAPI.CNCPipe.Plc.IOState.IO_LOGICAL_1)
                                {
                                    cncPipe.plc.GetPcSystemVariableBit(CentroidAPI.PcToMpuSysVarBit.SV_PC_HOME_SET, out var state_home);
                                    isHomed = (state_home == CentroidAPI.CNCPipe.Plc.IOState.IO_LOGICAL_1);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogWarning($"Could not get homed status: {ex.Message}", "SignalR");
                        }
                    }

                    // Check if machine requires reset (SV_STOP state)
                    bool requiresReset = false;
                    if (isConnected)
                    {
                        try
                        {
                            var cncPipe = CNCConnectionManager.GetCNCPipe();
                            if (cncPipe != null)
                            {
                                // SV_STOP indicates the machine is in a stopped/fault state requiring reset
                                var result = cncPipe.plc.GetPlcSystemVariableBit(CentroidAPI.MpuToPcSysVarBit.SV_STOP, out CentroidAPI.CNCPipe.Plc.IOState svStopState);
                                if (result == CentroidAPI.CNCPipe.ReturnCode.SUCCESS)
                                {
                                    requiresReset = (svStopState == CentroidAPI.CNCPipe.Plc.IOState.IO_LOGICAL_1);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogWarning($"Could not get requires reset status: {ex.Message}", "SignalR");
                        }
                    }

                    // Get PLC version from cache (populated on startup and after installation)
                    string plcVersion = _cachedPlcVersion ?? LoadPlcVersion();

                    // Parse version into major.minor.update
                    int? versionMajor = null;
                    int? versionMinor = null;
                    int versionUpdate = 0;

                    if (!string.IsNullOrEmpty(plcVersion) &&
                        plcVersion != "Not Installed" &&
                        plcVersion != "Unknown")
                    {
                        var versionParts = plcVersion.Split('.');
                        if (versionParts.Length >= 1 && int.TryParse(versionParts[0], out int major))
                            versionMajor = major;
                        if (versionParts.Length >= 2 && int.TryParse(versionParts[1], out int minor))
                            versionMinor = minor;
                        if (versionParts.Length >= 3 && int.TryParse(versionParts[2], out int update))
                            versionUpdate = update;
                    }

                    // Get system board type, machine type, and expansion board count
                    string boardType = "Unknown";
                    string machineType = "Unknown";
                    int expansionBoardCount = 0;
                    if (isConnected)
                    {
                        try
                        {
                            var cncPipe = CNCConnectionManager.GetCNCPipe();
                            if (cncPipe != null)
                            {
                                var unlockResult = cncPipe.system.GetUnlockVersion(out CentroidAPI.CNCPipe.Sys.UnlockVersions unlockVersion);
                                if (unlockResult == CentroidAPI.CNCPipe.ReturnCode.SUCCESS)
                                {
                                    string unlockVersionStr = unlockVersion.ToString();

                                    // Detect board type from unlock version
                                    if (unlockVersionStr.Contains("Acorn", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (unlockVersionStr.Contains("Six", StringComparison.OrdinalIgnoreCase) ||
                                            unlockVersionStr.Contains("6", StringComparison.OrdinalIgnoreCase))
                                        {
                                            boardType = "AcornSix";
                                        }
                                        else
                                        {
                                            boardType = "Acorn";
                                        }
                                    }
                                    else if (unlockVersionStr.Contains("Hickory", StringComparison.OrdinalIgnoreCase))
                                    {
                                        boardType = "Hickory";
                                    }
                                }

                                // Get machine type
                                var machineTypeResult = cncPipe.system.GetMachineType(out CentroidAPI.CNCPipe.Sys.MachineTypes mt);
                                if (machineTypeResult == CentroidAPI.CNCPipe.ReturnCode.SUCCESS)
                                {
                                    machineType = mt.ToString();
                                }

                                // Count expansion boards
                                // Check for Ethernet expansion boards
                                var etherResult = cncPipe.system.GetEther1616DeviceInfo(out List<CentroidAPI.CNCPipe.Sys.Ether1616Device> etherDevices);
                                if (etherResult == CentroidAPI.CNCPipe.ReturnCode.SUCCESS)
                                {
                                    expansionBoardCount += etherDevices?.Count ?? 0;
                                }

                                // Check for PLC expansion boards
                                var plcExpResult = cncPipe.system.GetPLCEXP1616NumberofDevices(out int plcExpCount);
                                if (plcExpResult == CentroidAPI.CNCPipe.ReturnCode.SUCCESS)
                                {
                                    expansionBoardCount += plcExpCount;
                                }

                                // Check for EtherCAT expansion boards
                                var ecatResult = cncPipe.system.GetECAT1616NumberOfDevices(out int ecatCount);
                                if (ecatResult == CentroidAPI.CNCPipe.ReturnCode.SUCCESS)
                                {
                                    expansionBoardCount += ecatCount;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogWarning($"Could not get board/machine type: {ex.Message}", "SignalR");
                        }
                    }

                    var serverStatus = new
                    {
                        Timestamp = DateTime.UtcNow,
                        ServerTime = DateTime.Now,
                        IsConnected = isConnected,
                        Status = isConnected ? "Connected" : "Disconnected",
                        MessageType = "ServerStatus",
                        Position = position,
                        IsApiRestricted = isApiRestricted,
                        IsJobRunning = isJobRunning,
                        RequiresReset = requiresReset,
                        IsHomed = isHomed,
                        JogSettings = Controllers.CNCMovementController.GetJogSettingsForStatus(),
                        PlcVersion = new
                        {
                            Major = versionMajor,
                            Minor = versionMinor,
                            Update = versionUpdate
                        },
                        BoardType = boardType,
                        MachineType = machineType,
                        ExpansionBoardCount = expansionBoardCount
                    };

                    // Send server status as a regular SignalR message via ReceiveCNCMessage
                    await _hubContext.Clients.Group("CNCClients").SendAsync("ReceiveCNCMessage", new
                    {
                        EventType = "ServerStatus",
                        Timestamp = DateTime.UtcNow,
                        Data = serverStatus
                    });
                }
                catch (Exception ex)
                {
                    LogError($"Error sending server status: {ex.Message}", "SignalR");
                }
            });
        }

        /// <summary>
        /// Broadcast the current server status to all connected clients
        /// </summary>
        public static async Task BroadcastServerStatus()
        {
            // SendHeartbeat is void, so wrap it in Task.Run to make it awaitable
            await Task.Run(() => SendHeartbeat());
        }

        /// <summary>
        /// Stop the heartbeat timer (called during shutdown)
        /// </summary>
        public static void StopHeartbeat()
        {
            lock (_lock)
            {
                _heartbeatTimer?.Dispose();
                _heartbeatTimer = null;
                LogInfo("Heartbeat timer stopped", "SignalR");
            }
        }

        /// <summary>
        /// Load and cache the version number from the currently installed PLC source file
        /// </summary>
        /// <returns>PLC version string or empty if not found</returns>
        private static string LoadPlcVersion()
        {
            try
            {
                var cnc12Path = SettingsManager.Settings.Cnc.Cnc12Path;
                var plcSourcePath = System.IO.Path.Combine(cnc12Path, "havencncplc.src");

                if (!System.IO.File.Exists(plcSourcePath))
                {
                    _cachedPlcVersion = "Not Installed";
                    return _cachedPlcVersion;
                }

                // Read first few lines to find version
                var lines = System.IO.File.ReadLines(plcSourcePath).Take(10).ToArray();

                // Look for line containing "Version:"
                foreach (var line in lines)
                {
                    if (line.Contains("Version:", StringComparison.OrdinalIgnoreCase))
                    {
                        // Extract version number after "Version:"
                        var versionIndex = line.IndexOf("Version:", StringComparison.OrdinalIgnoreCase);
                        if (versionIndex >= 0)
                        {
                            var version = line.Substring(versionIndex + 8).Trim();
                            _cachedPlcVersion = version;
                            return _cachedPlcVersion;
                        }
                    }
                }

                _cachedPlcVersion = "Unknown";
                return _cachedPlcVersion;
            }
            catch
            {
                _cachedPlcVersion = "Unknown";
                return _cachedPlcVersion;
            }
        }

        /// <summary>
        /// Refresh the cached PLC version (call this after installing a new PLC)
        /// </summary>
        public static void RefreshPlcVersion()
        {
            LoadPlcVersion();
            LogInfo($"PLC version refreshed: {_cachedPlcVersion}", "SignalR");
        }
    }

    /// <summary>
    /// Event listener that forwards CNC events to SignalR clients
    /// Uses a dedicated thread with queue to ensure ordered delivery
    /// </summary>
    /// <summary>
    /// Event listener for broadcasting CNC events to SignalR clients via the new CNCEventBus
    /// </summary>
    public class SignalREventListener : IEventSubscriber
    {
        private readonly IHubContext<CNCMessageHub> _hubContext;
        private long _skippedDroEventCount = 0;

        // Queue-based processing for ordered message delivery
        private readonly System.Collections.Concurrent.BlockingCollection<ICentroidEvent> _messageQueue;
        private readonly Thread _processingThread;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private bool _isRunning = false;

        /// <summary>
        /// Initializes a new instance of the SignalREventListener class with a dedicated message queue processor
        /// </summary>
        /// <param name="hubContext">The SignalR hub context for broadcasting messages</param>
        public SignalREventListener(IHubContext<CNCMessageHub> hubContext)
        {
            _hubContext = hubContext;
            _messageQueue = new System.Collections.Concurrent.BlockingCollection<ICentroidEvent>(boundedCapacity: 1000);
            _cancellationTokenSource = new CancellationTokenSource();

            // Start dedicated processing thread for ordered delivery
            _processingThread = new Thread(ProcessMessageQueue)
            {
                Name = "SignalR-MessageProcessor",
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal // Higher priority for real-time messages
            };
            _isRunning = true;
            _processingThread.Start();

            LogInfo("SignalR message queue processor started with bounded capacity of 1000", "SignalR");
        }

        /// <summary>
        /// Specify which event types this subscriber wants to receive (all types)
        /// </summary>
        public EventTypeFlags GetSubscribedEvents()
        {
            return EventTypeFlags.All; // Subscribe to all event types
        }

        /// <summary>
        /// Receive position/DRO update from event bus
        /// </summary>
        public void OnPositionUpdate(DROEvent position)
        {
            if (!_isRunning)
            {
                return;
            }

            // Try to add to queue without blocking
            if (!_messageQueue.TryAdd(position))
            {
                // Queue is full - log and drop the event
                LogWarning($"SignalR message queue full - dropping DROEvent", "SignalR");
            }
        }

        /// <summary>
        /// Receive log message from event bus
        /// </summary>
        public void OnLogMessage(LogEvent log)
        {
            if (!_isRunning)
            {
                return;
            }

            // Try to add to queue without blocking
            if (!_messageQueue.TryAdd(log))
            {
                // Queue is full - log and drop the event
                LogWarning($"SignalR message queue full - dropping LogEvent", "SignalR");
            }
        }

        /// <summary>
        /// Receive CNC message from event bus
        /// </summary>
        public void OnCNCMessage(ICentroidEvent message)
        {
            if (!_isRunning)
            {
                return;
            }

            // Try to add to queue without blocking
            if (!_messageQueue.TryAdd(message))
            {
                // Queue is full - log and drop the event
                LogWarning($"SignalR message queue full - dropping {message.GetType().Name} event", "SignalR");
            }
        }

        // LEGACY SUPPORT: Keep ICNCEventListener interface for compatibility during migration
        /// <summary>
        /// Receives a CNC event and queues it for processing and broadcast to SignalR clients
        /// </summary>
        /// <param name="centroidEvent">The CNC event to process and broadcast</param>
        public void EventReceived(ICentroidEvent centroidEvent)
        {
            if (!_isRunning)
            {
                return;
            }

            // Try to add to queue without blocking
            if (!_messageQueue.TryAdd(centroidEvent))
            {
                // Queue is full - log and drop the event
                LogWarning($"SignalR message queue full - dropping {centroidEvent.GetType().Name} event", "SignalR");
            }
        }

        /// <summary>
        /// Dedicated thread that processes messages from queue in order
        /// </summary>
        private void ProcessMessageQueue()
        {
            LogInfo("SignalR message processor thread started", "SignalR");

            try
            {
                foreach (var centroidEvent in _messageQueue.GetConsumingEnumerable(_cancellationTokenSource.Token))
                {
                    try
                    {
                        ProcessEventAsync(centroidEvent).Wait();
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error processing event in queue: {ex.Message}", "SignalR");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                LogInfo("SignalR message processor cancelled", "SignalR");
            }
            catch (Exception ex)
            {
                LogError($"Fatal error in SignalR message processor: {ex.Message}", "SignalR");
            }
        }

        /// <summary>
        /// Process a single event and send to SignalR
        /// </summary>
        private async Task ProcessEventAsync(ICentroidEvent centroidEvent)
        {
            var messageType = centroidEvent.GetType().Name;

            // Reset skipped counter when connection is restored and we get a non-DRO event or connected DRO event
            if (CNCConnectionManager.IsConnected && _skippedDroEventCount > 0)
            {
                LogSuccess($"🔄 CNC12 reconnected - Reset DRO skip counter (was {_skippedDroEventCount} events)", "SignalR");
                _skippedDroEventCount = 0;
            }

            // Skip DRO events when CNC12 is not connected to prevent flood of disconnection events
            if (messageType == "DROEvent" && !CNCConnectionManager.IsConnected)
            {
                // Special logging to track DRO events during disconnection
                var droEvent = centroidEvent as DROEvent;
                var positionInfo = droEvent != null ?
                    $" - Position: X:{droEvent.Axis1:F4}, Y:{droEvent.Axis2:F4}, Z:{droEvent.Axis3:F4}" :
                    "";

                // Only log this occasionally to avoid log spam, but show position data
                if (DateTime.Now.Second % 15 == 0) // Log once every 15 seconds
                {
                    LogWarning($"⚠️ DRO event received while CNC12 disconnected{positionInfo} - Skipping SignalR broadcast", "SignalR");
                }

                // Count total skipped events (log every 100 events)
                if (_skippedDroEventCount % 100 == 0)
                {
                    LogInfo($"📊 Total DRO events skipped during disconnection: {_skippedDroEventCount}", "SignalR");
                }
                _skippedDroEventCount++;

                return;
            }

            var messageData = new
            {
                EventType = messageType,
                Timestamp = DateTime.UtcNow,
                Data = SerializeEvent(centroidEvent)
            };

            // Send to all clients in CNCClients group
            await _hubContext.Clients.Group("CNCClients").SendAsync("ReceiveCNCMessage", messageData);
        }

        private static object SerializeEvent(ICentroidEvent centroidEvent)
        {
            // If the event implements ISignalRSerializable, let it serialize itself
            if (centroidEvent is ISignalRSerializable serializableEvent)
            {
                return serializableEvent.ToSignalRData();
            }

            // Fallback: return the event object as-is for JSON serialization
            // This preserves all properties without transformation
            return centroidEvent;
        }

        /// <summary>
        /// Stop the message processor and clean up resources
        /// </summary>
        public void Dispose()
        {
            _isRunning = false;
            _cancellationTokenSource.Cancel();
            _messageQueue.CompleteAdding();

            if (!_processingThread.Join(TimeSpan.FromSeconds(5)))
            {
                LogWarning("SignalR message processor thread did not stop within timeout", "SignalR");
            }

            _messageQueue.Dispose();
            _cancellationTokenSource.Dispose();

            LogInfo("SignalR message processor stopped", "SignalR");
        }
    }
}