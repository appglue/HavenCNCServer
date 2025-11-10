using System;
using System.Linq;
using CentroidAPI;
using HavenCNCServer.Services;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Centroid.Events
{
    /// <summary>
    /// Event containing Digital Readout (DRO) position information for all axes
    /// </summary>
    public class DROEvent : ICentroidEvent, ISignalRSerializable
    {
        // Static fields for position tracking
        private static double[]? _lastDroPositions = null;
        private static int _droSamePositionSkipCount = 0;

        // Throttling mechanism for DRO events to frontend - send at most every 100ms
        private static DROEvent? _pendingDroEvent = null;
        private static System.Threading.Timer? _throttleTimer = null;
        private static readonly object _throttleLock = new object();
        private static System.Action<ICentroidEvent>? _pendingNotifyAction = null;
        private static bool _isTimerActive = false;
        private const int ThrottleIntervalMs = 100;

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

        /// <summary>
        /// Type of message/event
        /// </summary>
        public string MessageType { get; set; } = "DROEvent";

        /// <summary>
        /// Process DRO_UPDATE message - Contains position data
        /// Returns tuple of (shouldSkip, droEvent) where shouldSkip indicates if positions haven't changed
        /// </summary>
        public static (bool shouldSkip, DROEvent? droEvent) ProcessMessage(CNCPipe.InboundComm.CommPacket packet, System.Action<string> logToFile, System.Action<ICentroidEvent, string> storeMessage, System.Action<ICentroidEvent> notifyListeners)
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
                            logToFile($"DRO positions unchanged - starting to skip identical position updates...");
                        }
                        else if (_droSamePositionSkipCount % 500 == 0)
                        {
                            logToFile($"DRO positions unchanged - skipped {_droSamePositionSkipCount} identical updates");
                        }

                        return (true, null); // Skip ALL logging for this message
                    }
                    else
                    {
                        // Positions changed, reset skip counter and log the change
                        if (_droSamePositionSkipCount > 0)
                        {
                            logToFile($"--- DRO Position CHANGED after {_droSamePositionSkipCount} unchanged updates ---");
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

                // Check for abnormally large values that might indicate disconnection
                bool hasLargeValue = false;
                double maxAbsValue = 0;
                int largeValueAxis = -1;
                for (int i = 0; i < positions.Length; i++)
                {
                    double absValue = Math.Abs(positions[i]);
                    if (absValue > 1000)
                    {
                        hasLargeValue = true;
                        if (absValue > maxAbsValue)
                        {
                            maxAbsValue = absValue;
                            largeValueAxis = i + 1; // 1-based axis numbering
                        }
                    }
                }

                if (hasLargeValue)
                {
                    // Check if connection is healthy - this will automatically disconnect if not working
                    bool isHealthy = CNCConnectionManager.VerifyConnectionHealth();
                    LogWarning($"⚠️ LARGE DRO! Axis{largeValueAxis}={maxAbsValue:F0} ConnectionHealthy={isHealthy}", "DROEvent");
                    // Create DRO event for position change
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
                    storeMessage(droEvent, "DRO_UPDATE");

                    // Throttle listener notifications to avoid flooding the frontend
                    // Send at most one event every 100ms - always use the latest event
                    ThrottledNotifyListeners(droEvent, notifyListeners);

                    return (false, droEvent); // Don't skip logging - positions have changed
                    return (false, droEvent); // Don't skip logging - positions have changed
                                              // Store the DRO event in message history
                    storeMessage(droEvent, "DRO_UPDATE");

                    // Notify listeners
                    notifyListeners(droEvent);

                    return (false, droEvent); // Don't skip logging - positions have changed
                }

                return (false, null); // Don't skip logging but no event created
            }
        /// <summary>
        /// Throttled notification to listeners - sends at most one event every 100ms
        /// Always sends the most recent event received during that interval
        /// </summary>
        private static void ThrottledNotifyListeners(DROEvent droEvent, System.Action<ICentroidEvent> notifyListeners)
        {
            lock (_throttleLock)
            {
                // Always store the latest event
                _pendingDroEvent = droEvent;
                _pendingNotifyAction = notifyListeners;

                // If timer is not active, start it and send immediately
                if (!_isTimerActive)
                {
                    _isTimerActive = true;

                    // Send the first event immediately
                    try
                    {
                        notifyListeners(droEvent);
                        _pendingDroEvent = null;
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error in throttled DRO notification: {ex.Message}", "DROEvent");
                    }

                    // Start timer for subsequent events
                    if (_throttleTimer == null)
                    {
                        _throttleTimer = new System.Threading.Timer(OnThrottleTimerElapsed, null, ThrottleIntervalMs, ThrottleIntervalMs);
                    }
                    else
                    {
                        _throttleTimer.Change(ThrottleIntervalMs, ThrottleIntervalMs);
                    }
                }
                // Otherwise, the pending event will be sent on the next timer tick
            }
        }

        /// <summary>
        /// Called every 100ms - sends the most recent DRO event if one is pending
        /// </summary>
        private static void OnThrottleTimerElapsed(object? state)
        {
            DROEvent? eventToSend = null;
            System.Action<ICentroidEvent>? actionToCall = null;

            lock (_throttleLock)
            {
                eventToSend = _pendingDroEvent;
                actionToCall = _pendingNotifyAction;

        /// <summary>
        /// Reset static tracking data (used when listener stops)
        /// </summary>
        public static void ResetTracking()
        {
            _lastDroPositions = null;
            _droSamePositionSkipCount = 0;

            // Clean up throttle timer
            lock (_throttleLock)
            {
                _isTimerActive = false;
                _throttleTimer?.Dispose();
                _throttleTimer = null;
                _pendingDroEvent = null;
                _pendingNotifyAction = null;
            }
        }       {
                    actionToCall(eventToSend);
    }
                catch (Exception ex)
                {
                    LogError($"Error in throttled DRO notification: {ex.Message}", "DROEvent");
}
            }
        }

        /// <summary>
        /// Reset static tracking data (used when listener stops)
        /// </summary>
        public static void ResetTracking()
{
    _lastDroPositions = null;
    _droSamePositionSkipCount = 0;

    // Clean up debounce timer
    lock (_debounceLock)
    {
        _debounceTimer?.Dispose();
        _debounceTimer = null;
        _pendingDroEvent = null;
        _pendingNotifyAction = null;
    }
}
_droSamePositionSkipCount = 0;
        }

        /// <summary>
        /// Serialize the DRO event for SignalR transmission
        /// </summary>
        /// <returns>The event itself for complete data transmission</returns>
        public object ToSignalRData()
{
    return this;
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
        LogDebug($"Error getting packet property '{propertyName}': {ex.Message}", "DROEvent");
        return defaultValue;
    }
}
    }
}