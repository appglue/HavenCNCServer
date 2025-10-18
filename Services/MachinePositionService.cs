using System;
using System.Linq;
using HavenCNCServer.Models;
using HavenCNCServer.Centriod.Events;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Services
{
    /// <summary>
    /// Service to cache and manage current machine position from DRO events
    /// </summary>
    public static class MachinePositionService
    {
        private static MachinePoint? _cachedPosition = null;
        private static readonly object _lock = new object();

        /// <summary>
        /// Get current machine position. Returns cached position from DRO events if available,
        /// otherwise fetches directly from CNC API
        /// </summary>
        /// <returns>Current machine position</returns>
        public static MachinePoint GetCurrentPosition()
        {
            lock (_lock)
            {
                // Return cached position if available (from previous DRO events or API fetch)
                if (_cachedPosition != null)
                {
                    return new MachinePoint(_cachedPosition.X, _cachedPosition.Y, _cachedPosition.Z, _cachedPosition.A);
                }

                // No cached position yet - try to get from recent DRO events
                var recentDroEvents = CNCJobInfoListener.GetRecentMessagesByType<DROEvent>(5000);
                if (recentDroEvents.Count > 0)
                {
                    var latestDro = (DROEvent)recentDroEvents[0].Event;
                    _cachedPosition = new MachinePoint
                    {
                        X = latestDro.Axis1,
                        Y = latestDro.Axis2,
                        Z = latestDro.Axis3,
                        A = latestDro.Axis4
                    };
                    return new MachinePoint(_cachedPosition.X, _cachedPosition.Y, _cachedPosition.Z, _cachedPosition.A);
                }

                // No cache and no recent events - fetch from API
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                if (cncPipe == null)
                    throw new InvalidOperationException("CNC connection not available and no cached position");

                try
                {
                    // Get machine coordinates using DRO API
                    var result = cncPipe.dro.GetDro(CentroidAPI.CNCPipe.Dro.DroCoordinates.DRO_MACHINE, out var droStrings);
                    
                    if (result != CentroidAPI.CNCPipe.ReturnCode.SUCCESS)
                        throw new InvalidOperationException($"Failed to get DRO position: {result}");

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
                    
                    // Cache the position for future calls
                    _cachedPosition = position;
                    
                    LogDebug($"Fetched machine position from API: {position}", "MachinePositionService");
                    
                    return position;
                }
                catch (Exception ex)
                {
                    LogError($"Failed to get machine position from API: {ex.Message}", "MachinePositionService");
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
                _cachedPosition = null;
            }
        }
    }
}
