using Microsoft.AspNetCore.SignalR;
using HavenCNCServer.Hubs;
using HavenCNCServer.Centriod.Events;
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

                            LogInfo("Adding listener to CNCJobInfoListener", "SignalR");
                            CNCJobInfoListener.AddListener(listener);

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
        /// Send a message to clients subscribed to a specific message type
        /// </summary>
        /// <param name="messageType">Type of the message</param>
        /// <param name="data">Message data</param>
        public static async Task SendToMessageTypeAsync(string messageType, object data)
        {
            if (_hubContext == null)
            {
                LogWarning("Cannot send SignalR message - hub context not available", "SignalR");
                return;
            }

            try
            {
                await _hubContext.Clients.Group($"MessageType_{messageType}").SendAsync("ReceiveCNCMessage", new
                {
                    MessageType = messageType,
                    Timestamp = DateTime.UtcNow,
                    Data = data
                });
            }
            catch (Exception ex)
            {
                LogError($"Error sending SignalR message to {messageType} subscribers: {ex.Message}", "SignalR");
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
        /// Start the heartbeat timer that periodically sends system status to clients
        /// </summary>
        private static void StartHeartbeatTimer()
        {
            // Get interval from settings, default to 30 seconds
            int intervalMs = SettingsManager.Settings.Cnc.HeartbeatIntervalMs;

            _heartbeatTimer = new System.Threading.Timer(
                callback: _ => SendHeartbeat(),
                state: null,
                dueTime: TimeSpan.FromSeconds(5), // First heartbeat after 5 seconds
                period: TimeSpan.FromMilliseconds(intervalMs)
            );

            LogInfo($"Heartbeat timer started with interval: {intervalMs}ms", "SignalR");
        }

        /// <summary>
        /// Send a heartbeat message with system status and current position to all connected clients
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

                    var heartbeat = new
                    {
                        Timestamp = DateTime.UtcNow,
                        ServerTime = DateTime.Now,
                        IsConnected = isConnected,
                        Status = isConnected ? "Connected" : "Disconnected",
                        MessageType = "Heartbeat",
                        Position = position
                    };

                    // Broadcast heartbeat to all clients
                    await _hubContext.Clients.Group("CNCClients").SendAsync("Heartbeat", heartbeat);
                }
                catch (Exception ex)
                {
                    LogError($"Error sending heartbeat: {ex.Message}", "SignalR");
                }
            });
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
    }

    /// <summary>
    /// Event listener that forwards CNC events to SignalR clients
    /// </summary>
    internal class SignalREventListener : ICNCEventListener
    {
        private readonly IHubContext<CNCMessageHub> _hubContext;

        public SignalREventListener(IHubContext<CNCMessageHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public void EventReceived(ICentroidEvent centroidEvent)
        {
            // Don't block the main thread with SignalR calls
            _ = Task.Run(async () =>
            {
                try
                {
                    var messageType = centroidEvent.GetType().Name;
                    var messageData = new
                    {
                        EventType = messageType,
                        Timestamp = DateTime.UtcNow,
                        Data = SerializeEvent(centroidEvent)
                    };

                    // Send to all clients
                    await _hubContext.Clients.Group("CNCClients").SendAsync("ReceiveCNCMessage", messageData);

                    // Also send to specific message type subscribers
                    await _hubContext.Clients.Group($"MessageType_{messageType}").SendAsync("ReceiveCNCMessage", messageData);
                }
                catch (Exception ex)
                {
                    LogError($"Error forwarding event to SignalR: {ex.Message}", "SignalR");
                }
            });
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
    }
}