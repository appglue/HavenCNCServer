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
        private const string ApiUrl = "http://localhost:5000";
        private const string SwaggerUrl = "http://localhost:5000/swagger";
        private const string ReactAppUrl = "http://localhost:5000"; // Now served by the embedded server

        /// <summary>
        /// Initializes a new instance of the MainForm class
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            
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
        }

        /// <summary>
        /// Set up the centralized logging system
        /// </summary>
        private void SetupLogging()
        {
            // Create and register a log target for the main form's text box
            var logTarget = new TextBoxLogTarget(txtLog, this);
            LoggingService.AddTarget(logTarget);
            
            // Set maximum log entries from settings or default
            LoggingService.MaxLogEntries = 2000;
            
            LogInfo("Logging system initialized", "System");
        }

        private async void MainForm_Load(object? sender, EventArgs e)
        {
            await StartApiServerAsync();
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

        private async void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_webHost != null)
            {
                await StopApiServerAsync();
            }
        }
    }
}
