using System;
using System.Linq;
using HavenCNCServer.Models;
using HavenCNCServer.Centroid.Events;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Services
{
    /// <summary>
    /// Service to cache and manage current machine position from DRO events
    /// </summary>
    public static class MachinePositionService
    {
        private static MachinePoint? _lastPosition = null;
        private static readonly object _lock = new object();
        private static bool _isListenerRegistered = false;

        /// <summary>
        /// Initialize the service and register as event listener
        /// </summary>
        public static void Initialize()
        {
            lock (_lock)
            {
                if (!_isListenerRegistered)
                {
                    CNCJobInfoListener.AddListener(new MachinePositionListener());
                    _isListenerRegistered = true;
                    LogDebug("MachinePositionService listener registered", "MachinePositionService");
                }
            }
        }

        /// <summary>
        /// Update position from DRO event (called by listener)
        /// </summary>
        internal static void UpdateFromDRO(DROEvent droEvent)
        {
            lock (_lock)
            {
                _lastPosition = new MachinePoint
                {
                    X = droEvent.Axis1,
                    Y = droEvent.Axis2,
                    Z = droEvent.Axis3,
                    A = droEvent.Axis4
                };
            }
        }

        /// <summary>
        /// Get current machine position. Always fetches fresh data from CNC API.
        /// </summary>
        /// <returns>Current machine position</returns>
        public static MachinePoint GetCurrentPosition()
        {
            lock (_lock)
            {
                // Always fetch from API for most accurate real-time position
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                {
                    // Fallback to cached position if API unavailable
                    if (_lastPosition != null)
                    {
                        LogWarning("CNC connection not available, returning last known position", "MachinePositionService");
                        return new MachinePoint(_lastPosition.X, _lastPosition.Y, _lastPosition.Z, _lastPosition.A);
                    }
                    throw new InvalidOperationException("CNC connection not available and no position data");
                }

                try
                {
                    // Get machine coordinates using DRO API
                    var result = cncPipe.dro.GetDro(CentroidAPI.CNCPipe.Dro.DroCoordinates.DRO_MACHINE, out var droStrings);

                    if (result != CentroidAPI.CNCPipe.ReturnCode.SUCCESS)
                    {
                        LogWarning($"API returned error code for DRO position: {result}", "MachinePositionService");

                        // Fallback to cached position
                        if (_lastPosition != null)
                        {
                            LogWarning("Using last known position due to API error response", "MachinePositionService");
                            return new MachinePoint(_lastPosition.X, _lastPosition.Y, _lastPosition.Z, _lastPosition.A);
                        }
                        throw new InvalidOperationException($"Failed to get DRO position: {result}");
                    }

                    // Check if we got valid data back
                    if (droStrings == null || droStrings.Length == 0)
                    {
                        LogWarning("API returned no DRO data", "MachinePositionService");

                        // Fallback to cached position
                        if (_lastPosition != null)
                        {
                            LogWarning("Using last known position due to empty API response", "MachinePositionService");
                            return new MachinePoint(_lastPosition.X, _lastPosition.Y, _lastPosition.Z, _lastPosition.A);
                        }
                        throw new InvalidOperationException("API returned no DRO data");
                    }

                    // Parse DRO strings into MachinePoint
                    // droStrings is an array of Tuple<string, string, string> (Axis, Position, Load Meter)
                    double x = 0, y = 0, z = 0, a = 0;

                    foreach (var droTuple in droStrings)
                    {
                        string axis = droTuple.Item1;
                        string posStr = droTuple.Item2;

                        if (double.TryParse(posStr, out double value))
                        {
                            switch (axis.ToUpper())
                            {
                                case "X":
                                    x = value;
                                    break;
                                case "Y":
                                    y = value;
                                    break;
                                case "Z":
                                    z = value;
                                    break;
                                case "A":
                                    a = value;
                                    break;
                            }
                        }
                    }

                    var position = new MachinePoint(x, y, z, a);

                    // Update cache with latest position
                    _lastPosition = position;

                    return position;
                }
                catch (Exception ex)
                {
                    LogError($"Failed to get machine position from API: {ex.Message}", "MachinePositionService");

                    // Fallback to cached position if API call fails
                    if (_lastPosition != null)
                    {
                        LogWarning("Using last known position due to API exception", "MachinePositionService");
                        return new MachinePoint(_lastPosition.X, _lastPosition.Y, _lastPosition.Z, _lastPosition.A);
                    }
                    throw;
                }
            }
        }

        /// <summary>
        /// Clear cached position (useful for testing or forcing fresh fetch)
        /// </summary>
        public static void ClearCache()
        {
            lock (_lock)
            {
                _lastPosition = null;
            }
        }

        /// <summary>
        /// Internal listener class that updates position cache from DRO events
        /// </summary>
        private class MachinePositionListener : ICNCEventListener
        {
            public void EventReceived(ICentroidEvent centroidEvent)
            {
                if (centroidEvent is DROEvent droEvent)
                {
                    UpdateFromDRO(droEvent);
                }
            }
        }
    }
}
