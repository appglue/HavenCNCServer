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
        private ApiManager? _apiManager;
        private const string ApiUrl = "http://localhost:5000";
        private const string SwaggerUrl = "http://localhost:5000/swagger";
        private const string ReactAppUrl = "http://localhost:5000"; // Now served by the embedded server

        /// <summary>
        /// Initializes a new instance of the MainForm class
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            
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
            
            // Initialize API manager
            _apiManager = new ApiManager(ApiUrl);
            _apiManager.StatusChanged += OnApiStatusChanged;
            
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
            await _apiManager?.StartAsync()!;
            
            // Start job listener auto-start task after API server is running
            _ = Task.Run(async () =>
            {
                LogInfo("Starting background job listener monitoring...", "JobInfo");
                
                // Actively try to establish CNC connection for job listener
                var maxAttempts = 5;
                var attempt = 0;
                
                while (attempt < maxAttempts && !(_apiManager?.CancellationToken.IsCancellationRequested ?? false))
                {
                    attempt++;
                    LogInfo($"Attempting to establish CNC connection for job listener (attempt {attempt}/{maxAttempts})...", "JobInfo");
                    
                    try
                    {
                        // Try to get or create a new CNC pipe connection
                        var pipe = CNCConnectionManager.GetOrCreateCNCPipe();
                        
                        if (pipe != null && pipe.IsConstructed())
                        {
                            LogSuccess("CNC pipe connected - starting job listener", "JobInfo");
                            CNCJobInfoListener.AutoStartIfConnected();
                            break;
                        }
                        else
                        {
                            LogWarning($"CNC pipe connection attempt {attempt} failed", "JobInfo");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogWarning($"CNC connection attempt {attempt} error: {ex.Message}", "JobInfo");
                    }
                    
                    if (attempt < maxAttempts)
                    {
                        // Wait before next attempt
                        await Task.Delay(3000, _apiManager?.CancellationToken ?? CancellationToken.None);
                    }
                }
                
                if (attempt >= maxAttempts)
                {
                    LogWarning("All CNC connection attempts failed - will retry periodically", "JobInfo");
                }
                
                // Continue monitoring and retry if connection is lost
                while (!(_apiManager?.CancellationToken.IsCancellationRequested ?? false))
                {
                    try
                    {
                        // Check if we need to establish or re-establish connection
                        if (!CNCConnectionManager.IsConnected || !CNCJobInfoListener.IsListening)
                        {
                            if (!CNCConnectionManager.IsConnected)
                            {
                                LogInfo("CNC not connected - attempting to establish connection...", "JobInfo");
                                
                                try
                                {
                                    var pipe = CNCConnectionManager.GetOrCreateCNCPipe();
                                    if (pipe != null && pipe.IsConstructed())
                                    {
                                        LogSuccess("CNC pipe reconnected", "JobInfo");
                                    }
                                }
                                catch (Exception connEx)
                                {
                                    LogWarning($"CNC reconnection failed: {connEx.Message}", "JobInfo");
                                }
                            }
                            
                            // Try to start job listener if connected but not running
                            if (CNCConnectionManager.IsConnected && !CNCJobInfoListener.IsListening)
                            {
                                LogInfo("Starting job listener...", "JobInfo");
                                CNCJobInfoListener.AutoStartIfConnected();
                            }
                        }
                        
                        // Check every 15 seconds
                        await Task.Delay(15000, _apiManager?.CancellationToken ?? CancellationToken.None);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error in job listener monitoring: {ex.Message}", "JobInfo");
                        await Task.Delay(5000); // Wait a bit on error
                    }
                }
                
                LogInfo("Job listener monitoring stopped", "JobInfo");
            });
        }

        /// <summary>
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

                // Stop CNC Job Info Listener
                LogInfo("Stopping CNC Job Info Listener...", "System");
                CNCJobInfoListener.StopListening();

                // Clear all event listeners to prevent callbacks during shutdown
                CNCJobInfoListener.ClearAllListeners();

                // Stop API manager
                if (_apiManager != null)
                {
                    LogInfo("Stopping API manager...", "System");
                    try
                    {
                        var stopTask = Task.Run(async () => await _apiManager.StopAsync());
                        if (!stopTask.Wait(5000))
                        {
                            LogWarning("API manager stop operation timed out after 5 seconds", "System");
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
                    finally
                    {
                        _apiManager?.Dispose();
                        _apiManager = null;
                    }
                }

                // Unsubscribe from CNC events
                CNCConnectionManager.ConnectionStatusChanged -= OnCNCConnectionStatusChanged;

                // Cleanup the CNC connection manager
                LogInfo("Disconnecting CNC connection...", "System");
                CNCConnectionManager.Disconnect();

                // Wait a moment for any final cleanup
                Thread.Sleep(500);

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
