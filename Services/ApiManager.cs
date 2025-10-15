using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Services
{
    /// <summary>
    /// Manages the ASP.NET Core Web API server lifecycle
    /// </summary>
    public class ApiManager : IDisposable
    {
        private IHost? _webHost;
        private CancellationTokenSource? _cancellationTokenSource;
        private ICNCServerManager? _cncServerManager;
        private readonly string _apiUrl;
        
        /// <summary>
        /// Event raised when the API server status changes
        /// </summary>
        public event Action<string, Color>? StatusChanged;

        /// <summary>
        /// Gets whether the API server is currently running
        /// </summary>
        public bool IsRunning => _webHost != null && !(_cancellationTokenSource?.Token.IsCancellationRequested ?? true);

        /// <summary>
        /// Gets the API server URL
        /// </summary>
        public string ApiUrl => _apiUrl;

        /// <summary>
        /// Gets the Swagger UI URL
        /// </summary>
        public string SwaggerUrl => $"{_apiUrl}/swagger";

        /// <summary>
        /// Gets the cancellation token for background operations
        /// </summary>
        public CancellationToken CancellationToken => _cancellationTokenSource?.Token ?? CancellationToken.None;

        /// <summary>
        /// Initializes a new instance of the ApiManager class
        /// </summary>
        /// <param name="apiUrl">The base URL for the API server</param>
        public ApiManager(string apiUrl)
        {
            _apiUrl = apiUrl ?? throw new ArgumentNullException(nameof(apiUrl));
        }

        /// <summary>
        /// Starts the API server asynchronously
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        public async Task StartAsync()
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
                            .UseUrls(_apiUrl)
                            .UseStartup<ApiStartup>()
                            .ConfigureLogging(logging =>
                            {
                                logging.ClearProviders();
                                // Use centralized logging service instead of WinFormsLoggerProvider
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
                        LogError($"Error running web host: {ex.Message}", "API");
                        UpdateStatus("API Server Error", Color.Red);
                    }
                });

                // Give the server a moment to start
                await Task.Delay(2000);

                UpdateStatus("API Server Running", Color.Green);
                LogSuccess($"API server started successfully at {_apiUrl}", "API");
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
                await OpenApiManager.AutoGenerateIfNeededAsync(_apiUrl);
            }
            catch (Exception ex)
            {
                UpdateStatus("Failed to Start", Color.Red);
                LogError($"Failed to start API server: {ex.Message}", "API");
                throw;
            }
        }

        /// <summary>
        /// Stops the API server asynchronously
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        public async Task StopAsync()
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
                throw;
            }
        }

        /// <summary>
        /// Restarts the API server asynchronously
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        public async Task RestartAsync()
        {
            LogInfo("Restarting API server...", "API");
            await StopAsync();
            await Task.Delay(1000); // Brief pause between stop and start
            await StartAsync();
        }

        /// <summary>
        /// Updates the status and raises the StatusChanged event
        /// </summary>
        /// <param name="status">The status message</param>
        /// <param name="color">The status color</param>
        private void UpdateStatus(string status, Color color)
        {
            StatusChanged?.Invoke(status, color);
        }

        /// <summary>
        /// Disposes the API manager and stops the server if running
        /// </summary>
        public void Dispose()
        {
            try
            {
                // Try to stop gracefully, but don't wait too long
                var stopTask = StopAsync();
                if (!stopTask.Wait(TimeSpan.FromSeconds(5)))
                {
                    LogWarning("API server stop operation timed out during disposal", "API");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error during API manager disposal: {ex.Message}", "API");
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _webHost?.Dispose();
            }
        }
    }
}