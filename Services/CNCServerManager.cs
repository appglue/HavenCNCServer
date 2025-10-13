using System.Diagnostics;
using System.Threading;
using HavenCNCServer.Models;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Services
{
    /// <summary>
    /// Interface for CNC Server process management
    /// </summary>
    public interface ICNCServerManager
    {
        /// <summary>
        /// Whether the CNC server is currently running
        /// </summary>
        bool IsServerRunning { get; }

        /// <summary>
        /// Whether we started the server (and should manage it)
        /// </summary>
        bool WeStartedServer { get; }

        /// <summary>
        /// Start monitoring and managing the CNC server
        /// </summary>
        Task StartManagementAsync();

        /// <summary>
        /// Stop monitoring and shut down the server if we started it
        /// </summary>
        Task StopManagementAsync();

        /// <summary>
        /// Manually start the CNC server
        /// </summary>
        Task<bool> StartServerAsync();

        /// <summary>
        /// Manually stop the CNC server (only if we started it)
        /// </summary>
        Task<bool> StopServerAsync();

        /// <summary>
        /// Force restart the CNC server
        /// </summary>
        Task<bool> RestartServerAsync();
    }

    /// <summary>
    /// Manages the CNC server process lifecycle
    /// </summary>
    public class CNCServerManager : ICNCServerManager, IDisposable
    {
        private readonly CncServerSettings _settings;
        private readonly System.Threading.Timer? _monitorTimer;
        private Process? _serverProcess;
        private bool _weStartedServer = false;
        private bool _isManaging = false;
        private bool _disposed = false;
        private readonly object _lock = new object();

        /// <summary>
        /// Whether the CNC server is currently running
        /// </summary>
        public bool IsServerRunning => _serverProcess?.HasExited == false;
        
        /// <summary>
        /// Whether we started the server (and should manage it)
        /// </summary>
        public bool WeStartedServer => _weStartedServer;

        /// <summary>
        /// Initialize the CNC Server Manager
        /// </summary>
        public CNCServerManager()
        {
            _settings = SettingsManager.Settings.Cnc.Server;
            
            // Create monitoring timer
            if (_settings.AutoRestartServer && _settings.MonitorIntervalMs > 0)
            {
                _monitorTimer = new System.Threading.Timer(MonitorServer, null, System.Threading.Timeout.Infinite, _settings.MonitorIntervalMs);
            }
        }

        /// <summary>
        /// Start managing the CNC server
        /// </summary>
        public async Task StartManagementAsync()
        {
            lock (_lock)
            {
                if (_isManaging)
                    return;
                    
                _isManaging = true;
            }

            LogInfo("Starting CNC server management", "CNCServer");

            try
            {
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
                if (_settings.AutoRestartServer && _monitorTimer != null)
                {
                    _monitorTimer.Change(TimeSpan.FromMilliseconds(_settings.MonitorIntervalMs), 
                                       TimeSpan.FromMilliseconds(_settings.MonitorIntervalMs));
                    LogInfo($"Started CNC server monitoring (interval: {_settings.MonitorIntervalMs}ms)", "CNCServer");
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to start CNC server management: {ex.Message}", "CNCServer");
            }
        }

        /// <summary>
        /// Stop managing the CNC server
        /// </summary>
        public async Task StopManagementAsync()
        {
            lock (_lock)
            {
                if (!_isManaging)
                    return;
                    
                _isManaging = false;
            }

            LogInfo("Stopping CNC server management", "CNCServer");

            try
            {
                // Stop monitoring
                _monitorTimer?.Change(Timeout.Infinite, Timeout.Infinite);

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
        }

        /// <summary>
        /// Start the CNC server process
        /// </summary>
        public async Task<bool> StartServerAsync()
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
                LogInfo($"Full executable path: {Path.GetFullPath(_settings.ExecutablePath)}", "CNCServer");
                
                // Automatically determine working directory from executable path
                var executableDir = Path.GetDirectoryName(_settings.ExecutablePath);
                if (string.IsNullOrEmpty(executableDir))
                {
                    LogError("Could not determine directory from executable path", "CNCServer");
                    return false;
                }
                
                LogInfo($"Auto-detected working directory from executable: {executableDir}", "CNCServer");
                LogInfo($"Full working directory: {Path.GetFullPath(executableDir)}", "CNCServer");
                
                // List files in working directory for debugging
                try
                {
                    var files = Directory.GetFiles(executableDir);
                    LogInfo($"Files in working directory ({files.Length} found):", "CNCServer");
                    foreach (var file in files.Take(10)) // Limit to first 10 files
                    {
                        LogInfo($"  - {Path.GetFileName(file)}", "CNCServer");
                    }
                    if (files.Length > 10)
                    {
                        LogInfo($"  ... and {files.Length - 10} more files", "CNCServer");
                    }
                }
                catch (Exception dirEx)
                {
                    LogError($"Could not list files in working directory: {dirEx.Message}", "CNCServer");
                }

                LogInfo("Creating process start info...", "CNCServer");
                
                // Use the executable's directory as the working directory
                var workingDirToSet = Path.GetDirectoryName(_settings.ExecutablePath);
                LogInfo($"WorkingDirectory to set: '{workingDirToSet ?? "<null>"}'", "CNCServer");
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = _settings.ExecutablePath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = workingDirToSet
                };
                
                // Verify what was actually set
                LogInfo($"ProcessStartInfo.WorkingDirectory after creation: '{startInfo.WorkingDirectory ?? "<null>"}'", "CNCServer");

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

                // Verify working directory was set correctly and check for required files
                if (!string.IsNullOrEmpty(startInfo.WorkingDirectory))
                {
                    LogInfo($"✓ Process working directory set to: {startInfo.WorkingDirectory}", "CNCServer");
                    
                    // Check for critical files in working directory
                    var languageFile = Path.Combine(startInfo.WorkingDirectory, "language.msg");
                    LogInfo($"Checking for language.msg at: {languageFile}", "CNCServer");
                    
                    if (File.Exists(languageFile))
                    {
                        LogSuccess("✓ Found language.msg file in working directory", "CNCServer");
                    }
                    else
                    {
                        LogError("✗ language.msg file NOT found in working directory - this WILL cause startup errors", "CNCServer");
                    }
                }
                else
                {
                    LogError("Failed to set working directory - this will likely cause startup errors", "CNCServer");
                    return false;
                }

                // Log the final command that will be executed
                var fullCommand = _settings.ExecutablePath;
                if (!string.IsNullOrEmpty(startInfo.Arguments))
                {
                    fullCommand += " " + startInfo.Arguments;
                }
                LogInfo($"Executing command: {fullCommand}", "CNCServer");
                LogInfo($"From directory: {startInfo.WorkingDirectory ?? "default"}", "CNCServer");

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
        public async Task<bool> StopServerAsync()
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
        public async Task<bool> RestartServerAsync()
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
        private async Task<bool> IsServerAlreadyRunningAsync()
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
        private void MonitorServer(object? state)
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
        private void OnServerProcessExited(object? sender, EventArgs e)
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

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;
                
            _disposed = true;
            
            _monitorTimer?.Dispose();
            
            // Don't stop the server in Dispose - that should be done explicitly via StopManagementAsync
            _serverProcess?.Dispose();
            
            GC.SuppressFinalize(this);
        }
    }
}