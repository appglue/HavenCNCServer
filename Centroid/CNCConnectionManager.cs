using CentroidAPI;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using static HavenCNCServer.Services.LoggingService;

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
        private static DateTime _lastConnectionCheck = DateTime.MinValue;
        private static int _connectionRetryCount = 0;
        private static readonly object _lock = new object();

        // Process check caching to avoid tight loops
        private static bool _cachedProcessRunning = true;
        private static DateTime _lastProcessCheck = DateTime.MinValue;
        private static readonly double ProcessCheckIntervalSeconds = 2.0; // Check every 2 seconds
        private static int _isCnc12ProcessRunningFlag = 1; // 1 = true, 0 = false, for Interlocked

        /// <summary>
        /// Event fired when connection status changes
        /// </summary>
        public static event Action<bool, string>? ConnectionStatusChanged;

        /// <summary>
        /// Gets whether the CNC12 process is currently running (cached, lock-free)
        /// </summary>
        public static bool IsCnc12ProcessRunning
        {
            get
            {
                // Lock-free read using Interlocked
                return Interlocked.CompareExchange(ref _isCnc12ProcessRunningFlag, 0, 0) == 1;
            }
        }

        /// <summary>
        /// Gets the number of connection retry attempts since last disconnect
        /// </summary>
        public static int ConnectionRetryCount
        {
            get
            {
                // Lock-free read using Interlocked
                return Interlocked.CompareExchange(ref _connectionRetryCount, 0, 0);
            }
        }

        /// <summary>
        /// Gets whether the CNC is currently connected
        /// </summary>
        public static bool IsConnected
        {
            get
            {
                lock (_lock)
                {
                    // CRITICAL: First check if CNC12 process is running
                    // If not, immediately mark as disconnected and clear the pipe
                    if (!IsCNC12ProcessRunning())
                    {
                        if (_isConnected || _cncPipe != null)
                        {
                            Log("CNC12 process not running - clearing connection", LogLevel.Warning, "CNCConnectionManager");
                            _isConnected = false;
                            _cncPipe = null;
                            NotifyStatusChanged(false, "CNC12 process terminated");
                        }
                        return false;
                    }

                    // Only verify connection health every 5 seconds to avoid spamming checks
                    var timeSinceLastCheck = (DateTime.Now - _lastConnectionCheck).TotalSeconds;

                    if (_isConnected && _cncPipe != null && timeSinceLastCheck > 5)
                    {
                        _lastConnectionCheck = DateTime.Now;

                        try
                        {
                            if (!_cncPipe.IsConstructed())
                            {
                                // CNC pipe is no longer valid - update status
                                _isConnected = false;
                                _cncPipe = null;
                                NotifyStatusChanged(false, "CNC connection lost");
                                return false;
                            }
                        }
                        catch (Exception ex)
                        {
                            // IsConstructed() threw an exception - CNC12 probably crashed
                            LogError($"Exception checking connection status: {ex.Message}", "CNCConnectionManager");
                            _isConnected = false;
                            _cncPipe = null;
                            NotifyStatusChanged(false, $"CNC connection error: {ex.Message}");
                            return false;
                        }
                    }
                    return _isConnected;
                }
            }
        }

        /// <summary>
        /// Checks if CNC connection is established and valid with full health verification
        /// This performs additional checks beyond just the connection flag
        /// </summary>
        /// <returns>True if connected with valid CNCPipe, false otherwise</returns>
        private static bool CheckIsConnected()
        {
            lock (_lock)
            {
                if (!_isConnected || _cncPipe == null)
                {
                    return false;
                }

                // CRITICAL: Check if process is running - if not, clear connection immediately
                if (!IsCNC12ProcessRunning())
                {
                    Log("Process check failed in CheckIsConnected - clearing connection", LogLevel.Warning, "CNCConnectionManager");
                    _isConnected = false;
                    _cncPipe = null;
                    return false;
                }

                try
                {
                    return _cncPipe.IsConstructed();
                }
                catch (Exception ex)
                {
                    LogError($"Exception in CheckIsConnected: {ex.Message}", "CNCConnectionManager");
                    _isConnected = false;
                    _cncPipe = null;
                    return false;
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
                // CRITICAL: Check process first - clear pipe if process not running
                if (!IsCNC12ProcessRunning())
                {
                    if (_isConnected || _cncPipe != null)
                    {
                        Log("Process not running in GetCNCPipe - clearing connection", LogLevel.Warning, "CNCConnectionManager");
                        _isConnected = false;
                        _cncPipe = null;
                        NotifyStatusChanged(false, "CNC12 process terminated");
                    }
                    return null;
                }

                if (_isConnected)
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
                // CRITICAL: Check process first - don't attempt any CNC operations if process not running
                if (!IsCNC12ProcessRunning())
                {
                    var processName = SettingsManager.Settings?.Cnc?.Cnc12ProcessName ?? "cncr";
                    Log($"CNC12 process '{processName}' not running - cannot create pipe", LogLevel.Warning, "CNCConnectionManager");

                    // Clear any existing connection state
                    if (_isConnected || _cncPipe != null)
                    {
                        _isConnected = false;
                        _cncPipe = null;
                        NotifyStatusChanged(false, $"CNC12 process '{processName}' not running");
                    }

                    return null;
                }

                // Return existing connection if available
                if (_isConnected)
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
        /// <summary>
        /// Checks if the CNC12 process is currently running
        /// </summary>
        /// <returns>True if the process is running, false otherwise</returns>
        private static bool IsCNC12ProcessRunning()
        {
            try
            {
                // Use cached result if checked recently (within interval)
                var timeSinceLastCheck = (DateTime.Now - _lastProcessCheck).TotalSeconds;
                if (timeSinceLastCheck < ProcessCheckIntervalSeconds)
                {
                    return _cachedProcessRunning;
                }

                // Time to do actual check
                _lastProcessCheck = DateTime.Now;

                // Check if settings are initialized
                if (SettingsManager.Settings?.Cnc == null)
                {
                    _cachedProcessRunning = true; // If settings not loaded yet, assume running
                    return _cachedProcessRunning;
                }

                var processName = SettingsManager.Settings.Cnc.Cnc12ProcessName;
                if (string.IsNullOrWhiteSpace(processName))
                {
                    _cachedProcessRunning = true; // If not configured, assume it's running
                    return _cachedProcessRunning;
                }

                var processes = Process.GetProcessesByName(processName);
                bool isRunning = processes.Length > 0;

                // Clean up process handles
                foreach (var p in processes)
                {
                    try { p.Dispose(); } catch { }
                }

                // Only log when state changes
                if (isRunning != _cachedProcessRunning)
                {
                    if (isRunning)
                    {
                        Log($"Process check: '{processName}' is now RUNNING", LogLevel.Info, "CNCConnectionManager");
                    }
                    else
                    {
                        Log($"Process check: '{processName}' is NOT running", LogLevel.Warning, "CNCConnectionManager");
                    }
                }

                _cachedProcessRunning = isRunning;
                Interlocked.Exchange(ref _isCnc12ProcessRunningFlag, isRunning ? 1 : 0);
                return _cachedProcessRunning;
            }
            catch (Exception ex)
            {
                LogError($"Error checking CNC12 process: {ex.Message}", "CNCConnectionManager");
                // On error, be conservative - assume NOT running to avoid potential crash
                _cachedProcessRunning = false;
                Interlocked.Exchange(ref _isCnc12ProcessRunningFlag, 0);
                return false;
            }
        }

        private static CNCPipe? ConnectInternal(int timeoutMs)
        {
            try
            {
                // Check if CNC12 process is running before attempting connection
                if (!IsCNC12ProcessRunning())
                {
                    var processName = SettingsManager.Settings.Cnc.Cnc12ProcessName;
                    NotifyStatusChanged(false, $"CNC12 process '{processName}' is not running");
                    Log($"Cannot connect - CNC12 process '{processName}' is not running", LogLevel.Warning, "CNCConnectionManager");
                    return null;
                }

                _isConnecting = true;
                _lastConnectionAttempt = DateTime.Now;

                NotifyStatusChanged(false, "Connecting to CNC...");

                var settings = SettingsManager.Settings.Cnc;
                int maxRetries = settings.ConnectionRetries;
                int retryDelay = settings.RetryDelayMs;

                DateTime startTime = DateTime.Now;

                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    // Check if CNC12 process is still running before each attempt
                    if (!IsCNC12ProcessRunning())
                    {
                        var processName = SettingsManager.Settings.Cnc.Cnc12ProcessName;
                        NotifyStatusChanged(false, $"CNC12 process '{processName}' stopped - aborting connection attempts");
                        Log($"CNC12 process '{processName}' no longer running - stopping retry attempts", LogLevel.Warning, "CNCConnectionManager");
                        break;
                    }

                    // Check timeout
                    if ((DateTime.Now - startTime).TotalMilliseconds > timeoutMs)
                    {
                        NotifyStatusChanged(false, "Connection timeout exceeded");
                        break;
                    }

                    try
                    {
                        Interlocked.Increment(ref _connectionRetryCount);
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
                            Log($"CNCPipe constructed successfully on attempt {attempt}", LogLevel.Info, "CNCConnectionManager");
                            _isConnected = true;
                            Interlocked.Exchange(ref _connectionRetryCount, 0); // Reset counter on success
                            NotifyStatusChanged(true, "Connected to CNC successfully");

                            // Connection is established - no additional tests needed
                            Log("Connection established, CNCPipe is constructed and ready", LogLevel.Info, "CNCConnectionManager");

                            // Restore last fixture point asynchronously after successful connection
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    // Give Centroid a moment to fully stabilize
                                    await Task.Delay(500);
                                    await RestoreLastFixturePointAsync();

                                    // Reset all outputs to clean state on startup
                                    await ResetAllOutputsAsync();
                                }
                                catch (Exception ex)
                                {
                                    LogError($"Error during post-connection initialization: {ex.Message}", "CNCConnectionManager");
                                }
                            });

                            return _cncPipe;
                        }
                        else
                        {
                            Log($"CNCPipe construction failed on attempt {attempt}", LogLevel.Warning, "CNCConnectionManager");
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
                Log("Testing CNC connection...", LogLevel.Info, "CNCConnectionManager");

                // Test parameter reading
                var result = cncPipe.parameter.GetMachineParameterValue(1, out double param1);
                Log($"GetMachineParameterValue(1) returned: {result}, value: {param1}", LogLevel.Info, "CNCConnectionManager");

                if (result != CNCPipe.ReturnCode.SUCCESS)
                {
                    Log($"Parameter test failed with code: {result}, but continuing anyway", LogLevel.Warning, "CNCConnectionManager");
                    // Don't fail on parameter read - sometimes parameters aren't ready immediately
                    // return false;
                }

                // Test system information
                cncPipe.system.GetUnlockVersion(out CNCPipe.Sys.UnlockVersions version);
                Log($"GetUnlockVersion returned: {version}", LogLevel.Info, "CNCConnectionManager");

                Log("CNC connection test passed", LogLevel.Info, "CNCConnectionManager");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Connection test failed: {ex.Message}", LogLevel.Error, "CNCConnectionManager");
                return false;
            }
        }

        /// <summary>
        /// Checks if the CNC connection is actually working (health check)
        /// If the connection appears established but is not working, disconnects automatically
        /// </summary>
        /// <returns>True if connection is healthy, false otherwise</returns>
        public static bool VerifyConnectionHealth()
        {
            lock (_lock)
            {
                // CRITICAL: First check if process is running
                if (!IsCNC12ProcessRunning())
                {
                    if (_isConnected || _cncPipe != null)
                    {
                        NotifyStatusChanged(false, "Connection health check failed: CNC12 process not running");
                        _isConnected = false;
                        _cncPipe = null;
                    }
                    return false;
                }

                // If we're not supposed to be connected, return false
                if (!_isConnected || _cncPipe == null)
                {
                    return false;
                }

                // Check if the pipe is still constructed
                try
                {
                    if (!_cncPipe.IsConstructed())
                    {
                        NotifyStatusChanged(false, "Connection health check failed: CNCPipe not constructed");
                        _isConnected = false;
                        _cncPipe = null;
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Exception in HealthCheck IsConstructed: {ex.Message}", "CNCConnectionManager");
                    _isConnected = false;
                    _cncPipe = null;
                    return false;
                }

                // Check if CNC12 is powering off (deprecated but still useful)
                try
                {
                    var powerOffResult = _cncPipe.state.IsPCPoweringOff(out bool isPoweringOff);
                    if (powerOffResult == CNCPipe.ReturnCode.SUCCESS && isPoweringOff)
                    {
                        NotifyStatusChanged(false, "Connection health check failed: CNC12 is powering off");
                        _isConnected = false;
                        _cncPipe = null;
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    NotifyStatusChanged(false, $"Connection health check failed: Error checking power state - {ex.Message}");
                    _isConnected = false;
                    _cncPipe = null;
                    return false;
                }

                // Try to read a parameter to verify the connection is actually working
                try
                {
                    var result = _cncPipe.parameter.GetMachineParameterValue(1, out double _);
                    if (result != CNCPipe.ReturnCode.SUCCESS)
                    {
                        NotifyStatusChanged(false, $"Connection health check failed: Cannot read parameters (ReturnCode: {result})");
                        _isConnected = false;
                        _cncPipe = null;
                        return false;
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    NotifyStatusChanged(false, $"Connection health check failed: {ex.Message}");
                    _isConnected = false;
                    _cncPipe = null;
                    return false;
                }
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
                bool isConstructed = false;
                if (_cncPipe != null && IsCNC12ProcessRunning())
                {
                    try
                    {
                        isConstructed = _cncPipe.IsConstructed();
                    }
                    catch
                    {
                        isConstructed = false;
                    }
                }

                return new CNCConnectionStatus
                {
                    IsConnected = _isConnected,
                    IsConnecting = _isConnecting,
                    HasCNCPipe = _cncPipe != null,
                    IsConstructed = isConstructed,
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

        /// <summary>
        /// Reset all outputs to normal (not forced) state on startup
        /// </summary>
        private static async Task ResetAllOutputsAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        var cncPipe = GetCNCPipe();
                        if (cncPipe == null)
                        {
                            LogWarning("Cannot reset outputs - no CNC connection", "CNCConnectionManager");
                            return;
                        }

                        // Get output definitions from PLC source file
                        string sourcePath = @"C:\cncr\acorn_router_plc.src";
                        if (!System.IO.File.Exists(sourcePath))
                        {
                            LogWarning($"Cannot reset outputs - PLC source file not found: {sourcePath}", "CNCConnectionManager");
                            return;
                        }

                        var outputNumbers = GetOutputNumbersFromPLC(sourcePath);
                        if (outputNumbers.Length == 0)
                        {
                            LogWarning("No outputs found in PLC source file", "CNCConnectionManager");
                            return;
                        }

                        LogInfo($"Resetting {outputNumbers.Length} outputs to normal state...", "CNCConnectionManager");

                        int successCount = 0;
                        int errorCount = 0;

                        foreach (var outputNumber in outputNumbers)
                        {
                            try
                            {
                                var result = cncPipe.plc.SetIoForceState(
                                    outputNumber,
                                    CentroidAPI.CNCPipe.Plc.BitType.Output,
                                    CentroidAPI.CNCPipe.Plc.ForceState.NotForced);

                                if (result == CentroidAPI.CNCPipe.ReturnCode.SUCCESS)
                                {
                                    successCount++;
                                }
                                else
                                {
                                    errorCount++;
                                    LogWarning($"Failed to reset output {outputNumber}: {result}", "CNCConnectionManager");
                                }
                            }
                            catch (Exception ex)
                            {
                                errorCount++;
                                LogWarning($"Error resetting output {outputNumber}: {ex.Message}", "CNCConnectionManager");
                            }
                        }

                        if (errorCount == 0)
                        {
                            LogInfo($"✓ Successfully reset all {successCount} outputs to normal state", "CNCConnectionManager");
                        }
                        else
                        {
                            LogWarning($"Reset outputs completed: {successCount} success, {errorCount} errors", "CNCConnectionManager");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"Failed to reset outputs on startup: {ex.Message}", "CNCConnectionManager");
                    }
                });
            }
            catch (Exception ex)
            {
                LogError($"Failed to start output reset task: {ex.Message}", "CNCConnectionManager");
            }
        }

        /// <summary>
        /// Get output numbers from PLC source file
        /// </summary>
        private static int[] GetOutputNumbersFromPLC(string filePath)
        {
            try
            {
                var outputNumbers = new List<int>();
                var outputPattern = new System.Text.RegularExpressions.Regex(@"^([A-Za-z0-9_]+)\s+IS\s+OUT(\d+)", System.Text.RegularExpressions.RegexOptions.Compiled);

                var lines = System.IO.File.ReadAllLines(filePath);
                bool inOutputSection = false;

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();

                    // Detect output section start
                    if (trimmedLine.Contains("Output Definitions") || trimmedLine.Contains("OUTPUT DEFINITIONS"))
                    {
                        inOutputSection = true;
                        continue;
                    }

                    // Stop at #endregion
                    if (trimmedLine == "; #endregion")
                    {
                        inOutputSection = false;
                        continue;
                    }

                    // Skip comments and empty lines
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";"))
                        continue;

                    // Parse output definitions when in output section
                    if (inOutputSection)
                    {
                        var outputMatch = outputPattern.Match(trimmedLine);
                        if (outputMatch.Success)
                        {
                            int outputNumber = int.Parse(outputMatch.Groups[2].Value);
                            if (!outputNumbers.Contains(outputNumber))
                            {
                                outputNumbers.Add(outputNumber);
                            }
                        }
                    }
                }

                return outputNumbers.OrderBy(n => n).ToArray();
            }
            catch (Exception ex)
            {
                LogError($"Failed to parse output numbers from PLC file: {ex.Message}", "CNCConnectionManager");
                return Array.Empty<int>();
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