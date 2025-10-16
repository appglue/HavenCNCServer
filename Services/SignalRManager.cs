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