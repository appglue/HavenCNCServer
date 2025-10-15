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
    public static class ApiManager
    {
        private static IHost? _webHost;
        private static CancellationTokenSource? _cancellationTokenSource;
        private static bool _isRunning = false;
        private static readonly object _lock = new object();
        private static readonly string _apiUrl = "http://localhost:5000";
        
        /// <summary>
        /// Event raised when the API server status changes
        /// </summary>
        public static event Action<string, Color>? StatusChanged;

        /// <summary>
        /// Gets whether the API server is currently running
        /// </summary>
        public static bool IsRunning
        {
            get
            {
                lock (_lock)
                {
                    return _isRunning;
                }
            }
        }

        /// <summary>
        /// Gets the API server URL
        /// </summary>
        public static string ApiUrl => _apiUrl;

        /// <summary>
        /// Gets the Swagger UI URL
        /// </summary>
        public static string SwaggerUrl => $"{_apiUrl}/swagger";

        /// <summary>
        /// Gets the cancellation token for background operations
        /// </summary>
        public static CancellationToken CancellationToken => _cancellationTokenSource?.Token ?? CancellationToken.None;

        /// <summary>
        /// Starts the API server asynchronously
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>Task representing the async operation</returns>
        public static async Task StartAsync(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (_isRunning)
                {
                    LogWarning("API server is already running", "API");
                    return;
                }
                _isRunning = true;
            }

            try
            {
                LogInfo("Initializing API server...", "API");
                UpdateStatus("Starting API Server...", Color.Orange);

                // Use the provided cancellation token or create a linked one
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

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

                // Auto-generate OpenAPI specification if it doesn't exist
                await OpenApiManager.AutoGenerateIfNeededAsync(_apiUrl);
            }
            catch (Exception ex)
            {
                lock (_lock)
                {
                    _isRunning = false;
                }
                UpdateStatus("Failed to Start", Color.Red);
                LogError($"Failed to start API server: {ex.Message}", "API");
                throw;
            }
        }

        /// <summary>
        /// Stops the API server asynchronously
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>Task representing the async operation</returns>
        public static async Task StopAsync(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (!_isRunning)
                {
                    LogInfo("API server is not running", "API");
                    return;
                }
                _isRunning = false;
            }

            try
            {
                UpdateStatus("Stopping API Server...", Color.Orange);
                LogInfo("Stopping API server...", "API");

                _cancellationTokenSource?.Cancel();
                
                if (_webHost != null)
                {
                    // Give web host limited time to stop gracefully
                    using var timeoutToken = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    using var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationToken, timeoutToken.Token);
                    
                    try
                    {
                        await _webHost.StopAsync(combinedToken.Token);
                    }
                    catch (OperationCanceledException) when (timeoutToken.Token.IsCancellationRequested)
                    {
                        LogWarning("Web host stop operation timed out after 3 seconds", "API");
                    }
                    
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
        public static async Task RestartAsync()
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
        private static void UpdateStatus(string status, Color color)
        {
            StatusChanged?.Invoke(status, color);
        }
    }
}