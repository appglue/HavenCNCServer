using CentroidAPI;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HavenCNCServer.Services
{
    /// <summary>
    /// Centralized CNC connection manager for the entire application
    /// Provides static access to CNCPipe instance with automatic connection management
    /// </summary>
    public static class CNCConnectionManager
    {
        private static CNCPipe? _cncPipe;
        private static bool _isConnected = false;
        private static bool _isConnecting = false;
        private static DateTime _lastConnectionAttempt = DateTime.MinValue;
        private static readonly object _lock = new object();

        /// <summary>
        /// Event fired when connection status changes
        /// </summary>
        public static event Action<bool, string>? ConnectionStatusChanged;

        /// <summary>
        /// Gets whether the CNC is currently connected
        /// </summary>
        public static bool IsConnected
        {
            get
            {
                lock (_lock)
                {
                    return _isConnected && _cncPipe != null && _cncPipe.IsConstructed();
                }
            }
        }

        /// <summary>
        /// Gets whether a connection attempt is currently in progress
        /// </summary>
        public static bool IsConnecting
        {
            get
            {
                lock (_lock)
                {
                    return _isConnecting;
                }
            }
        }

        /// <summary>
        /// Gets the current CNCPipe instance if connected, null otherwise
        /// </summary>
        public static CNCPipe? GetCNCPipe()
        {
            lock (_lock)
            {
                if (_isConnected && _cncPipe != null && _cncPipe.IsConstructed())
                {
                    return _cncPipe;
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the CNCPipe instance, attempting to connect if necessary
        /// </summary>
        /// <param name="timeoutMs">Maximum time to spend trying to connect (default: from settings)</param>
        /// <returns>CNCPipe instance if successful, null if failed</returns>
        public static CNCPipe? GetOrCreateCNCPipe(int? timeoutMs = null)
        {
            lock (_lock)
            {
                // Return existing connection if available
                if (_isConnected && _cncPipe != null && _cncPipe.IsConstructed())
                {
                    return _cncPipe;
                }

                // Don't attempt connection if one is already in progress
                if (_isConnecting)
                {
                    return null;
                }

                // Use settings timeout if not specified
                int timeout = timeoutMs ?? SettingsManager.Settings.Cnc.ConnectionTimeoutMs;

                // Attempt to connect
                return ConnectInternal(timeout);
            }
        }

        /// <summary>
        /// Attempts to connect to the CNC asynchronously
        /// </summary>
        /// <param name="timeoutMs">Maximum time to spend trying to connect</param>
        /// <returns>True if connection successful</returns>
        public static async Task<bool> ConnectAsync(int? timeoutMs = null)
        {
            return await Task.Run(() => Connect(timeoutMs));
        }

        /// <summary>
        /// Attempts to connect to the CNC synchronously
        /// </summary>
        /// <param name="timeoutMs">Maximum time to spend trying to connect</param>
        /// <returns>True if connection successful</returns>
        public static bool Connect(int? timeoutMs = null)
        {
            lock (_lock)
            {
                if (_isConnecting)
                {
                    return false; // Already connecting
                }

                int timeout = timeoutMs ?? SettingsManager.Settings.Cnc.ConnectionTimeoutMs;
                var result = ConnectInternal(timeout);
                return result != null;
            }
        }

        /// <summary>
        /// Internal connection method - must be called with lock held
        /// </summary>
        private static CNCPipe? ConnectInternal(int timeoutMs)
        {
            try
            {
                _isConnecting = true;
                _lastConnectionAttempt = DateTime.Now;

                NotifyStatusChanged(false, "Connecting to CNC...");

                var settings = SettingsManager.Settings.Cnc;
                int maxRetries = settings.ConnectionRetries;
                int retryDelay = settings.RetryDelayMs;
                
                DateTime startTime = DateTime.Now;

                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    // Check timeout
                    if ((DateTime.Now - startTime).TotalMilliseconds > timeoutMs)
                    {
                        NotifyStatusChanged(false, "Connection timeout exceeded");
                        break;
                    }

                    try
                    {
                        NotifyStatusChanged(false, $"Connection attempt {attempt}/{maxRetries}...");

                        // Clean up any existing failed instance
                        if (_cncPipe != null)
                        {
                            try
                            {
                                _cncPipe = null;
                            }
                            catch { }
                        }

                        // Create new CNCPipe instance
                        _cncPipe = new CNCPipe();

                        // Check if construction was successful
                        if (_cncPipe.IsConstructed())
                        {
                            _isConnected = true;
                            NotifyStatusChanged(true, "Connected to CNC successfully");

                            // Test basic functionality
                            if (TestConnection(_cncPipe))
                            {
                                NotifyStatusChanged(true, "CNC connection verified and ready");
                                
                                // Restore last fixture point asynchronously after successful connection
                                _ = Task.Run(async () =>
                                {
                                    // Give Centroid a moment to fully stabilize
                                    await Task.Delay(500);
                                    await RestoreLastFixturePointAsync();
                                });
                                
                                return _cncPipe;
                            }
                            else
                            {
                                NotifyStatusChanged(false, "CNC connection test failed");
                                _isConnected = false;
                                _cncPipe = null;
                            }
                        }
                        else
                        {
                            NotifyStatusChanged(false, $"CNC construction failed (attempt {attempt}/{maxRetries})");
                            _cncPipe = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        NotifyStatusChanged(false, $"Connection error (attempt {attempt}/{maxRetries}): {ex.Message}");
                        _cncPipe = null;
                    }

                    // Wait before retry (except on last attempt)
                    if (attempt < maxRetries)
                    {
                        // Check timeout before waiting
                        if ((DateTime.Now - startTime).TotalMilliseconds + retryDelay > timeoutMs)
                        {
                            NotifyStatusChanged(false, "Connection timeout during retry delay");
                            break;
                        }

                        Thread.Sleep(retryDelay);
                    }
                }

                // All attempts failed
                _isConnected = false;
                _cncPipe = null;
                NotifyStatusChanged(false, "All connection attempts failed");
                return null;
            }
            finally
            {
                _isConnecting = false;
            }
        }

        /// <summary>
        /// Tests the CNC connection with basic API calls
        /// </summary>
        private static bool TestConnection(CNCPipe cncPipe)
        {
            try
            {
                // Test parameter reading
                var result = cncPipe.parameter.GetMachineParameterValue(1, out double param1);
                if (result != CNCPipe.ReturnCode.SUCCESS)
                {
                    return false;
                }

                // Test system information
                cncPipe.system.GetUnlockVersion(out CNCPipe.Sys.UnlockVersions version);
                
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Disconnects from the CNC and cleans up resources
        /// </summary>
        public static void Disconnect()
        {
            lock (_lock)
            {
                try
                {
                    if (_cncPipe != null)
                    {
                        _cncPipe = null;
                    }
                }
                catch (Exception ex)
                {
                    NotifyStatusChanged(false, $"Error during disconnect: {ex.Message}");
                }
                finally
                {
                    _isConnected = false;
                    _isConnecting = false;
                    NotifyStatusChanged(false, "Disconnected from CNC");
                }
            }
        }

        /// <summary>
        /// Resets the connection by disconnecting and clearing state
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                Disconnect();
                _lastConnectionAttempt = DateTime.MinValue;
                NotifyStatusChanged(false, "CNC connection reset");
            }
        }

        /// <summary>
        /// Gets detailed connection status information
        /// </summary>
        public static CNCConnectionStatus GetStatus()
        {
            lock (_lock)
            {
                return new CNCConnectionStatus
                {
                    IsConnected = _isConnected,
                    IsConnecting = _isConnecting,
                    HasCNCPipe = _cncPipe != null,
                    IsConstructed = _cncPipe?.IsConstructed() ?? false,
                    LastConnectionAttempt = _lastConnectionAttempt,
                    ConnectionSettings = new CNCConnectionSettings
                    {
                        TimeoutMs = SettingsManager.Settings.Cnc.ConnectionTimeoutMs,
                        Retries = SettingsManager.Settings.Cnc.ConnectionRetries,
                        RetryDelayMs = SettingsManager.Settings.Cnc.RetryDelayMs,
                        AutoConnect = SettingsManager.Settings.Cnc.AutoConnectOnStartup
                    }
                };
            }
        }

        /// <summary>
        /// Gets CNC system information if connected
        /// </summary>
        public static CNCSystemInfo? GetSystemInfo()
        {
            var pipe = GetCNCPipe();
            if (pipe == null) return null;

            try
            {
                pipe.system.GetUnlockVersion(out CNCPipe.Sys.UnlockVersions version);
                
                // Test parameter access
                var param1Result = pipe.parameter.GetMachineParameterValue(1, out double param1);
                var param34Result = pipe.parameter.GetMachineParameterValue(34, out double param34);

                return new CNCSystemInfo
                {
                    SystemType = version.ToString(),
                    Parameter1Value = param1Result == CNCPipe.ReturnCode.SUCCESS ? param1 : null,
                    Parameter34Value = param34Result == CNCPipe.ReturnCode.SUCCESS ? param34 : null,
                    IsParameterAccessWorking = param1Result == CNCPipe.ReturnCode.SUCCESS
                };
            }
            catch (Exception ex)
            {
                return new CNCSystemInfo
                {
                    SystemType = "Unknown",
                    ErrorMessage = ex.Message,
                    IsParameterAccessWorking = false
                };
            }
        }

        /// <summary>
        /// Attempts auto-connection if enabled in settings
        /// </summary>
        public static async Task TryAutoConnectAsync()
        {
            if (SettingsManager.Settings.Cnc.AutoConnectOnStartup && !IsConnected && !IsConnecting)
            {
                await ConnectAsync();
            }
        }

        /// <summary>
        /// Notifies subscribers of connection status changes
        /// </summary>
        private static void NotifyStatusChanged(bool connected, string message)
        {
            try
            {
                ConnectionStatusChanged?.Invoke(connected, message);
            }
            catch (Exception)
            {
                // Don't let event handler exceptions crash the connection manager
            }
        }

        /// <summary>
        /// Restores the last fixture point from CNCMovementController if one was previously set
        /// Called automatically when connection is established
        /// </summary>
        private static async Task RestoreLastFixturePointAsync()
        {
            try
            {
                // Use reflection to avoid circular dependency
                var movementControllerType = Type.GetType("HavenCNCServer.Controllers.CNCMovementController, HavenCNCServer");
                if (movementControllerType != null)
                {
                    var restoreMethod = movementControllerType.GetMethod("RestoreLastFixturePointAsync", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    
                    if (restoreMethod != null)
                    {
                        var task = restoreMethod.Invoke(null, null) as Task;
                        if (task != null)
                        {
                            await task;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Silently fail - fixture point restoration is not critical for connection
            }
        }
    }

    /// <summary>
    /// Detailed CNC connection status information
    /// </summary>
    public class CNCConnectionStatus
    {
        /// <summary>
        /// Gets or sets whether the CNC is currently connected
        /// </summary>
        public bool IsConnected { get; set; }
        
        /// <summary>
        /// Gets or sets whether a connection attempt is in progress
        /// </summary>
        public bool IsConnecting { get; set; }
        
        /// <summary>
        /// Gets or sets whether a CNCPipe instance exists
        /// </summary>
        public bool HasCNCPipe { get; set; }
        
        /// <summary>
        /// Gets or sets whether the CNCPipe has been successfully constructed
        /// </summary>
        public bool IsConstructed { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp of the last connection attempt
        /// </summary>
        public DateTime LastConnectionAttempt { get; set; }
        
        /// <summary>
        /// Gets or sets the connection settings used
        /// </summary>
        public CNCConnectionSettings ConnectionSettings { get; set; } = new();
    }

    /// <summary>
    /// CNC connection settings snapshot
    /// </summary>
    public class CNCConnectionSettings
    {
        /// <summary>
        /// Gets or sets the connection timeout in milliseconds
        /// </summary>
        public int TimeoutMs { get; set; }
        
        /// <summary>
        /// Gets or sets the number of connection retry attempts
        /// </summary>
        public int Retries { get; set; }
        
        /// <summary>
        /// Gets or sets the delay between retry attempts in milliseconds
        /// </summary>
        public int RetryDelayMs { get; set; }
        
        /// <summary>
        /// Gets or sets whether to automatically attempt connection on startup
        /// </summary>
        public bool AutoConnect { get; set; }
    }

    /// <summary>
    /// CNC system information
    /// </summary>
    public class CNCSystemInfo
    {
        /// <summary>
        /// Gets or sets the CNC system type description
        /// </summary>
        public string SystemType { get; set; } = "";
        
        /// <summary>
        /// Gets or sets the value of Parameter 1 from the CNC system
        /// </summary>
        public double? Parameter1Value { get; set; }
        
        /// <summary>
        /// Gets or sets the value of Parameter 34 (spindle encoder counts) from the CNC system
        /// </summary>
        public double? Parameter34Value { get; set; }
        
        /// <summary>
        /// Gets or sets whether parameter access is working correctly
        /// </summary>
        public bool IsParameterAccessWorking { get; set; }
        
        /// <summary>
        /// Gets or sets any error message encountered during system info retrieval
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}