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
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer
{
    /// <summary>
    /// Main Windows Forms application that hosts the ASP.NET Core Web API server
    /// </summary>
    public partial class MainForm : Form
    {
        private IHost? _webHost;
        private CancellationTokenSource? _cancellationTokenSource;
        private ICNCServerManager? _cncServerManager;
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
            await StartApiServerAsync();
            
            // Start job listener auto-start task after API server is running
            _ = Task.Run(async () =>
            {
                LogInfo("Starting background job listener monitoring...", "JobInfo");
                
                // Actively try to establish CNC connection for job listener
                var maxAttempts = 5;
                var attempt = 0;
                
                while (attempt < maxAttempts && !(_cancellationTokenSource?.Token.IsCancellationRequested ?? false))
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
                        await Task.Delay(3000, _cancellationTokenSource?.Token ?? CancellationToken.None);
                    }
                }
                
                if (attempt >= maxAttempts)
                {
                    LogWarning("All CNC connection attempts failed - will retry periodically", "JobInfo");
                }
                
                // Continue monitoring and retry if connection is lost
                while (!_cancellationTokenSource?.Token.IsCancellationRequested ?? false)
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
                        await Task.Delay(15000, _cancellationTokenSource?.Token ?? CancellationToken.None);
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

        private async Task StartApiServerAsync()
        {
            try
            {
                LogInfo("Initializing API server...", "API");
                UpdateStatus("Starting API Server...", Color.Orange);

                _cancellationTokenSource = new CancellationTokenSource();

                var builder = Host.CreateDefaultBuilder()
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder
                            .UseUrls(ApiUrl)
                            .UseStartup<ApiStartup>()
                            .ConfigureLogging(logging =>
                            {
                                logging.ClearProviders();
                                logging.AddProvider(new WinFormsLoggerProvider(this));
                            });
                    });

                _webHost = builder.Build();

                // Start the web host in a background task
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _webHost.RunAsync(_cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when cancellation is requested
                    }
                    catch (Exception ex)
                    {
                        this.Invoke(() =>
                        {
                            LogError($"Error running web host: {ex.Message}", "API");
                            UpdateStatus("API Server Error", Color.Red);
                        });
                    }
                });

                // Give the server a moment to start
                await Task.Delay(2000);

                UpdateStatus("API Server Running", Color.Green);
                LogSuccess($"API server started successfully at {ApiUrl}", "API");
                LogInfo($"Swagger UI available at {SwaggerUrl}", "API");

                // Get the CNC Server Manager from DI and start management (auto-start is enabled)
                _cncServerManager = _webHost.Services.GetService<ICNCServerManager>();
                if (_cncServerManager != null)
                {
                    await _cncServerManager.StartManagementAsync();
                    LogInfo("CNC Server Manager started with auto-start enabled", "CNCServer");
                }
                else
                {
                    LogWarning("CNC Server Manager not found in DI container", "CNCServer");
                }

                // Auto-generate OpenAPI specification if it doesn't exist
                await OpenApiManager.AutoGenerateIfNeededAsync(ApiUrl);
            }
            catch (Exception ex)
            {
                UpdateStatus("Failed to Start", Color.Red);
                LogError($"Failed to start API server: {ex.Message}", "API");
            }
        }

        private async Task StopApiServerAsync()
        {
            try
            {
                UpdateStatus("Stopping API Server...", Color.Orange);
                LogInfo("Stopping API server...", "API");

                // Stop CNC Server Manager first
                if (_cncServerManager != null)
                {
                    await _cncServerManager.StopManagementAsync();
                    LogInfo("CNC Server Manager stopped", "CNCServer");
                    _cncServerManager = null;
                }

                _cancellationTokenSource?.Cancel();
                
                if (_webHost != null)
                {
                    await _webHost.StopAsync();
                    _webHost.Dispose();
                    _webHost = null;
                }

                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                UpdateStatus("API Server Stopped", Color.Gray);
                LogSuccess("API server stopped successfully", "API");
            }
            catch (Exception ex)
            {
                LogError($"Error stopping API server: {ex.Message}", "API");
            }
        }

        private void UpdateStatus(string status, Color color)
        {
            if (InvokeRequired)
            {
                Invoke(() => UpdateStatus(status, color));
                return;
            }

            lblStatus.Text = $"API Server Status: {status}";
            lblStatus.ForeColor = color;
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
        /// Cleanup when form is closing
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                LogInfo("Application shutdown initiated", "System");

                // Cancel all background tasks first
                _cancellationTokenSource?.Cancel();

                // Stop CNC Job Info Listener
                LogInfo("Stopping CNC Job Info Listener...", "System");
                CNCJobInfoListener.StopListening();

                // Clear all event listeners to prevent callbacks during shutdown
                CNCJobInfoListener.ClearAllListeners();

                // Stop web host first
                if (_webHost != null)
                {
                    LogInfo("Stopping web host...", "System");
                    try
                    {
                        var stopWebTask = Task.Run(async () => 
                        {
                            await _webHost.StopAsync(TimeSpan.FromSeconds(3));
                            _webHost.Dispose();
                        });

                        if (!stopWebTask.Wait(5000))
                        {
                            LogWarning("Web host stop operation timed out after 5 seconds", "System");
                        }
                        else
                        {
                            LogInfo("Web host stopped successfully", "System");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error stopping web host: {ex.Message}", "System");
                    }
                    finally
                    {
                        _webHost = null;
                    }
                }

                // Stop CNC Server Manager if it's still running
                if (_cncServerManager != null)
                {
                    LogInfo("Stopping CNC Server Manager...", "System");
                    try
                    {
                        var stopTask = Task.Run(async () => await _cncServerManager.StopManagementAsync());
                        if (!stopTask.Wait(5000))
                        {
                            LogWarning("CNC Server Manager stop operation timed out after 5 seconds", "System");
                        }
                        else
                        {
                            LogInfo("CNC Server Manager cleanup completed", "System");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error stopping CNC Server Manager: {ex.Message}", "System");
                    }
                    finally
                    {
                        _cncServerManager = null;
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
                // Ensure cancellation token is disposed
                try
                {
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = null;
                }
                catch (Exception ex)
                {
                    LogError($"Error disposing cancellation token: {ex.Message}", "System");
                }
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
