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

namespace HavenCNCServer
{
    public partial class MainForm : Form
    {
        private IHost? _webHost;
        private CancellationTokenSource? _cancellationTokenSource;
        private const string ApiUrl = "http://localhost:5000";
        private const string SwaggerUrl = "http://localhost:5000/swagger";
        private const string ReactAppUrl = "http://localhost:5000"; // Now served by the embedded server

        public MainForm()
        {
            InitializeComponent();
            
            // Register this form with the UI control service
            Services.UIControlService.RegisterMainForm(this);
            
            // Start the API server automatically when the form loads
            this.Load += MainForm_Load;
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            await StartApiServerAsync();
        }

        private async Task StartApiServerAsync()
        {
            try
            {
                UpdateStatus("Starting API Server...", Color.Orange);
                LogMessage("Initializing API server...");

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
                            LogMessage($"Error running web host: {ex.Message}");
                            UpdateStatus("API Server Error", Color.Red);
                        });
                    }
                });

                // Give the server a moment to start
                await Task.Delay(2000);

                UpdateStatus("API Server Running", Color.Green);
                LogMessage($"API server started successfully at {ApiUrl}");
                LogMessage($"Swagger UI available at {SwaggerUrl}");
                
                btnStartServer.Enabled = false;
                btnStopServer.Enabled = true;

                // Auto-generate OpenAPI specification if it doesn't exist
                await AutoGenerateOpenApiIfNeeded();
            }
            catch (Exception ex)
            {
                UpdateStatus("Failed to Start", Color.Red);
                LogMessage($"Failed to start API server: {ex.Message}");
                btnStartServer.Enabled = true;
                btnStopServer.Enabled = false;
            }
        }

        private async Task StopApiServerAsync()
        {
            try
            {
                UpdateStatus("Stopping API Server...", Color.Orange);
                LogMessage("Stopping API server...");

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
                LogMessage("API server stopped successfully");
                
                btnStartServer.Enabled = true;
                btnStopServer.Enabled = false;
            }
            catch (Exception ex)
            {
                LogMessage($"Error stopping API server: {ex.Message}");
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

        public void LogMessage(string message)
        {
            if (InvokeRequired)
            {
                Invoke(() => LogMessage(message));
                return;
            }

            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            txtLog.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
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

        private async void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_webHost != null)
            {
                await StopApiServerAsync();
            }
        }
    }
}
