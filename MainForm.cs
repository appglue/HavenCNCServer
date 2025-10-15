using CentroidAPI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using HavenCNCServer.Services;
using HavenCNCServer.Centriod.Events;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer
{
    /// <summary>
    /// Main Windows Forms application that hosts the ASP.NET Core Web API server
    /// </summary>
    public partial class MainForm : Form
    {
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _startupComplete = false;
        private readonly object _startupLock = new object();
        
        private const string ApiUrl = "http://localhost:5000";
        private const string SwaggerUrl = "http://localhost:5000/swagger";
        private const string ReactAppUrl = "http://localhost:5000"; // Now served by the embedded server

        /// <summary>
        /// Initializes a new instance of the MainForm class
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            
            // Initialize cancellation token source for coordinated shutdown
            _cancellationTokenSource = new CancellationTokenSource();
            
            // Set up 50/50 layout for messages and logs
            SetupLayout();
            
            // Set up centralized logging
            SetupLogging();
            
            // Initialize application settings
            try
            {
                SettingsManager.LoadSettings();
                LogSuccess($"Settings loaded from: {SettingsManager.GetSettingsFilePath()}", "Settings");
                LogInfo($"Temp files directory: {SettingsManager.Settings.Files.TempFilesDirectory}", "Settings");
                LogInfo($"CNC programs directory: {SettingsManager.GetCncProgramsDirectory()}", "Settings");
                
                // Subscribe to CNC connection status changes
                CNCConnectionManager.ConnectionStatusChanged += OnCNCConnectionStatusChanged;
                
                // Try auto-connect if enabled
                _ = Task.Run(async () => await CNCConnectionManager.TryAutoConnectAsync());
            }
            catch (Exception ex)
            {
                LogWarning($"Settings initialization failed: {ex.Message}", "Settings");
            }
            
            // Register this form with the UI control service
            Services.UIControlService.RegisterMainForm(this);
            
            // Subscribe to status change events
            ApiManager.StatusChanged += OnApiStatusChanged;
            
            // Start the API server automatically when the form loads
            this.Load += MainForm_Load;
            this.Resize += MainForm_Resize;
        }

        /// <summary>
        /// Set up the three-column layout for logs, messages, and G-code
        /// </summary>
        private void SetupLayout()
        {
            // Subscribe to resize event to maintain proper layout
            this.Resize += MainForm_Resize;
            
            // Initial layout setup will be done in resize handler
            MainForm_Resize(null, EventArgs.Empty);
        }

        /// <summary>
        /// Handle form resize to maintain three-column split between logs, messages, and G-code
        /// </summary>
        private void MainForm_Resize(object? sender, EventArgs e)
        {
            if (txtLog != null && txtMessages != null && txtGCode != null && pnlControls != null)
            {
                var availableWidth = this.ClientSize.Width - 36; // Account for margins
                var columnWidth = availableWidth / 3 - 8; // Account for gaps between controls
                
                // Update log section (left column)
                txtLog.Width = columnWidth;
                
                // Update messages section (center column)
                txtMessages.Left = txtLog.Right + 12; // 12px gap
                txtMessages.Width = columnWidth;
                
                // Update G-code section (right column)
                txtGCode.Left = txtMessages.Right + 12; // 12px gap
                txtGCode.Width = columnWidth;
                
                // Update labels accordingly
                if (lblMessages != null)
                {
                    lblMessages.Left = txtMessages.Left;
                }
                
                if (lblGCode != null)
                {
                    lblGCode.Left = txtGCode.Left;
                }
                
                if (lblCurrentJob != null)
                {
                    lblCurrentJob.Left = txtGCode.Left;
                }
            }
        }

        /// <summary>
        /// Set up the centralized logging system
        /// </summary>
        private void SetupLogging()
        {
            // Create and register a log target for the main form's text box
            var logTarget = new RichTextBoxLogTarget(txtLog, this);
            LoggingService.AddTarget(logTarget);
            
            // Set maximum log entries from settings or default
            LoggingService.MaxLogEntries = 2000;
            
            LogInfo("Logging system initialized", "System");
        }

        /// <summary>
        /// Load G-code into the display panel (placeholder for component integration)
        /// </summary>
        public void LoadGCodeForDisplay(string[] gcode)
        {
            // TODO: Delegate to GCodeViewerComponent when components are integrated
            LogInfo($"LoadGCodeForDisplay called with {gcode?.Length ?? 0} lines", "GCodeDisplay");
        }

        private async void MainForm_Load(object? sender, EventArgs e)
        {
            var cancellationToken = _cancellationTokenSource?.Token ?? CancellationToken.None;
            
            await ApiManager.StartAsync(cancellationToken);
            
            // Start CNC server management
            await CNCServerManager.StartAsync(cancellationToken);
            LogInfo("CNC Server Manager started", "CNCServer");
            
            // Start job listener with background monitoring
            CNCJobInfoListener.Start(cancellationToken);
            
            // Mark startup as complete
            lock (_startupLock)
            {
                _startupComplete = true;
                LogSuccess("🚀 Application startup completed successfully", "System");
            }
        }        /// <summary>
        /// Handle CNC connection status changes
        /// </summary>
        private void OnCNCConnectionStatusChanged(bool connected, string message)
        {
            // Use Invoke to ensure we're on the UI thread
            if (InvokeRequired)
            {
                Invoke(() => OnCNCConnectionStatusChanged(connected, message));
                return;
            }

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
        /// Handles API server status changes from ApiManager
        /// </summary>
        private void OnApiStatusChanged(string status, Color color)
        {
            // Use Invoke to ensure we're on the UI thread
            if (InvokeRequired)
            {
                Invoke(() => OnApiStatusChanged(status, color));
                return;
            }

            // TODO: Update status label when available
            // For now, just log the status change
            LogInfo($"API server status: {status}", "API");
        }

        /// <summary>
        /// Cleanup when form is closing
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Check if startup is complete before allowing shutdown
            lock (_startupLock)
            {
                if (!_startupComplete)
                {
                    LogWarning("⏳ Startup still in progress - delaying shutdown to prevent race conditions", "System");
                    e.Cancel = true;
                    
                    // Schedule a retry in 500ms
                    var retryTimer = new System.Windows.Forms.Timer();
                    retryTimer.Interval = 500;
                    retryTimer.Tick += (s, args) =>
                    {
                        retryTimer.Stop();
                        retryTimer.Dispose();
                        this.Close(); // Try closing again
                    };
                    retryTimer.Start();
                    return;
                }
            }
            
            // Cancel the close to handle it asynchronously
            if (!e.Cancel)
            {
                e.Cancel = true;
                
                LogInfo("🔄 Starting async shutdown process...", "System");
                
                // Run shutdown asynchronously but with better error handling and timeout
                _ = Task.Run(async () =>
                {
                    try
                    {
                        LogInfo("📍 Background shutdown task started", "System");
                        
                        // Add a timeout to the entire shutdown process
                        using var shutdownTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                        try
                        {
                            await PerformShutdownAsync().WaitAsync(shutdownTimeout.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            LogError("⏰ Shutdown process timed out after 10 seconds - forcing exit", "System");
                            Environment.Exit(1);
                            return;
                        }
                        
                        // // Log completion on UI thread
                        // this.Invoke(() => 
                        // {
                        //     LogInfo("🛑 Shutdown complete - waiting 5 seconds before exit for log review...", "System");
                        //     LogInfo("📋 You can now capture/review the shutdown logs", "System");
                        //     LogInfo("⏰ Application will exit automatically in 5 seconds", "System");
                        // });
                        
                        // // Wait 5 seconds (reduced from 10) before forcing exit
                        // await Task.Delay(10000);
                        
                        LogInfo("🚪 Calling Environment.Exit(0)...", "System");
                        Environment.Exit(0);
                    }
                    catch (Exception ex)
                    {
                        // Log any errors in the shutdown process
                        try
                        {
                            this.Invoke(() => 
                            {
                                LogError($"❌ Fatal error during shutdown: {ex.Message}", "System");
                                LogInfo("🚪 Forcing exit due to error...", "System");
                            });
                            await Task.Delay(2000); // Give time to see the error
                        }
                        catch
                        {
                            // If even logging fails, just exit
                        }
                        finally
                        {
                            Environment.Exit(1);
                        }
                    }
                });
            }

            base.OnFormClosing(e);
        }

        /// <summary>
        /// Perform the actual shutdown operations asynchronously
        /// </summary>
        private async Task PerformShutdownAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                LogInfo("🔄 Application shutdown initiated", "System");

                // Create a separate shutdown cancellation token with timeout
                LogInfo("⏱️ Creating shutdown cancellation token (5 second timeout)", "System");
                using var shutdownTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var shutdownToken = shutdownTokenSource.Token;

                // Signal all background operations to stop gracefully
                LogInfo("🛑 Cancelling main cancellation token to signal background operations", "System");
                _cancellationTokenSource?.Cancel();
                LogInfo($"✅ Main cancellation token cancelled ({stopwatch.ElapsedMilliseconds}ms)", "System");

                // Stop CNC Job Info Listener
                LogInfo("🔌 Starting CNC Job Info Listener shutdown...", "System");
                var listenerStopwatch = System.Diagnostics.Stopwatch.StartNew();
                CNCJobInfoListener.Stop(shutdownToken);
                LogInfo($"✅ CNC Job Info Listener stopped ({listenerStopwatch.ElapsedMilliseconds}ms)", "System");

                // Clear all event listeners to prevent callbacks during shutdown
                LogInfo("🧹 Clearing all CNC event listeners...", "System");
                CNCJobInfoListener.ClearAllListeners();
                LogInfo($"✅ Event listeners cleared ({stopwatch.ElapsedMilliseconds}ms total)", "System");

                // Stop CNC server manager
                LogInfo("🖥️ Starting CNC server manager shutdown...", "System");
                var serverStopwatch = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    LogInfo("🎯 Calling CNCServerManager.StopAsync directly (no Task.Run)...", "System");
                    
                    // Create a timeout-protected call without Task.Run to avoid thread pool issues
                    using var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    using var combinedSource = CancellationTokenSource.CreateLinkedTokenSource(shutdownToken, timeoutSource.Token);
                    
                    try
                    {
                        await CNCServerManager.StopAsync(combinedSource.Token);
                        LogInfo($"✅ CNC server manager stopped successfully ({serverStopwatch.ElapsedMilliseconds}ms)", "System");
                    }
                    catch (OperationCanceledException) when (timeoutSource.Token.IsCancellationRequested)
                    {
                        LogWarning($"⏰ CNC server manager stop timed out after 3 seconds ({serverStopwatch.ElapsedMilliseconds}ms)", "System");
                    }
                }
                catch (Exception ex)
                {
                    LogError($"❌ Error stopping CNC server manager: {ex.Message} ({serverStopwatch.ElapsedMilliseconds}ms)", "System");
                }

                // Stop API manager
                LogInfo("🌐 Starting API manager shutdown...", "System");
                var apiStopwatch = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    LogInfo("🎯 Calling ApiManager.StopAsync directly (no Task.Run)...", "System");
                    
                    // Create a timeout-protected call without Task.Run to avoid thread pool issues
                    using var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    using var combinedSource = CancellationTokenSource.CreateLinkedTokenSource(shutdownToken, timeoutSource.Token);
                    
                    try
                    {
                        await ApiManager.StopAsync(combinedSource.Token);
                        LogInfo($"✅ API manager stopped successfully ({apiStopwatch.ElapsedMilliseconds}ms)", "System");
                    }
                    catch (OperationCanceledException) when (timeoutSource.Token.IsCancellationRequested)
                    {
                        LogWarning($"⏰ API manager stop timed out after 3 seconds ({apiStopwatch.ElapsedMilliseconds}ms)", "System");
                    }
                }
                catch (Exception ex)
                {
                    LogError($"❌ Error stopping API manager: {ex.Message} ({apiStopwatch.ElapsedMilliseconds}ms)", "System");
                }

                // Unsubscribe from CNC events
                LogInfo("📤 Unsubscribing from CNC connection events...", "System");
                CNCConnectionManager.ConnectionStatusChanged -= OnCNCConnectionStatusChanged;
                LogInfo($"✅ Event unsubscription completed ({stopwatch.ElapsedMilliseconds}ms total)", "System");

                // Cleanup the CNC connection manager
                LogInfo("🔌 Disconnecting CNC connection...", "System");
                var disconnectStopwatch = System.Diagnostics.Stopwatch.StartNew();
                CNCConnectionManager.Disconnect();
                LogInfo($"✅ CNC connection disconnected ({disconnectStopwatch.ElapsedMilliseconds}ms)", "System");

                // Wait a moment for any final cleanup (reduced from 500ms)
                LogInfo("⏳ Final cleanup wait (200ms)...", "System");
                await Task.Delay(200);
                LogInfo($"✅ Final cleanup wait completed ({stopwatch.ElapsedMilliseconds}ms total)", "System");

                // Force garbage collection to clean up any remaining resources
                LogInfo("🗑️ Running garbage collection...", "System");
                var gcStopwatch = System.Diagnostics.Stopwatch.StartNew();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                LogInfo($"✅ Garbage collection completed ({gcStopwatch.ElapsedMilliseconds}ms)", "System");

                LogInfo($"🏁 Application shutdown completed successfully (Total: {stopwatch.ElapsedMilliseconds}ms)", "System");
            }
            catch (Exception ex)
            {
                LogError($"❌ Error during shutdown cleanup: {ex.Message} (Total time: {stopwatch.ElapsedMilliseconds}ms)", "System");
            }
            finally
            {
                LogInfo("🧹 Final cleanup: disposing cancellation token source...", "System");
                // Dispose cancellation token source
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                LogInfo($"✅ Cancellation token disposed (Total time: {stopwatch.ElapsedMilliseconds}ms)", "System");
            }
        }

        private void btnOpenSwagger_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = SwaggerUrl,
                    UseShellExecute = true
                });
                LogInfo("Opened Swagger UI in browser", "UI");
            }
            catch (Exception ex)
            {
                LogError($"Failed to open Swagger UI: {ex.Message}", "UI");
                MessageBox.Show($"Failed to open Swagger UI: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnGCodeTest_Click(object sender, EventArgs e)
        {
            try
            {
                LogInfo("Opening G-Code Test Dialog...", "UI");
                
                using (var gCodeDialog = new GCodeTestDialog(this))
                {
                    gCodeDialog.ShowDialog(this);
                }
                
                LogInfo("G-Code Test Dialog closed.", "UI");
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error opening G-Code Test Dialog: {ex.Message}";
                LogError(errorMessage, "UI");
                MessageBox.Show(errorMessage, "Dialog Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}
