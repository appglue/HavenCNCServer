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
        private CoordinateDisplayListener? _coordinateListener;
        private CNCMessageDisplayListener? _messageListener;
        private GCodeDisplayListener? _gcodeListener;
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
            
            // Set up coordinate display listener
            SetupCoordinateDisplay();
            
            // Set up CNC message display listener
            SetupMessageDisplay();
            
            // Set up G-code display listener
            SetupGCodeDisplay();
            
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
        /// Set up coordinate display listener for machine position updates
        /// </summary>
        private void SetupCoordinateDisplay()
        {
            try
            {
                // Create coordinate display listener
                _coordinateListener = new CoordinateDisplayListener(this);
                
                // Register listener with CNC job info listener
                CNCJobInfoListener.AddListener(_coordinateListener);
                
                LogInfo("Coordinate display listener registered", "CoordinateDisplay");
            }
            catch (Exception ex)
            {
                LogError($"Failed to setup coordinate display: {ex.Message}", "CoordinateDisplay");
            }
        }

        /// <summary>
        /// Set up CNC message display listener for showing classified messages
        /// </summary>
        private void SetupMessageDisplay()
        {
            try
            {
                // Create message display listener
                _messageListener = new CNCMessageDisplayListener(this);
                
                // Register listener with CNC job info listener
                CNCJobInfoListener.AddListener(_messageListener);
                
                // Initialize the message display
                txtMessages.Text = "=== CNC Message Monitor ===\r\nWaiting for CNC messages...\r\n\r\n";
                
                LogInfo("CNC message display listener registered", "MessageDisplay");
            }
            catch (Exception ex)
            {
                LogError($"Failed to setup message display: {ex.Message}", "MessageDisplay");
            }
        }

        /// <summary>
        /// Set up the G-code display and job monitoring
        /// </summary>
        private void SetupGCodeDisplay()
        {
            try
            {
                // Create G-code display listener
                _gcodeListener = new GCodeDisplayListener(this);
                
                // Register listener with CNC job info listener
                CNCJobInfoListener.AddListener(_gcodeListener);
                
                // Initialize the G-code display
                txtGCode.Clear();
                txtGCode.ReadOnly = true; // Make it read-only for display purposes
                lblCurrentJob.Text = "No active job";
                
                LogInfo("G-code display listener registered", "GCodeDisplay");
            }
            catch (Exception ex)
            {
                LogError($"Failed to setup G-code display: {ex.Message}", "GCodeDisplay");
            }
        }

        /// <summary>
        /// Load G-code into the display panel
        /// </summary>
        public void LoadGCodeForDisplay(string[] gcode)
        {
            try
            {
                _gcodeListener?.LoadGCode(gcode);
                LogInfo($"Loaded {gcode?.Length ?? 0} lines of G-code for display", "GCodeDisplay");
            }
            catch (Exception ex)
            {
                LogError($"Error loading G-code for display: {ex.Message}", "GCodeDisplay");
            }
        }

        /// <summary>
        /// Clear the G-code display
        /// </summary>
        public void ClearGCodeDisplay()
        {
            try
            {
                _gcodeListener?.ClearGCode();
                LogInfo("G-code display cleared", "GCodeDisplay");
            }
            catch (Exception ex)
            {
                LogError($"Error clearing G-code display: {ex.Message}", "GCodeDisplay");
            }
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
                await AutoGenerateOpenApiIfNeeded();
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

        /// <summary>
        /// Logs a message to the application log display with timestamp
        /// </summary>
        /// <param name="message">The message to log</param>
        public void LogMessage(string message)
        {
            // Delegate to the centralized logging service
            LogInfo(message, "MainForm");
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
                LogMessage("Opened Swagger UI in browser");
            }
            catch (Exception ex)
            {
                LogMessage($"Failed to open Swagger UI: {ex.Message}");
                MessageBox.Show($"Failed to open Swagger UI: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task AutoGenerateOpenApiIfNeeded()
        {
            try
            {
                var projectRoot = Directory.GetCurrentDirectory();
                var openApiPath = Path.Combine(projectRoot, "openapi.json");
                
                // Check if openapi.json already exists
                if (File.Exists(openApiPath))
                {
                    LogMessage("OpenAPI specification file already exists, skipping auto-generation");
                    return;
                }

                LogMessage("OpenAPI specification file not found, generating automatically...");
                await GenerateOpenApiSpec();
            }
            catch (Exception ex)
            {
                LogMessage($"Auto-generation of OpenAPI specification failed: {ex.Message}");
                // Don't show a message box for auto-generation failures, just log it
            }
        }

        private async Task GenerateOpenApiSpec()
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            
            // Download the OpenAPI specification
            var openApiUrl = $"{ApiUrl}/swagger/v1/swagger.json";
            var response = await httpClient.GetAsync(openApiUrl);
            
            if (response.IsSuccessStatusCode)
            {
                var openApiJson = await response.Content.ReadAsStringAsync();
                
                // Save to project root
                var projectRoot = Directory.GetCurrentDirectory();
                var openApiPath = Path.Combine(projectRoot, "openapi.json");
                await File.WriteAllTextAsync(openApiPath, openApiJson);
                
                // Also save to bin directory for easy access
                var binPath = Path.Combine(projectRoot, "bin", "Debug", "net8.0-windows", "openapi.json");
                var binDir = Path.GetDirectoryName(binPath);
                if (!Directory.Exists(binDir))
                {
                    Directory.CreateDirectory(binDir!);
                }
                await File.WriteAllTextAsync(binPath, openApiJson);
                
                LogMessage($"OpenAPI specification generated successfully!");
                LogMessage($"Saved to: {openApiPath}");
                LogMessage($"Also saved to: {binPath}");
            }
            else
            {
                throw new HttpRequestException($"Failed to download OpenAPI specification. Status: {response.StatusCode}");
            }
        }

        private void btnGCodeTest_Click(object sender, EventArgs e)
        {
            try
            {
                LogMessage("Opening G-Code Test Dialog...");
                
                using (var gCodeDialog = new GCodeTestDialog(this))
                {
                    gCodeDialog.ShowDialog(this);
                }
                
                LogMessage("G-Code Test Dialog closed.");
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error opening G-Code Test Dialog: {ex.Message}";
                LogMessage(errorMessage);
                MessageBox.Show(errorMessage, "Dialog Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Uncomment and modify this method when you're ready to test Centroid API
        /*
        private void TestCentroidAPI()
        {
            try
            {
                LogMessage("Testing Centroid API connection...");
                
                // Example Centroid API test code:
                // var centroidAPI = new CentroidAPI(); // Replace with actual class name
                // bool isConnected = centroidAPI.Connect();
                // LogMessage($"Centroid API connection: {(isConnected ? "Success" : "Failed")}");
                
                LogMessage("Centroid API test completed.");
            }
            catch (Exception ex)
            {
                LogMessage($"Centroid API test failed: {ex.Message}");
                throw;
            }
        }
        */

        private async void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                // Clean up coordinate listener
                if (_coordinateListener != null)
                {
                    CNCJobInfoListener.RemoveListener(_coordinateListener);
                    LogInfo("Coordinate display listener removed", "CoordinateDisplay");
                }

                // Clean up message listener
                if (_messageListener != null)
                {
                    CNCJobInfoListener.RemoveListener(_messageListener);
                    LogInfo("CNC message display listener removed", "MessageDisplay");
                }

                // Set a timeout for shutdown to prevent hanging
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                {
                    // Stop the API server with timeout
                    if (_webHost != null)
                    {
                        await StopApiServerAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Error during application shutdown: {ex.Message}", "Shutdown");
            }
            
            // Force exit if needed - this will terminate any remaining background threads
            Environment.Exit(0);
        }

        /// <summary>
        /// Helper method to determine message severity based on MessageEventType
        /// </summary>
        /// <param name="messageType">The message event type to classify</param>
        /// <returns>MessageSeverity indicating the severity level</returns>
        public static MessageSeverity GetMessageSeverity(MessageEventType messageType)
        {
            // Use the centralized error check from CNCJobInfoListener
            if (CNCJobInfoListener.IsErrorMessage(messageType))
            {
                return MessageSeverity.Error;
            }

            switch (messageType)
            {

                // Warnings and less critical issues
                case MessageEventType.SyntaxError:
                case MessageEventType.GCodeError:
                case MessageEventType.ParameterError:
                case MessageEventType.CutterCompensationError:
                case MessageEventType.ParameterSettingError:
                case MessageEventType.CannedCycleError:
                case MessageEventType.ScalingError:
                    return MessageSeverity.Warning;

                // Job lifecycle events
                case MessageEventType.JobStarted:
                    return MessageSeverity.Success;
                case MessageEventType.JobCompleted:
                    return MessageSeverity.Success;
                case MessageEventType.JobCancelled:
                    return MessageSeverity.Warning;

                // System events
                case MessageEventType.ExitMessage:
                case MessageEventType.ConfigurationChange:
                    return MessageSeverity.Info;

                // Default status messages
                case MessageEventType.StatusMessage:
                case MessageEventType.Unknown:
                default:
                    return MessageSeverity.Normal;
            }
        }

        /// <summary>
        /// Message severity levels for color coding
        /// </summary>
        public enum MessageSeverity
        {
            /// <summary>Default message with normal formatting (black text)</summary>
            Normal,    // Default color (black)
            /// <summary>Informational message (blue text)</summary>
            Info,      // Blue
            /// <summary>Success or positive status message (green text)</summary>
            Success,   // Green  
            /// <summary>Warning message that needs attention (orange text)</summary>
            Warning,   // Orange
            /// <summary>Error message requiring immediate attention (red text)</summary>
            Error      // Red
        }

        /// <summary>
        /// CNC message display listener that shows messages with color coding
        /// </summary>
        private class CNCMessageDisplayListener : ICNCEventListener
        {
            private readonly MainForm _mainForm;
            private int _maxMessages = 1000;
            private int _currentMessageCount = 0;

            public CNCMessageDisplayListener(MainForm mainForm)
            {
                _mainForm = mainForm;
            }

            public void EventReceived(ICentroidEvent centroidEvent)
            {
                // Process both MessageEvent and DROEvent types for comprehensive display
                if (centroidEvent is MessageEvent messageEvent)
                {
                    // Update UI on the main thread using Invoke
                    if (_mainForm.InvokeRequired)
                    {
                        _mainForm.Invoke(new Action(() => AddMessage(messageEvent)));
                    }
                    else
                    {
                        AddMessage(messageEvent);
                    }
                }
                else if (centroidEvent is JobInfoEvent jobEvent)
                {
                    // Show job info events (line numbers, program changes, etc.)
                    if (_mainForm.InvokeRequired)
                    {
                        _mainForm.Invoke(new Action(() => AddJobInfoMessage(jobEvent)));
                    }
                    else
                    {
                        AddJobInfoMessage(jobEvent);
                    }
                }
            }

            private void AddMessage(MessageEvent messageEvent)
            {
                try
                {
                    var severity = GetMessageSeverity(messageEvent.EventType);
                    var timestamp = messageEvent.Timestamp.ToString("HH:mm:ss.fff");
                    var eventCode = messageEvent.EventCode > 0 ? $"[{messageEvent.EventCode}]" : "";
                    var messageText = $"[{timestamp}] {eventCode} ({messageEvent.EventType}) {messageEvent.Message}";

                    // Add message with color coding
                    AddColoredMessage(messageText, GetColorForSeverity(severity));

                    _currentMessageCount++;

                    // Trim old messages if we exceed the limit
                    if (_currentMessageCount > _maxMessages)
                    {
                        TrimOldMessages();
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Error adding CNC message to display: {ex.Message}", "MessageDisplay");
                }
            }

            private void AddJobInfoMessage(JobInfoEvent jobEvent)
            {
                try
                {
                    var timestamp = jobEvent.Timestamp.ToString("HH:mm:ss.fff");
                    var messageText = $"[{timestamp}] JOB: Line {jobEvent.LineNumber} - {jobEvent.Message}";

                    // Job info messages are shown in blue
                    AddColoredMessage(messageText, Color.Blue);

                    _currentMessageCount++;

                    // Trim old messages if we exceed the limit
                    if (_currentMessageCount > _maxMessages)
                    {
                        TrimOldMessages();
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Error adding job info message to display: {ex.Message}", "MessageDisplay");
                }
            }

            private void AddColoredMessage(string message, Color color)
            {
                try
                {
                    // Save current selection
                    var originalStart = _mainForm.txtMessages.SelectionStart;
                    var originalLength = _mainForm.txtMessages.SelectionLength;

                    // Move to end and add text
                    _mainForm.txtMessages.SelectionStart = _mainForm.txtMessages.Text.Length;
                    _mainForm.txtMessages.SelectionLength = 0;
                    _mainForm.txtMessages.SelectionColor = color;
                    _mainForm.txtMessages.AppendText(message + Environment.NewLine);

                    // Reset color to default
                    _mainForm.txtMessages.SelectionColor = Color.Black;

                    // Scroll to bottom to show latest messages
                    _mainForm.txtMessages.ScrollToCaret();

                    // Restore original selection (if any)
                    if (originalLength > 0)
                    {
                        _mainForm.txtMessages.SelectionStart = originalStart;
                        _mainForm.txtMessages.SelectionLength = originalLength;
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Error adding colored message: {ex.Message}", "MessageDisplay");
                }
            }

            private Color GetColorForSeverity(MessageSeverity severity)
            {
                return severity switch
                {
                    MessageSeverity.Error => Color.Red,
                    MessageSeverity.Warning => Color.Orange,
                    MessageSeverity.Success => Color.Green,
                    MessageSeverity.Info => Color.Blue,
                    MessageSeverity.Normal => Color.Black,
                    _ => Color.Black
                };
            }

            private void TrimOldMessages()
            {
                try
                {
                    var lines = _mainForm.txtMessages.Lines;
                    if (lines.Length > _maxMessages)
                    {
                        // Keep only the most recent messages
                        var keepLines = lines.Skip(lines.Length - (_maxMessages * 3 / 4)).ToArray();
                        _mainForm.txtMessages.Lines = keepLines;
                        _currentMessageCount = keepLines.Length;
                        
                        // Add separator to show where trimming occurred
                        AddColoredMessage("--- [Previous messages trimmed] ---", Color.Gray);
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Error trimming old messages: {ex.Message}", "MessageDisplay");
                }
            }
        }

        /// <summary>
        /// G-code display listener that shows current job G-code with line highlighting
        /// </summary>
        private class GCodeDisplayListener : ICNCEventListener
        {
            private readonly MainForm _mainForm;
            private string[] _currentGCode = Array.Empty<string>();
            private int _currentLineNumber = 0;

            public GCodeDisplayListener(MainForm mainForm)
            {
                _mainForm = mainForm;
            }

            public void EventReceived(ICentroidEvent centroidEvent)
            {
                // Prioritize our custom StepExecutionEvent for accurate line tracking
                if (centroidEvent is StepExecutionEvent stepEvent)
                {
                    // Handle step execution event (most accurate for G-code display)
                    if (_mainForm.InvokeRequired)
                    {
                        _mainForm.Invoke(new Action(() => HandleStepExecution(stepEvent)));
                    }
                    else
                    {
                        HandleStepExecution(stepEvent);
                    }
                }
                else if (centroidEvent is JobStartedEvent jobStartedEvent)
                {
                    // Handle job started event
                    if (_mainForm.InvokeRequired)
                    {
                        _mainForm.Invoke(new Action(() => HandleJobStarted(jobStartedEvent)));
                    }
                    else
                    {
                        HandleJobStarted(jobStartedEvent);
                    }
                }
                else if (centroidEvent is JobCompletedEvent jobCompletedEvent)
                {
                    // Handle job completed event
                    if (_mainForm.InvokeRequired)
                    {
                        _mainForm.Invoke(new Action(() => HandleJobCompleted(jobCompletedEvent)));
                    }
                    else
                    {
                        HandleJobCompleted(jobCompletedEvent);
                    }
                }
                else if (centroidEvent is JobInfoEvent jobEvent)
                {
                    // Fallback to JobInfoEvent only if no StepExecutionEvent is available
                    // This ensures compatibility with existing CNC system events
                    if (_mainForm.InvokeRequired)
                    {
                        _mainForm.Invoke(new Action(() => UpdateGCodeDisplayFallback(jobEvent)));
                    }
                    else
                    {
                        UpdateGCodeDisplayFallback(jobEvent);
                    }
                }
            }

            private void UpdateGCodeDisplayFallback(JobInfoEvent jobEvent)
            {
                try
                {
                    // Update current line number
                    _currentLineNumber = jobEvent.LineNumber;

                    // Update current job label
                    _mainForm.lblCurrentJob.Text = $"Current Job: Line {_currentLineNumber} - {jobEvent.JobName ?? "Unknown"}";

                    // If we have G-code loaded, highlight the current line
                    if (_currentGCode.Length > 0)
                    {
                        DisplayGCodeWithHighlight();
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Error updating G-code display (fallback): {ex.Message}", "GCodeDisplay");
                }
            }

            private void HandleStepExecution(StepExecutionEvent stepEvent)
            {
                try
                {
                    // Update current line number
                    _currentLineNumber = stepEvent.LineNumber;

                    // Update job status with step information
                    var statusText = stepEvent.Status switch
                    {
                        StepExecutionStatus.AboutToExecute => "About to execute",
                        StepExecutionStatus.Executing => "Executing",
                        StepExecutionStatus.Completed => "Completed",
                        StepExecutionStatus.Failed => "Failed",
                        StepExecutionStatus.Skipped => "Skipped",
                        _ => "Unknown"
                    };

                    _mainForm.lblCurrentJob.Text = $"Step Run: {stepEvent.JobId} - Line {stepEvent.LineNumber}/{stepEvent.TotalLines} ({statusText})";

                    // If we have G-code loaded, highlight the current line
                    if (_currentGCode.Length > 0)
                    {
                        DisplayGCodeWithHighlight();
                    }

                    LogDebug($"Step execution event: Line {stepEvent.LineNumber} - {statusText} - {stepEvent.CurrentLine}", "GCodeDisplay");
                }
                catch (Exception ex)
                {
                    LogError($"Error handling step execution event: {ex.Message}", "GCodeDisplay");
                }
            }

            private void HandleJobStarted(JobStartedEvent jobStartedEvent)
            {
                try
                {
                    // Load the G-code into the display
                    LoadGCode(jobStartedEvent.GCodeLines);
                    
                    // Update job info
                    _mainForm.lblCurrentJob.Text = $"Job Started: {jobStartedEvent.JobId} ({jobStartedEvent.TotalLines} lines)";
                    
                    // Reset current line to start
                    _currentLineNumber = 1;
                    
                    DisplayGCodeWithHighlight();
                    
                    LogInfo($"Job started event handled: {jobStartedEvent.JobId}", "GCodeDisplay");
                }
                catch (Exception ex)
                {
                    LogError($"Error handling job started event: {ex.Message}", "GCodeDisplay");
                }
            }

            private void HandleJobCompleted(JobCompletedEvent jobCompletedEvent)
            {
                try
                {
                    // Update job status
                    var status = jobCompletedEvent.Success ? "COMPLETED" : "FAILED";
                    var duration = jobCompletedEvent.Duration.TotalSeconds.ToString("F1");
                    
                    _mainForm.lblCurrentJob.Text = $"Job {status}: {jobCompletedEvent.JobId} ({duration}s, {jobCompletedEvent.LinesExecuted} lines)";
                    
                    if (!jobCompletedEvent.Success && !string.IsNullOrEmpty(jobCompletedEvent.ErrorMessage))
                    {
                        _mainForm.lblCurrentJob.Text += $" - Error: {jobCompletedEvent.ErrorMessage}";
                    }
                    
                    LogInfo($"Job completed event handled: {jobCompletedEvent.JobId} - Success: {jobCompletedEvent.Success}", "GCodeDisplay");
                }
                catch (Exception ex)
                {
                    LogError($"Error handling job completed event: {ex.Message}", "GCodeDisplay");
                }
            }

            public void LoadGCode(string[] gcode)
            {
                try
                {
                    _currentGCode = gcode ?? Array.Empty<string>();
                    _currentLineNumber = 0;

                    // Display the G-code
                    DisplayGCodeWithHighlight();
                }
                catch (Exception ex)
                {
                    LogError($"Error loading G-code: {ex.Message}", "GCodeDisplay");
                }
            }

            public void ClearGCode()
            {
                try
                {
                    _currentGCode = Array.Empty<string>();
                    _currentLineNumber = 0;
                    _mainForm.txtGCode.Clear();
                    _mainForm.lblCurrentJob.Text = "No active job";
                }
                catch (Exception ex)
                {
                    LogError($"Error clearing G-code: {ex.Message}", "GCodeDisplay");
                }
            }

            private void DisplayGCodeWithHighlight()
            {
                try
                {
                    if (_currentGCode.Length == 0)
                    {
                        _mainForm.txtGCode.Clear();
                        return;
                    }

                    // Clear existing text
                    _mainForm.txtGCode.Clear();

                    // Add each line with appropriate highlighting
                    for (int i = 0; i < _currentGCode.Length; i++)
                    {
                        var lineNumber = i + 1;
                        var line = _currentGCode[i];
                        var displayLine = $"{lineNumber:D4}: {line}";

                        // Set color based on whether this is the current line
                        Color lineColor = lineNumber == _currentLineNumber ? Color.Red : Color.Black;
                        Color backgroundColor = lineNumber == _currentLineNumber ? Color.Yellow : Color.White;

                        // Add the line with color
                        AddColoredGCodeLine(displayLine, lineColor, backgroundColor);
                    }

                    // Scroll to the current line
                    ScrollToCurrentLine();
                }
                catch (Exception ex)
                {
                    LogError($"Error displaying G-code with highlight: {ex.Message}", "GCodeDisplay");
                }
            }

            private void AddColoredGCodeLine(string line, Color textColor, Color backgroundColor)
            {
                try
                {
                    // Move to end and add text
                    _mainForm.txtGCode.SelectionStart = _mainForm.txtGCode.Text.Length;
                    _mainForm.txtGCode.SelectionLength = 0;
                    _mainForm.txtGCode.SelectionColor = textColor;
                    _mainForm.txtGCode.SelectionBackColor = backgroundColor;
                    _mainForm.txtGCode.AppendText(line + Environment.NewLine);

                    // Reset colors to default
                    _mainForm.txtGCode.SelectionColor = Color.Black;
                    _mainForm.txtGCode.SelectionBackColor = Color.White;
                }
                catch (Exception ex)
                {
                    LogError($"Error adding colored G-code line: {ex.Message}", "GCodeDisplay");
                }
            }

            private void ScrollToCurrentLine()
            {
                try
                {
                    if (_currentLineNumber > 0 && _currentGCode.Length > 0)
                    {
                        // Calculate the character position of the current line
                        int charPosition = 0;
                        for (int i = 0; i < Math.Min(_currentLineNumber - 1, _currentGCode.Length); i++)
                        {
                            charPosition += $"{i + 1:D4}: {_currentGCode[i]}".Length + Environment.NewLine.Length;
                        }

                        // Set selection to the current line
                        _mainForm.txtGCode.SelectionStart = charPosition;
                        _mainForm.txtGCode.ScrollToCaret();
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Error scrolling to current line: {ex.Message}", "GCodeDisplay");
                }
            }
        }

        /// <summary>
        /// Coordinate display listener that updates the UI with machine position data
        /// </summary>
        private class CoordinateDisplayListener : ICNCEventListener
        {
            private readonly MainForm _mainForm;

            public CoordinateDisplayListener(MainForm mainForm)
            {
                _mainForm = mainForm;
            }

            public void EventReceived(ICentroidEvent centroidEvent)
            {
                // Only process DRO events for coordinate updates
                if (centroidEvent is DROEvent droEvent)
                {
                    // Update UI on the main thread using Invoke
                    if (_mainForm.InvokeRequired)
                    {
                        _mainForm.Invoke(new Action(() => UpdateCoordinateDisplay(droEvent)));
                    }
                    else
                    {
                        UpdateCoordinateDisplay(droEvent);
                    }
                }
            }

            private void UpdateCoordinateDisplay(DROEvent droEvent)
            {
                try
                {
                    // Update X, Y, Z coordinate displays with 4 decimal places
                    _mainForm.lblXValue.Text = droEvent.Axis1.ToString("F4");
                    _mainForm.lblYValue.Text = droEvent.Axis2.ToString("F4");
                    _mainForm.lblZValue.Text = droEvent.Axis3.ToString("F4");
                }
                catch (Exception ex)
                {
                    LogError($"Error updating coordinate display: {ex.Message}", "CoordinateDisplay");
                }
            }
        }
    }
}
