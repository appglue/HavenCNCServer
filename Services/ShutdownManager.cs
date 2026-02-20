using System;
using System.Threading;
using System.Threading.Tasks;
using HavenCNCServer.Centroid;
using HavenCNCServer.Centroid.Events;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Services
{
    /// <summary>
    /// Centralized shutdown manager for graceful application termination
    /// Ensures all services are stopped in the correct order, only once
    /// </summary>
    public static class ShutdownManager
    {
        private static readonly object _shutdownLock = new object();
        private static bool _isShuttingDown = false;
        private static bool _shutdownComplete = false;
        private static CancellationTokenSource? _shutdownCancellationSource;
        private static System.Threading.Timer? _forceExitTimer;

        /// <summary>
        /// Indicates whether shutdown is in progress or completed
        /// </summary>
        public static bool IsShuttingDown => _isShuttingDown;

        /// <summary>
        /// Indicates whether shutdown has completed
        /// </summary>
        public static bool IsShutdownComplete => _shutdownComplete;

        /// <summary>
        /// Register the application's main cancellation token source
        /// This will be cancelled during shutdown
        /// </summary>
        public static void RegisterCancellationSource(CancellationTokenSource cancellationSource)
        {
            _shutdownCancellationSource = cancellationSource;
        }

        /// <summary>
        /// Initiate graceful shutdown of the application
        /// Safe to call multiple times - only executes once
        /// </summary>
        /// <param name="maxShutdownTimeSeconds">Maximum time in seconds before force exit</param>
        public static void InitiateShutdown(int maxShutdownTimeSeconds = 5)
        {
            lock (_shutdownLock)
            {
                if (_isShuttingDown)
                {
                    LogWarning("Shutdown already in progress, ignoring duplicate request", "Shutdown");
                    return;
                }
                _isShuttingDown = true;
            }

            LogInfo("═══════════════════════════════════════════════════════", "Shutdown");
            LogInfo("INITIATING APPLICATION SHUTDOWN", "Shutdown");
            LogInfo("═══════════════════════════════════════════════════════", "Shutdown");

            // Start force exit timer
            _forceExitTimer = new System.Threading.Timer(_ =>
            {
                LogWarning($"!!! FORCE EXIT AFTER {maxShutdownTimeSeconds} SECONDS !!!", "Shutdown");
                try
                {
                    var app = System.Windows.Application.Current;
                    if (app != null)
                    {
                        app.Dispatcher.Invoke(() => app.Shutdown(1));
                    }
                    else
                    {
                        Environment.Exit(1);
                    }
                }
                catch
                {
                    Environment.Exit(1);
                }
            }, null, maxShutdownTimeSeconds * 1000, Timeout.Infinite);

            // Execute shutdown immediately on this thread
            ExecuteShutdownSequence();
        }

        /// <summary>
        /// Execute the shutdown sequence in order
        /// </summary>
        private static void ExecuteShutdownSequence()
        {
            try
            {
                LogInfo("Starting shutdown sequence...", "Shutdown");

                // Step 1: Cancel all background operations
                LogInfo("[1/7] Cancelling background operations...", "Shutdown");
                try
                {
                    _shutdownCancellationSource?.Cancel();
                    Thread.Sleep(100); // Brief pause to let cancellation propagate
                    LogSuccess("[1/7] ✓ Background operations cancelled", "Shutdown");
                }
                catch (Exception ex)
                {
                    LogError($"[1/7] ✗ Error cancelling operations: {ex.Message}", "Shutdown");
                }

                // Step 2: Stop Centroid Event Bridge (stops CNC connection monitoring)
                LogInfo("[2/7] Stopping Centroid Event Bridge...", "Shutdown");
                try
                {
                    var bridgeTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token;
                    CentroidEventBridge.Stop(bridgeTimeout);
                    LogSuccess("[2/7] ✓ Centroid Event Bridge stopped", "Shutdown");
                }
                catch (Exception ex)
                {
                    LogError($"[2/7] ✗ Event Bridge error: {ex.Message}", "Shutdown");
                }

                // Step 3: Stop API Manager (ASP.NET Core Web API)
                LogInfo("[3/7] Stopping API Manager...", "Shutdown");
                try
                {
                    var apiTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    ApiManager.StopAsync(apiTimeout.Token).Wait(3500);
                    LogSuccess("[3/7] ✓ API Manager stopped", "Shutdown");
                }
                catch (Exception ex)
                {
                    LogError($"[3/7] ✗ API Manager error: {ex.Message}", "Shutdown");
                }

                // Step 4: Dispose CNC Event Bus (worker threads)
                LogInfo("[4/7] Disposing CNC Event Bus...", "Shutdown");
                try
                {
                    CNCEventBus.Instance.Dispose();
                    LogSuccess("[4/7] ✓ CNC Event Bus disposed", "Shutdown");
                }
                catch (Exception ex)
                {
                    LogError($"[4/7] ✗ Event Bus error: {ex.Message}", "Shutdown");
                }

                // Step 5: Exit CNC12 gracefully BEFORE disconnecting (needs the pipe)
                LogInfo("[5/7] Exiting CNC12...", "Shutdown");
                try
                {
                    var cnc12ProcessName = SettingsManager.Settings?.Cnc?.Cnc12ProcessName ?? "cncr";
                    var processes = System.Diagnostics.Process.GetProcessesByName(cnc12ProcessName);
                    bool isCnc12Running = processes.Length > 0;
                    foreach (var p in processes) { try { p.Dispose(); } catch { } }

                    if (isCnc12Running)
                    {
                        CNCUtils.ExitCNC12();
                        LogSuccess("[5/7] ✓ CNC12 exit command sent", "Shutdown");
                    }
                    else
                    {
                        LogInfo("[5/7] ⊘ CNC12 not running, skipping exit", "Shutdown");
                    }
                }
                catch (Exception ex)
                {
                    LogError($"[5/7] ✗ CNC12 exit error: {ex.Message}", "Shutdown");
                }

                // Step 6: Disconnect from CNC (after CNC12 has been told to exit)
                LogInfo("[6/7] Disconnecting from CNC...", "Shutdown");
                try
                {
                    CNCConnectionManager.Disconnect();
                    LogSuccess("[6/7] ✓ CNC disconnected", "Shutdown");
                }
                catch (Exception ex)
                {
                    LogError($"[6/7] ✗ CNC disconnect error: {ex.Message}", "Shutdown");
                }

                // Step 7: Dispose cancellation source
                LogInfo("[7/7] Cleaning up resources...", "Shutdown");
                try
                {
                    _shutdownCancellationSource?.Dispose();
                    LogSuccess("[7/7] ✓ Resources cleaned up", "Shutdown");
                }
                catch (Exception ex)
                {
                    LogError($"[7/7] ✗ Cleanup error: {ex.Message}", "Shutdown");
                }

                _shutdownComplete = true;
                LogSuccess("═══════════════════════════════════════════════════════", "Shutdown");
                LogSuccess("SHUTDOWN SEQUENCE COMPLETED SUCCESSFULLY", "Shutdown");
                LogSuccess("═══════════════════════════════════════════════════════", "Shutdown");

                // Cancel the force exit timer
                _forceExitTimer?.Dispose();

                // Give logs time to flush
                Thread.Sleep(200);

                // Use WPF Application.Shutdown instead of Environment.Exit to avoid WeakEventTable errors
                try
                {
                    var app = System.Windows.Application.Current;
                    if (app != null)
                    {
                        // Must invoke on UI thread
                        app.Dispatcher.Invoke(() => app.Shutdown(0));
                    }
                    else
                    {
                        // Fallback if no WPF app context
                        Environment.Exit(0);
                    }
                }
                catch
                {
                    // If dispatcher fails, use Environment.Exit
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                LogError($"Fatal error during shutdown: {ex.Message}", "Shutdown");
                LogError($"Stack trace: {ex.StackTrace}", "Shutdown");
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Force immediate shutdown without graceful cleanup
        /// Use only as last resort
        /// </summary>
        public static void ForceShutdown()
        {
            LogWarning("!!! FORCE SHUTDOWN - Terminating immediately !!!", "Shutdown");
            try
            {
                var app = System.Windows.Application.Current;
                if (app != null)
                {
                    app.Dispatcher.Invoke(() => app.Shutdown(1));
                }
                else
                {
                    Environment.Exit(1);
                }
            }
            catch
            {
                Environment.Exit(1);
            }
        }
    }
}
