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

        private static void App_Exit(object? sender, System.Windows.ExitEventArgs e)
        {
            try
            {
                LogInfo("Application shutdown initiated", "System");

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

                LogSuccess("Application shutdown completed", "System");
            }
            catch (Exception ex)
            {
                LogError($"Error during shutdown: {ex.Message}", "System");
            }
        }
    }
}
