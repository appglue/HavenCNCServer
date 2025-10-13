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
            
            // Start the API server automatically when the form loads
            this.Load += MainForm_Load;
            this.Resize += MainForm_Resize;
        }

        /// <summary>
        /// Set up the 50/50 layout for messages and logs
        /// </summary>
        private void SetupLayout()
        {
            // This will be called in MainForm_Resize to dynamically adjust the layout
        }

        /// <summary>
        /// Handle form resize to maintain 50/50 split between messages and logs
        /// </summary>
        private void MainForm_Resize(object? sender, EventArgs e)
        {
            if (txtLog != null && txtMessages != null && pnlControls != null)
            {
                var availableWidth = this.ClientSize.Width - 24; // Account for margins
                var halfWidth = availableWidth / 2 - 6; // Account for gap between controls
                
                // Update log section (left 50%)
                txtLog.Width = halfWidth;
                
                // Update messages section (right 50%)
                txtMessages.Left = txtLog.Right + 12; // 12px gap
                txtMessages.Width = halfWidth;
                
                // Update labels accordingly
                if (lblMessages != null)
                {
                    lblMessages.Left = txtMessages.Left;
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
        /// Test coordinate display with simulated DRO events
        /// </summary>
        private void TestCoordinateDisplay()
        {
            try
            {
                LogMessage("Testing coordinate display with simulated data...");
                
                // Create test DRO events to simulate position updates
                var testPositions = new[]
                {
                    new { X = 1.2345, Y = 2.3456, Z = 3.4567 },
                    new { X = 10.1234, Y = 20.2345, Z = 30.3456 },
                    new { X = -5.6789, Y = -15.7890, Z = -25.8901 }
                };

                foreach (var pos in testPositions)
                {
                    var droEvent = new DROEvent
                    {
                        Timestamp = DateTime.Now,
                        Axis1 = pos.X, // X axis
                        Axis2 = pos.Y, // Y axis  
                        Axis3 = pos.Z, // Z axis
                        Axis4 = 0.0,   // A axis (not displayed)
                        Axis5 = 0.0,   // B axis (not displayed)
                        Axis6 = 0.0,   // C axis (not displayed)
                        Axis7 = 0.0,   // U axis (not displayed)
                        Axis8 = 0.0,   // V axis (not displayed)
                        Message = $"Test position: X={pos.X:F4}, Y={pos.Y:F4}, Z={pos.Z:F4}"
                    };

                    // Simulate the coordinate listener receiving the event
                    _coordinateListener?.EventReceived(droEvent);
                    
                    // Small delay to show the updates
                    System.Threading.Thread.Sleep(500);
                }

                LogMessage("Coordinate display test completed!");

                // Test message display with various message types
                TestMessageDisplay();
            }
            catch (Exception ex)
            {
                LogError($"Error testing coordinate display: {ex.Message}", "CoordinateDisplay");
            }
        }

        /// <summary>
        /// Test message display with simulated CNC messages of different types
        /// </summary>
        private void TestMessageDisplay()
        {
            try
            {
                LogMessage("Testing CNC message display with various message types...");

                // Create test message events of different types
                var testMessages = new[]
                {
                    new MessageEvent { Timestamp = DateTime.Now, EventCode = 306, Message = "Job Finished Successfully", EventType = MessageEventType.JobCompleted },
                    new MessageEvent { Timestamp = DateTime.Now, EventCode = 301, Message = "Machine Stopped", EventType = MessageEventType.StatusMessage },
                    new MessageEvent { Timestamp = DateTime.Now, EventCode = 405, Message = "Lubricant level low", EventType = MessageEventType.SystemFault },
                    new MessageEvent { Timestamp = DateTime.Now, EventCode = 407, Message = "X-axis limit switch tripped", EventType = MessageEventType.LimitError },
                    new MessageEvent { Timestamp = DateTime.Now, EventCode = 0, Message = "Job started: TEST_PROGRAM.nc", EventType = MessageEventType.JobStarted },
                    new MessageEvent { Timestamp = DateTime.Now, EventCode = 501, Message = "Invalid G-code syntax", EventType = MessageEventType.SyntaxError },
                    new MessageEvent { Timestamp = DateTime.Now, EventCode = 307, Message = "Operator abort: job canceled", EventType = MessageEventType.JobCancelled },
                    new MessageEvent { Timestamp = DateTime.Now, EventCode = 199, Message = "CNC started", EventType = MessageEventType.StatusMessage }
                };

                foreach (var message in testMessages)
                {
                    // Simulate a small delay between messages
                    System.Threading.Thread.Sleep(300);
                    
                    // Update timestamp to current time
                    message.Timestamp = DateTime.Now;
                    
                    // Send to message listener
                    _messageListener?.EventReceived(message);
                }

                LogMessage("CNC message display test completed!");
                
                // Test message storage functionality
                TestMessageStorage();
            }
            catch (Exception ex)
            {
                LogError($"Error testing message display: {ex.Message}", "MessageDisplay");
            }
        }

        /// <summary>
        /// Test message storage and retrieval functionality
        /// </summary>
        private void TestMessageStorage()
        {
            try
            {
                LogMessage("Testing CNC message storage functionality...");

                // Wait a moment for messages to be stored
                System.Threading.Thread.Sleep(1000);

                // Test getting all stored messages
                var allMessages = CNCJobInfoListener.GetStoredMessages();
                LogMessage($"Total stored messages: {allMessages.Count}");

                // Test getting recent messages (last 10 seconds)
                var recentMessages = CNCJobInfoListener.GetRecentMessages(10000);
                LogMessage($"Recent messages (last 10 seconds): {recentMessages.Count}");

                // Test getting messages by type (MessageEvent)
                var messageEvents = CNCJobInfoListener.GetRecentMessagesByType<MessageEvent>(10000);
                LogMessage($"Recent MessageEvents: {messageEvents.Count}");

                // Test getting messages by type (DROEvent)
                var droEvents = CNCJobInfoListener.GetRecentMessagesByType<DROEvent>(10000);
                LogMessage($"Recent DROEvents: {droEvents.Count}");

                // Test getting messages by communication type
                var messageWindowMessages = CNCJobInfoListener.GetRecentMessagesByCommunicationType(10000, "MESSAGE_WINDOW_MESSAGE");
                LogMessage($"Recent MESSAGE_WINDOW_MESSAGE: {messageWindowMessages.Count}");

                // Display some recent messages for verification
                if (recentMessages.Count > 0)
                {
                    LogMessage("Sample of recent messages:");
                    foreach (var msg in recentMessages.Take(3))
                    {
                        var eventTypeName = msg.Event.GetType().Name;
                        LogMessage($"  [{msg.Timestamp:HH:mm:ss.fff}] {msg.CommunicationType} - {eventTypeName}: {msg.Event.Message}");
                    }
                }

                LogMessage("Message storage test completed successfully!");
            }
            catch (Exception ex)
            {
                LogError($"Error testing message storage: {ex.Message}", "MessageStorage");
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
                
                btnStartServer.Enabled = false;
                btnStopServer.Enabled = true;

                // Get the CNC Server Manager from DI and start management (auto-start is enabled)
                _cncServerManager = _webHost.Services.GetService<ICNCServerManager>();
                if (_cncServerManager != null)
                {
                    await _cncServerManager.StartManagementAsync();
                    LogInfo("CNC Server Manager started with auto-start enabled", "CNCServer");
                    
                    // Update CNC server button state
                    UpdateCNCServerButtonState();
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
                btnStartServer.Enabled = true;
                btnStopServer.Enabled = false;
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
                
                btnStartServer.Enabled = true;
                btnStopServer.Enabled = false;
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
                // Stop CNC Server Manager if it's still running
                if (_cncServerManager != null)
                {
                    // This is synchronous cleanup in form closing
                    Task.Run(async () => await _cncServerManager.StopManagementAsync()).Wait(5000);
                    LogInfo("CNC Server Manager cleanup completed", "System");
                }
                
                // Unsubscribe from CNC events
                CNCConnectionManager.ConnectionStatusChanged -= OnCNCConnectionStatusChanged;
                
                // Cleanup the CNC connection manager
                CNCConnectionManager.Disconnect();
                
                LogInfo("Application shutting down", "System");
            }
            catch (Exception ex)
            {
                LogError($"Error during shutdown cleanup: {ex.Message}", "System");
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

        private async void btnStartServer_Click(object sender, EventArgs e)
        {
            await StartApiServerAsync();
        }

        private async void btnStopServer_Click(object sender, EventArgs e)
        {
            await StopApiServerAsync();
        }

        private async void btnOpenReactApp_Click(object sender, EventArgs e)
        {
            try
            {
                if (!Services.UIControlService.IsFullScreen)
                {
                    // Open the browser in full screen mode
                    bool success = await Services.UIControlService.EnterFullScreenAsync();
                    
                    if (success)
                    {
                        // Update button text
                        btnOpenReactApp.Text = "Hide React App";
                        LogMessage($"Browser opened in full screen mode at {ReactAppUrl}");
                    }
                    else
                    {
                        LogMessage("Failed to open browser in full screen mode");
                        MessageBox.Show("Failed to open browser", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    // Close/hide the browser
                    bool success = await Services.UIControlService.ExitFullScreenAsync();
                    
                    if (success)
                    {
                        // Update button text
                        btnOpenReactApp.Text = "Open React App";
                        LogMessage("Browser closed");
                    }
                    else
                    {
                        LogMessage("Failed to close browser");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Failed to control browser: {ex.Message}");
                MessageBox.Show($"Failed to control browser: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private async void btnGenerateOpenApi_Click(object sender, EventArgs e)
        {
            try
            {
                if (_webHost == null)
                {
                    MessageBox.Show("API server is not running. Please start the server first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                LogMessage("Manually generating OpenAPI specification...");
                await GenerateOpenApiSpec();
                
                MessageBox.Show($"OpenAPI specification generated successfully!\n\nFiles saved to:\n• openapi.json (project root)\n• bin/Debug/net8.0-windows/openapi.json", 
                              "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (HttpRequestException ex)
            {
                var errorMessage = $"Network error while generating OpenAPI: {ex.Message}";
                LogMessage(errorMessage);
                MessageBox.Show($"Failed to connect to API server.\nMake sure the API server is running.\n\nError: {ex.Message}", 
                              "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error generating OpenAPI specification: {ex.Message}";
                LogMessage(errorMessage);
                MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            try
            {
                LogMessage("Test button clicked!");

                // Test coordinate display with simulated data
                TestCoordinateDisplay();

                // Test CNCConnectionManager instead of creating CNCPipe directly
                var cncPipe = CNCConnectionManager.GetCNCPipe();
                
                if (cncPipe != null && cncPipe.IsConstructed())
                {
                    LogMessage("CNCPipe is available via CNCConnectionManager!");
                    MessageBox.Show("Test button working! CNCPipe is ready for use via CNCConnectionManager.",
                        "Test", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    LogMessage("CNCPipe not available - attempting connection...");
                    // Try to establish connection
                    cncPipe = CNCConnectionManager.GetOrCreateCNCPipe();
                    
                    if (cncPipe != null && cncPipe.IsConstructed())
                    {
                        LogMessage("CNCPipe connected successfully via CNCConnectionManager!");
                        MessageBox.Show("CNC connected successfully!",
                            "Test", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        LogMessage("Failed to connect to CNC via CNCConnectionManager");
                        MessageBox.Show("Failed to connect to CNC. Make sure CNC12 is running.",
                            "Test Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                LogMessage("Test completed successfully.");
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Test error: {ex.Message}";
                    LogMessage(errorMessage);
                    MessageBox.Show(errorMessage, "Test Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        /// <summary>
        /// Update the CNC Server button state based on current server status
        /// </summary>
        private void UpdateCNCServerButtonState()
        {
            if (InvokeRequired)
            {
                Invoke(() => UpdateCNCServerButtonState());
                return;
            }

            try
            {
                if (_cncServerManager == null)
                {
                    btnCNCServer.Text = "CNC Manager N/A";
                    btnCNCServer.Enabled = false;
                    btnCNCServer.BackColor = SystemColors.Control;
                    return;
                }

                var isRunning = _cncServerManager.IsServerRunning;
                var weStarted = _cncServerManager.WeStartedServer;

                if (isRunning && weStarted)
                {
                    btnCNCServer.Text = "Stop CNC Server";
                    btnCNCServer.Enabled = true;
                    btnCNCServer.BackColor = Color.LightGreen;
                }
                else if (isRunning && !weStarted)
                {
                    btnCNCServer.Text = "CNC Running (Ext)";
                    btnCNCServer.Enabled = false;
                    btnCNCServer.BackColor = Color.LightBlue;
                }
                else
                {
                    btnCNCServer.Text = "Start CNC Server";
                    btnCNCServer.Enabled = true;
                    btnCNCServer.BackColor = SystemColors.Control;
                }
            }
            catch (Exception ex)
            {
                LogError($"Error updating CNC server button state: {ex.Message}", "UI");
                btnCNCServer.Text = "CNC Server Error";
                btnCNCServer.Enabled = false;
            }
        }

        private async void btnCNCServer_Click(object sender, EventArgs e)
        {
            try
            {
                if (_cncServerManager == null)
                {
                    LogWarning("CNC Server Manager not available - API server must be running first", "CNCServer");
                    MessageBox.Show("Please start the API server first before managing the CNC server.", "CNC Server", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var isRunning = _cncServerManager.IsServerRunning;
                var weStarted = _cncServerManager.WeStartedServer;
                
                if (isRunning && weStarted)
                {
                    // Stop the server
                    LogInfo("Stopping CNC server manually...", "CNCServer");
                    btnCNCServer.Enabled = false;
                    btnCNCServer.Text = "Stopping...";
                    
                    var result = await _cncServerManager.StopServerAsync();
                    
                    if (result)
                    {
                        LogSuccess("CNC server stopped successfully", "CNCServer");
                    }
                    else
                    {
                        LogError("Failed to stop CNC server", "CNCServer");
                    }
                    
                    UpdateCNCServerButtonState();
                }
                else if (!isRunning)
                {
                    // Start the server
                    LogInfo("Starting CNC server manually...", "CNCServer");
                    btnCNCServer.Enabled = false;
                    btnCNCServer.Text = "Starting...";
                    
                    var result = await _cncServerManager.StartServerAsync();
                    
                    if (result)
                    {
                        LogSuccess("CNC server started successfully", "CNCServer");
                    }
                    else
                    {
                        LogError("Failed to start CNC server", "CNCServer");
                    }
                    
                    UpdateCNCServerButtonState();
                }
                else
                {
                    // Server is running but we didn't start it
                    LogInfo("CNC server is running (started externally)", "CNCServer");
                    MessageBox.Show("CNC server is already running but was started externally.\nCannot control external processes.", 
                        "CNC Server", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                LogError($"Error controlling CNC server: {ex.Message}", "CNCServer");
                MessageBox.Show($"Error controlling CNC server: {ex.Message}", "CNC Server Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnCNCServer.Text = "Start CNC Server";
            }
            finally
            {
                UpdateCNCServerButtonState();
            }
        }

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
