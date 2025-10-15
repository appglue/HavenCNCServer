using System.Diagnostics;
using System.Threading;
using HavenCNCServer.Models;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Services
{
    /// <summary>
    /// Manages the CNC server process lifecycle
    /// </summary>
    public static class CNCServerManager
    {
        private static CncServerSettings _settings = SettingsManager.Settings.Cnc.Server;
        private static System.Threading.Timer? _monitorTimer;
        private static Process? _serverProcess;
        private static bool _weStartedServer = false;
        private static bool _isManaging = false;
        private static CancellationTokenSource? _cancellationTokenSource;
        private static readonly object _lock = new object();

        /// <summary>
        /// Whether the CNC server is currently running
        /// </summary>
        public static bool IsServerRunning => _serverProcess?.HasExited == false;
        
        /// <summary>
        /// Whether we started the server (and should manage it)
        /// </summary>
        public static bool WeStartedServer => _weStartedServer;

        /// <summary>
        /// Whether management is currently active
        /// </summary>
        public static bool IsManaging
        {
            get
            {
                lock (_lock)
                {
                    return _isManaging;
                }
            }
        }

        /// <summary>
        /// Start managing the CNC server
        /// </summary>
        public static async Task StartAsync()
        {
            lock (_lock)
            {
                if (_isManaging)
                {
                    LogWarning("CNC server management is already running", "CNCServer");
                    return;
                }
                    
                _isManaging = true;
                _cancellationTokenSource = new CancellationTokenSource();
            }

            LogInfo("Starting CNC server management", "CNCServer");

            try
            {
                // Refresh settings
                _settings = SettingsManager.Settings.Cnc.Server;
                
                // Check if server is already running
                if (await IsServerAlreadyRunningAsync())
                {
                    LogInfo("CNC server is already running (started externally)", "CNCServer");
                    _weStartedServer = false;
                }
                else if (_settings.AutoStartServer)
                {
                    LogInfo("Starting CNC server automatically", "CNCServer");
                    await StartServerAsync();
                }

                // Start monitoring if enabled
                if (_settings.AutoRestartServer && _settings.MonitorIntervalMs > 0)
                {
                    _monitorTimer = new System.Threading.Timer(MonitorServer, null, 
                        TimeSpan.FromMilliseconds(_settings.MonitorIntervalMs), 
                        TimeSpan.FromMilliseconds(_settings.MonitorIntervalMs));
                    LogInfo($"Started CNC server monitoring (interval: {_settings.MonitorIntervalMs}ms)", "CNCServer");
                }
            }
            catch (Exception ex)
            {
                lock (_lock)
                {
                    _isManaging = false;
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = null;
                }
                LogError($"Failed to start CNC server management: {ex.Message}", "CNCServer");
                throw;
            }
        }

        /// <summary>
        /// Stop managing the CNC server
        /// </summary>
        public static async Task StopAsync()
        {
            lock (_lock)
            {
                if (!_isManaging)
                {
                    LogInfo("CNC server management is not running", "CNCServer");
                    return;
                }
                    
                _isManaging = false;
            }

            LogInfo("Stopping CNC server management", "CNCServer");

            try
            {
                // Stop monitoring
                _monitorTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                _monitorTimer?.Dispose();
                _monitorTimer = null;

                // Cancel any background operations
                _cancellationTokenSource?.Cancel();

                // Stop server if we started it and setting is enabled
                if (_weStartedServer && _settings.StopServerOnShutdown)
                {
                    LogInfo("Stopping CNC server (we started it)", "CNCServer");
                    await StopServerAsync();
                }
                else if (_weStartedServer)
                {
                    LogInfo("Leaving CNC server running (StopServerOnShutdown is disabled)", "CNCServer");
                }
                else
                {
                    LogInfo("Not stopping CNC server (we didn't start it)", "CNCServer");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error during CNC server management shutdown: {ex.Message}", "CNCServer");
            }
            finally
            {
                // Clean up cancellation token
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        /// <summary>
        /// Start the CNC server process
        /// </summary>
        public static async Task<bool> StartServerAsync()
        {
            try
            {
                if (IsServerRunning)
                {
                    LogInfo("CNC server is already running", "CNCServer");
                    return true;
                }

                if (!File.Exists(_settings.ExecutablePath))
                {
                    LogError($"CNC server executable not found: {_settings.ExecutablePath}", "CNCServer");
                    LogError("Please check the ExecutablePath setting in appsettings.json", "CNCServer");
                    return false;
                }

                // Validate that the executable directory exists (will be used as working directory)
                var execDir = Path.GetDirectoryName(_settings.ExecutablePath);
                if (string.IsNullOrEmpty(execDir) || !Directory.Exists(execDir))
                {
                    LogError($"CNC server executable directory not found: {execDir}", "CNCServer");
                    LogError("Please check the ExecutablePath setting in appsettings.json", "CNCServer");
                    return false;
                }

                LogInfo($"Starting CNC server: {_settings.ExecutablePath}", "CNCServer");
                
                // Automatically determine working directory from executable path
                var executableDir = Path.GetDirectoryName(_settings.ExecutablePath);
                if (string.IsNullOrEmpty(executableDir))
                {
                    LogError("Could not determine directory from executable path", "CNCServer");
                    return false;
                }
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = _settings.ExecutablePath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = executableDir
                };

                // Add arguments if specified
                if (_settings.Arguments.Length > 0)
                {
                    startInfo.Arguments = string.Join(" ", _settings.Arguments);
                    LogInfo($"CNC server arguments: {startInfo.Arguments}", "CNCServer");
                }
                else
                {
                    LogInfo("No arguments specified for CNC server", "CNCServer");
                }

                // Verify working directory and critical files
                if (string.IsNullOrEmpty(startInfo.WorkingDirectory))
                {
                    LogError("Failed to set working directory", "CNCServer");
                    return false;
                }
                
                var languageFile = Path.Combine(startInfo.WorkingDirectory, "language.msg");
                if (!File.Exists(languageFile))
                {
                    LogError("language.msg file not found - this will cause startup errors", "CNCServer");
                }

                _serverProcess = Process.Start(startInfo);
                
                if (_serverProcess == null)
                {
                    LogError("Failed to start CNC server process", "CNCServer");
                    LogError("Process.Start returned null - check executable path and permissions", "CNCServer");
                    return false;
                }

                _weStartedServer = true;

                // Set up process event handlers
                _serverProcess.EnableRaisingEvents = true;
                _serverProcess.Exited += OnServerProcessExited;

                LogSuccess($"CNC server process started (PID: {_serverProcess.Id})", "CNCServer");
                
                // Give the process a moment to initialize and check if it's still running
                await Task.Delay(2000);
                
                if (_serverProcess.HasExited)
                {
                    LogError($"CNC server process exited immediately with code: {_serverProcess.ExitCode}", "CNCServer");
                    
                    // Try to read any error output
                    try
                    {
                        var errorOutput = await _serverProcess.StandardError.ReadToEndAsync();
                        if (!string.IsNullOrEmpty(errorOutput))
                        {
                            LogError($"Process error output: {errorOutput}", "CNCServer");
                        }
                        
                        var standardOutput = await _serverProcess.StandardOutput.ReadToEndAsync();
                        if (!string.IsNullOrEmpty(standardOutput))
                        {
                            LogInfo($"Process standard output: {standardOutput}", "CNCServer");
                        }
                    }
                    catch (Exception readEx)
                    {
                        LogError($"Could not read process output: {readEx.Message}", "CNCServer");
                    }
                    
                    return false;
                }
                else
                {
                    LogSuccess("CNC server is running successfully", "CNCServer");
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to start CNC server: {ex.Message}", "CNCServer");
                return false;
            }
        }

        /// <summary>
        /// Stop the CNC server process
        /// </summary>
        public static async Task<bool> StopServerAsync()
        {
            try
            {
                if (_serverProcess == null || _serverProcess.HasExited)
                {
                    LogInfo("CNC server is not running", "CNCServer");
                    return true;
                }

                if (!_weStartedServer)
                {
                    LogWarning("Cannot stop CNC server - we didn't start it", "CNCServer");
                    return false;
                }

                LogInfo($"Stopping CNC server (PID: {_serverProcess.Id})", "CNCServer");

                // Try graceful shutdown first
                _serverProcess.CloseMainWindow();
                
                // Wait for graceful exit
                if (await WaitForExitAsync(_serverProcess, 5000))
                {
                    LogSuccess("CNC server stopped gracefully", "CNCServer");
                    return true;
                }

                // Force kill if necessary
                LogWarning("CNC server didn't exit gracefully, forcing termination", "CNCServer");
                _serverProcess.Kill();
                
                if (await WaitForExitAsync(_serverProcess, 3000))
                {
                    LogSuccess("CNC server terminated forcefully", "CNCServer");
                    return true;
                }

                LogError("Failed to stop CNC server", "CNCServer");
                return false;
            }
            catch (Exception ex)
            {
                LogError($"Error stopping CNC server: {ex.Message}", "CNCServer");
                return false;
            }
            finally
            {
                _serverProcess?.Dispose();
                _serverProcess = null;
                _weStartedServer = false;
            }
        }

        /// <summary>
        /// Restart the CNC server
        /// </summary>
        public static async Task<bool> RestartServerAsync()
        {
            LogInfo("Restarting CNC server", "CNCServer");
            
            await StopServerAsync();
            
            if (_settings.RestartDelayMs > 0)
            {
                LogInfo($"Waiting {_settings.RestartDelayMs}ms before restart", "CNCServer");
                await Task.Delay(_settings.RestartDelayMs);
            }
            
            return await StartServerAsync();
        }

        /// <summary>
        /// Check if the CNC server is already running externally
        /// </summary>
        private static async Task<bool> IsServerAlreadyRunningAsync()
        {
            try
            {
                await Task.Delay(1); // Make async
                
                var serverFileName = Path.GetFileNameWithoutExtension(_settings.ExecutablePath);
                var processes = Process.GetProcessesByName(serverFileName);
                
                return processes.Length > 0;
            }
            catch (Exception ex)
            {
                LogError($"Error checking if CNC server is running: {ex.Message}", "CNCServer");
                return false;
            }
        }

        /// <summary>
        /// Monitor the server process
        /// </summary>
        private static void MonitorServer(object? state)
        {
            try
            {
                if (!_isManaging || !_settings.AutoRestartServer)
                    return;

                if (_weStartedServer && (_serverProcess == null || _serverProcess.HasExited))
                {
                    LogWarning("CNC server has stopped unexpectedly, attempting restart", "CNCServer");
                    
                    // Use Task.Run to avoid blocking the timer thread
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(_settings.RestartDelayMs);
                            await StartServerAsync();
                        }
                        catch (Exception ex)
                        {
                            LogError($"Failed to restart CNC server: {ex.Message}", "CNCServer");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                LogError($"Error in server monitoring: {ex.Message}", "CNCServer");
            }
        }

        /// <summary>
        /// Handle server process exit event
        /// </summary>
        private static void OnServerProcessExited(object? sender, EventArgs e)
        {
            if (_serverProcess != null)
            {
                LogWarning($"CNC server process exited (Exit Code: {_serverProcess.ExitCode})", "CNCServer");
                
                // Log common exit codes for debugging
                switch (_serverProcess.ExitCode)
                {
                    case 0:
                        LogInfo("Process exited normally", "CNCServer");
                        break;
                    case -1:
                    case 1:
                        LogError("Process exited with error - check CNC server logs", "CNCServer");
                        break;
                    case -1073741819: // 0xC0000005
                        LogError("Access violation - possible missing files or permissions issue", "CNCServer");
                        break;
                    default:
                        LogError($"Process exited with unknown error code: {_serverProcess.ExitCode}", "CNCServer");
                        break;
                }
                
                if (_weStartedServer && _settings.AutoRestartServer)
                {
                    LogInfo("Process will be restarted due to AutoRestartServer setting", "CNCServer");
                }
            }
        }

        /// <summary>
        /// Wait for process to exit with timeout
        /// </summary>
        private static async Task<bool> WaitForExitAsync(Process process, int timeoutMs)
        {
            var tcs = new TaskCompletionSource<bool>();
            
            void ProcessExited(object? sender, EventArgs e) => tcs.TrySetResult(true);
            
            try
            {
                process.Exited += ProcessExited;
                process.EnableRaisingEvents = true;
                
                if (process.HasExited)
                    return true;
                
                var timeoutTask = Task.Delay(timeoutMs);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
                
                return completedTask == tcs.Task;
            }
            finally
            {
                process.Exited -= ProcessExited;
            }
        }
    }
}