using System;
using System.IO;
using System.Runtime.InteropServices;
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

        // Import AllocConsole from kernel32.dll to create console window
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int dwProcessId);

        private const int ATTACH_PARENT_PROCESS = -1;

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
                // Parse command line arguments
                bool launchBrowser = true;
                foreach (var arg in args)
                {
                    if (arg.Equals("--no-browser", StringComparison.OrdinalIgnoreCase) ||
                        arg.Equals("-nb", StringComparison.OrdinalIgnoreCase))
                    {
                        launchBrowser = false;
                    }
                }

                // Ensure console is available for logging (WPF apps don't have console by default)
                if (!AttachConsole(ATTACH_PARENT_PROCESS))
                {
                    AllocConsole();
                }

                Console.WriteLine("==========================================================");
                Console.WriteLine("HavenCNC Server - Console Output Enabled");
                Console.WriteLine("==========================================================");
                Console.WriteLine("Starting HavenCNC Server (WPF UI)...");
                if (!launchBrowser)
                {
                    Console.WriteLine("Browser UI auto-launch disabled via command line");
                }

                // Create cancellation token for coordinated shutdown
                _cancellationTokenSource = new CancellationTokenSource();

                // Register with shutdown manager
                ShutdownManager.RegisterCancellationSource(_cancellationTokenSource);

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

                // Auto-launch browser UI if enabled
                if (launchBrowser)
                {
                    Console.WriteLine("Auto-launching browser UI...");
                    // Delay slightly to let the main window and API initialize
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(2000);
                        app.Dispatcher.Invoke(() =>
                        {
                            var browserWindow = new WPF.Views.BrowserWindow();
                            browserWindow.Show();
                        });
                    });
                }

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

            // MongoDB sync now handled automatically by MachineConfigurationController.CheckAndPerformInitialSync()
            // Runs 2 seconds after controller initialization with version-based comparison

            try
            {
                // Subscribe to CNC connection status changes
                CNCConnectionManager.ConnectionStatusChanged += OnCNCConnectionStatusChanged;

                // Check if CNC12 is running by checking the process directly (not cached)
                var cnc12ProcessName = SettingsManager.Settings.Cnc.Cnc12ProcessName;
                var processes = System.Diagnostics.Process.GetProcessesByName(cnc12ProcessName);
                bool isCnc12Running = processes.Length > 0;

                // Clean up process handles
                foreach (var p in processes)
                {
                    try { p.Dispose(); } catch { }
                }

                if (!isCnc12Running)
                {
                    LogInfo("CNC12 is not running - attempting to start...", "CNC");
                    if (CNCUtils.LaunchCNC12())
                    {
                        LogSuccess("CNC12 started successfully", "CNC");
                        // Give CNC12 time to fully initialize before attempting connection
                        Thread.Sleep(3000);
                    }
                    else
                    {
                        LogError("Failed to start CNC12 - connection may not be possible", "CNC");
                    }
                }
                else
                {
                    LogInfo("CNC12 is already running", "CNC");
                }

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

        /// <summary>
        /// Cleanup that must happen BEFORE WPF shutdown to avoid WeakEventTable errors
        /// Now uses centralized ShutdownManager
        /// </summary>
        public static void CleanupBeforeShutdown()
        {
            // Unsubscribe from CNC events to prevent WeakEventTable errors during shutdown
            try
            {
                CNCConnectionManager.ConnectionStatusChanged -= OnCNCConnectionStatusChanged;
            }
            catch (Exception ex)
            {
                LogError($"Error unsubscribing CNC events: {ex.Message}", "System");
            }

            // Delegate to centralized shutdown manager
            ShutdownManager.InitiateShutdown(maxShutdownTimeSeconds: 5);
        }

        private static void App_Exit(object? sender, System.Windows.ExitEventArgs e)
        {
            // If shutdown already in progress, just exit
            if (ShutdownManager.IsShuttingDown)
            {
                LogInfo("App_Exit: Shutdown already in progress via ShutdownManager", "System");
                return;
            }

            // Initiate shutdown via centralized manager
            LogInfo("App_Exit: Initiating shutdown via ShutdownManager", "System");
            ShutdownManager.InitiateShutdown(maxShutdownTimeSeconds: 5);
        }
    }
}
