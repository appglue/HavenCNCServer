using System;
using System.Threading;
using System.Threading.Tasks;
using HavenCNCServer.Centroid;
using HavenCNCServer.Centroid.Events;
using HavenCNCServer.Services;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer
{
    /// <summary>
    /// Entry point for the WPF UI version of HavenCNC Server
    /// </summary>
    internal static class ProgramWPF
    {
        private static CancellationTokenSource? _cancellationTokenSource;

        /// <summary>
        /// Cancel all background operations (called during shutdown)
        /// </summary>
        public static void CancelAllOperations()
        {
            _cancellationTokenSource?.Cancel();
        }

        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting HavenCNC Server (WPF UI)...");

                // Create cancellation token for coordinated shutdown
                _cancellationTokenSource = new CancellationTokenSource();

                // Initialize the same backend services as WinForms
                Console.WriteLine("Initializing backend services...");
                InitializeBackend();

                // Start the WPF application
                Console.WriteLine("Creating WPF application...");
                var app = new WPF.App();

                Console.WriteLine("Initializing WPF components...");
                app.InitializeComponent();

                // Handle app shutdown
                app.Exit += App_Exit;

                // Handle unhandled exceptions
                app.DispatcherUnhandledException += (s, e) =>
                {
                    Console.WriteLine($"Unhandled exception: {e.Exception.Message}");
                    Console.WriteLine($"Inner exception: {e.Exception.InnerException?.Message}");
                    Console.WriteLine($"Stack trace: {e.Exception.StackTrace}");
                    if (e.Exception.InnerException != null)
                    {
                        Console.WriteLine($"Inner stack trace: {e.Exception.InnerException.StackTrace}");
                    }
                    Console.ReadLine(); // Pause to read error
                    e.Handled = false; // Let it crash so we can see the dialog too
                };

                // Create MainWindow manually after backend is initialized
                Console.WriteLine("Creating MainWindow...");
                var mainWindow = new WPF.MainWindow();
                app.MainWindow = mainWindow;
                mainWindow.Show();

                Console.WriteLine("Running WPF application...");
                app.Run();

                Console.WriteLine("WPF application exited normally.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error starting WPF UI: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.ReadLine(); // Pause to read error
                Environment.Exit(1);
            }
        }

        private static void InitializeBackend()
        {
            try
            {
                // Initialize CNCEventBus early to avoid lazy initialization during UI creation
                Console.WriteLine("Initializing CNC Event Bus...");
                _ = HavenCNCServer.Centroid.Events.CNCEventBus.Instance;
                Console.WriteLine("CNC Event Bus initialized successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize CNC Event Bus: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            try
            {
                // Initialize application settings
                SettingsManager.LoadSettings();
                LogSuccess($"Settings loaded from: {SettingsManager.GetSettingsFilePath()}", "Settings");
                LogInfo($"Temp files directory: {SettingsManager.Settings.Files.TempFilesDirectory}", "Settings");
                LogInfo($"CNC programs directory: {SettingsManager.GetCncProgramsDirectory()}", "Settings");

                // Ensure CNC programs directory exists
                var cncProgramsDir = SettingsManager.GetCncProgramsDirectory();
                if (!Directory.Exists(cncProgramsDir))
                {
                    Directory.CreateDirectory(cncProgramsDir);
                    LogSuccess($"Created CNC programs directory: {cncProgramsDir}", "Settings");
                }

                // Clean up old job files from previous sessions
                CleanupOldJobFiles();
            }
            catch (Exception ex)
            {
                LogWarning($"Settings initialization failed: {ex.Message}", "Settings");
            }

            try
            {
                // Initialize MachinePositionService to listen for DRO events
                MachinePositionService.Initialize();
                LogInfo("MachinePositionService initialized", "System");
            }
            catch (Exception ex)
            {
                LogError($"Failed to initialize MachinePositionService: {ex.Message}", "System");
            }

            try
            {
                // Deploy scripts on startup (same as MainForm does)
                ScriptDeploymentService.DeployScriptsToCnc12();
                LogSuccess("Scripts deployed successfully", "Scripts");
            }
            catch (Exception ex)
            {
                LogWarning($"Script deployment failed: {ex.Message}", "Scripts");
            }

            try
            {
                // Subscribe to CNC connection status changes
                CNCConnectionManager.ConnectionStatusChanged += OnCNCConnectionStatusChanged;

                // Force connection for WPF (unlike WinForms, WPF doesn't have manual connect UI)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(500); // Brief delay to let services initialize
                        LogInfo("Attempting to connect to CNC12...", "CNC");
                        await CNCConnectionManager.ConnectAsync();
                    }
                    catch (Exception ex)
                    {
                        LogError($"CNC connection failed: {ex.Message}", "CNC");
                    }
                });
            }
            catch (Exception ex)
            {
                LogWarning($"CNC connection initialization failed: {ex.Message}", "CNC");
            }

            try
            {
                // Start the API server
                var cancellationToken = _cancellationTokenSource?.Token ?? CancellationToken.None;
                _ = Task.Run(async () => await ApiManager.StartAsync(cancellationToken));
                LogInfo("API server starting...", "API");
            }
            catch (Exception ex)
            {
                LogError($"Failed to start API server: {ex.Message}", "API");
            }

            try
            {
                // Start job listener with background monitoring
                var cancellationToken = _cancellationTokenSource?.Token ?? CancellationToken.None;
                CentroidEventBridge.Start(cancellationToken);
                LogInfo("Centroid event bridge started", "CNC");
            }
            catch (Exception ex)
            {
                LogError($"Failed to start Centroid event bridge: {ex.Message}", "CNC");
            }

            // Set up SignalR event listeners asynchronously after API is fully ready
            _ = Task.Run(async () =>
            {
                try
                {
                    // Wait for API to be fully initialized
                    await Task.Delay(3000);
                    SignalRManager.SetupEventListeners();
                    LogInfo("SignalR event listeners configured", "SignalR");
                }
                catch (Exception ex)
                {
                    LogError($"Failed to setup SignalR listeners: {ex.Message}", "SignalR");
                }
            });
        }

        private static void OnCNCConnectionStatusChanged(bool connected, string message)
        {
            if (connected)
            {
                LogSuccess(message, "CNC");
            }
            else
            {
                LogWarning(message, "CNC");
            }
        }

        /// <summary>
        /// Clean up old job files from previous sessions on startup
        /// </summary>
        private static void CleanupOldJobFiles()
        {
            try
            {
                var cncProgramsDir = SettingsManager.GetCncProgramsDirectory();
                if (!Directory.Exists(cncProgramsDir))
                {
                    LogInfo($"CNC programs directory does not exist yet: {cncProgramsDir}", "Cleanup");
                    return;
                }

                // Find all job files (pattern: job_*.nc)
                var jobFiles = Directory.GetFiles(cncProgramsDir, "job_*.nc");

                if (jobFiles.Length == 0)
                {
                    LogInfo("No old job files to clean up", "Cleanup");
                    return;
                }

                int deletedCount = 0;
                foreach (var filePath in jobFiles)
                {
                    try
                    {
                        File.Delete(filePath);
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        LogWarning($"Failed to delete old job file {Path.GetFileName(filePath)}: {ex.Message}", "Cleanup");
                    }
                }

                LogSuccess($"Cleaned up {deletedCount} old job file(s) from {cncProgramsDir}", "Cleanup");
            }
            catch (Exception ex)
            {
                LogWarning($"Error during job file cleanup: {ex.Message}", "Cleanup");
            }
        }

        private static void DumpActiveThreads()
        {
            try
            {
                var process = System.Diagnostics.Process.GetCurrentProcess();
                Console.WriteLine("\n========== ACTIVE THREADS DUMP ==========");
                Console.WriteLine($"Process: {process.ProcessName} (PID: {process.Id})");
                Console.WriteLine($"Total Threads: {process.Threads.Count}");
                Console.WriteLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\n");

                foreach (System.Diagnostics.ProcessThread thread in process.Threads)
                {
                    Console.WriteLine($"Thread ID: {thread.Id}");
                    Console.WriteLine($"  State: {thread.ThreadState}");
                    Console.WriteLine($"  Priority: {thread.PriorityLevel}");
                    Console.WriteLine($"  Start Time: {thread.StartTime:yyyy-MM-dd HH:mm:ss}");
                    Console.WriteLine($"  CPU Time: {thread.TotalProcessorTime}");
                    Console.WriteLine($"  Wait Reason: {(thread.ThreadState == System.Diagnostics.ThreadState.Wait ? thread.WaitReason.ToString() : "N/A")}");
                    Console.WriteLine();
                }

                Console.WriteLine("=========================================\n");

                // Also dump managed threads
                Console.WriteLine("\n========== MANAGED THREADS ==========\n");
                var managedThreads = System.Diagnostics.Process.GetCurrentProcess().Threads;
                Console.WriteLine($"Active managed thread pool threads: {System.Threading.ThreadPool.ThreadCount}");
                Console.WriteLine($"Pending work items: {System.Threading.ThreadPool.PendingWorkItemCount}");
                Console.WriteLine("\n====================================\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error dumping thread info: {ex.Message}");
            }
        }

        private static void App_Exit(object? sender, System.Windows.ExitEventArgs e)
        {
            try
            {
                LogInfo("Application shutdown initiated", "System");
                Console.WriteLine("\n*** SHUTDOWN STARTED - Dumping active threads ***\n");
                DumpActiveThreads();

                // Set a global timeout for shutdown
                var shutdownTimer = new System.Timers.Timer(10000); // 10 seconds max
                shutdownTimer.Elapsed += (s, args) =>
                {
                    LogWarning("Shutdown timeout reached, forcing exit", "System");
                    Environment.Exit(0);
                };
                shutdownTimer.AutoReset = false;
                shutdownTimer.Start();

                // Signal all background operations to stop
                _cancellationTokenSource?.Cancel();

                // Stop Centroid Event Bridge
                var shutdownToken = new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token;
                CentroidEventBridge.Stop(shutdownToken);

                // Dispose CNCEventBus to stop worker threads
                try
                {
                    LogInfo("Disposing CNC Event Bus...", "System");
                    HavenCNCServer.Centroid.Events.CNCEventBus.Instance.Dispose();
                    LogInfo("CNC Event Bus disposed", "System");
                }
                catch (Exception ex)
                {
                    LogError($"Error disposing CNC Event Bus: {ex.Message}", "System");
                }

                // Event bus subscribers will be cleaned up automatically via Dispose

                // Stop API manager
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                        await ApiManager.StopAsync(timeoutSource.Token);
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error stopping API manager: {ex.Message}", "System");
                    }
                }).Wait(3500);

                // Unsubscribe from CNC events
                CNCConnectionManager.ConnectionStatusChanged -= OnCNCConnectionStatusChanged;

                // Cleanup the CNC connection manager
                CNCConnectionManager.Disconnect();

                _cancellationTokenSource?.Dispose();

                shutdownTimer.Stop();
                shutdownTimer.Dispose();

                Console.WriteLine("\n*** SHUTDOWN CLEANUP COMPLETE - Dumping remaining threads ***\n");
                DumpActiveThreads();

                LogSuccess("Application shutdown completed", "System");

                // Give console time to display final messages
                System.Threading.Thread.Sleep(500);

                // Force process exit since WPF shutdown is already in progress
                Console.WriteLine("\n*** Forcing process exit ***\n");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                LogError($"Error during shutdown: {ex.Message}", "System");
                Console.WriteLine($"\n*** Error during shutdown, forcing exit: {ex.Message} ***\n");
                // Force exit on error
                Environment.Exit(1);
            }
        }
    }
}
