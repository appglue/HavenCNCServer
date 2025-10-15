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
            try
            {
                LogInfo("Application shutdown initiated", "System");

                // Create a separate shutdown cancellation token with timeout
                using var shutdownTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var shutdownToken = shutdownTokenSource.Token;

                // Signal all background operations to stop gracefully
                _cancellationTokenSource?.Cancel();

                // Stop CNC Job Info Listener
                LogInfo("Stopping CNC Job Info Listener...", "System");
                CNCJobInfoListener.Stop(shutdownToken);

                // Clear all event listeners to prevent callbacks during shutdown
                CNCJobInfoListener.ClearAllListeners();

                // Stop CNC server manager
                LogInfo("Stopping CNC server manager...", "System");
                try
                {
                    var stopTask = Task.Run(async () => await CNCServerManager.StopAsync(shutdownToken));
                    if (!stopTask.Wait(3000))
                    {
                        LogWarning("CNC server manager stop operation timed out after 3 seconds", "System");
                    }
                    else
                    {
                        LogInfo("CNC server manager stopped successfully", "System");
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Error stopping CNC server manager: {ex.Message}", "System");
                }

                // Stop API manager
                LogInfo("Stopping API manager...", "System");
                try
                {
                    var stopTask = Task.Run(async () => await ApiManager.StopAsync(shutdownToken));
                    if (!stopTask.Wait(3000))
                    {
                        LogWarning("API manager stop operation timed out after 3 seconds", "System");
                    }
                    else
                    {
                        LogInfo("API manager stopped successfully", "System");
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Error stopping API manager: {ex.Message}", "System");
                }

                // Unsubscribe from CNC events
                CNCConnectionManager.ConnectionStatusChanged -= OnCNCConnectionStatusChanged;

                // Cleanup the CNC connection manager
                LogInfo("Disconnecting CNC connection...", "System");
                CNCConnectionManager.Disconnect();

                // Wait a moment for any final cleanup (reduced from 500ms)
                Thread.Sleep(200);

                // Force garbage collection to clean up any remaining resources
                GC.Collect();
                GC.WaitForPendingFinalizers();

                LogInfo("Application shutdown completed", "System");
            }
            catch (Exception ex)
            {
                LogError($"Error during shutdown cleanup: {ex.Message}", "System");
            }
            finally
            {
                // Dispose cancellation token source
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                
                Environment.Exit(0);
            }

            base.OnFormClosing(e);
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
