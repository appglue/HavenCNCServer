using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HavenCNCServer
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                // Set up global exception handlers for GUI mode
                if (args.Length == 0 || !IsConsoleMode(args))
                {
                    SetupGlobalExceptionHandlers();
                }

                // Check for command line arguments
                if (args.Length > 0)
                {
                    switch (args[0].ToLowerInvariant())
                    {
                        case "--generate-openapi":
                        case "-g":
                            GenerateOpenApiAsync().GetAwaiter().GetResult();
                            return;
                        case "--help":
                        case "-h":
                        case "/?":
                            ShowHelp();
                            return;
                        default:
                            Console.WriteLine($"Unknown argument: {args[0]}");
                            Console.WriteLine("Use --help or -h for usage information.");
                            Environment.Exit(1);
                            return;
                    }
                }

                // Normal GUI mode
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                if (args.Length > 0)
                {
                    // Console mode - write to console
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.WriteLine($"Details: {ex}");
                    Environment.Exit(1);
                }
                else
                {
                    // GUI mode - show message box
                    MessageBox.Show($"Application startup error: {ex.Message}\n\nDetails: {ex}", 
                        "Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Check if the application is running in console mode based on arguments
        /// </summary>
        private static bool IsConsoleMode(string[] args)
        {
            if (args.Length == 0) return false;
            
            var firstArg = args[0].ToLowerInvariant();
            return firstArg == "--generate-openapi" || firstArg == "-g" || 
                   firstArg == "--help" || firstArg == "-h" || firstArg == "/?";
        }

        /// <summary>
        /// Set up global exception handlers to prevent application crashes
        /// </summary>
        private static void SetupGlobalExceptionHandlers()
        {
            // Handle unhandled exceptions in the main UI thread
            Application.ThreadException += Application_ThreadException;
            
            // Set the unhandled exception mode to force all Windows Forms errors to go through our handler
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            
            // Handle unhandled exceptions in other threads
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        /// <summary>
        /// Handle exceptions in the main UI thread (Windows Forms)
        /// </summary>
        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            HandleException(e.Exception, "UI Thread Exception", false);
        }

        /// <summary>
        /// Handle unhandled exceptions in other threads
        /// </summary>
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleException(e.ExceptionObject as Exception, "Unhandled Exception", e.IsTerminating);
        }

        /// <summary>
        /// Common exception handling logic
        /// </summary>
        private static void HandleException(Exception? ex, string title, bool isTerminating)
        {
            try
            {
                if (ex == null)
                {
                    ex = new Exception("Unknown exception occurred");
                }

                // Log the exception details
                var errorMessage = $"{title}: {ex.Message}";
                var fullDetails = $"Exception Type: {ex.GetType().FullName}\n" +
                                 $"Message: {ex.Message}\n" +
                                 $"Stack Trace:\n{ex.StackTrace}";

                // Try to log using the application's logging system if available
                try
                {
                    HavenCNCServer.Services.LoggingService.LogError(errorMessage, "GlobalHandler");
                    HavenCNCServer.Services.LoggingService.LogError($"Full details: {fullDetails}", "GlobalHandler");
                }
                catch
                {
                    // If logging fails, fall back to console/debug output
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {errorMessage}");
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {fullDetails}");
                }

                // Show user-friendly message
                var userMessage = isTerminating 
                    ? $"A critical error occurred and the application must close:\n\n{ex.Message}\n\nPlease restart the application."
                    : $"An error occurred but the application can continue:\n\n{ex.Message}\n\nThe error has been logged. If this problem persists, please restart the application.";

                var messageType = isTerminating ? MessageBoxIcon.Error : MessageBoxIcon.Warning;
                var messageTitle = isTerminating ? "Critical Error" : "Application Error";

                MessageBox.Show(userMessage, messageTitle, MessageBoxButtons.OK, messageType);

                // If this is a terminating exception, exit gracefully
                if (isTerminating)
                {
                    Environment.Exit(1);
                }
            }
            catch (Exception handlerEx)
            {
                // Last resort: even our exception handler failed
                try
                {
                    MessageBox.Show($"A critical error occurred in the error handler:\n{handlerEx.Message}\n\nOriginal error: {ex?.Message ?? "Unknown"}\n\nThe application will now close.",
                        "Critical System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    Environment.Exit(1);
                }
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine("HavenCNCServer - CNC Server Management Application");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  HavenCNCServer.exe                    Start the GUI application");
            Console.WriteLine("  HavenCNCServer.exe --generate-openapi Generate OpenAPI specification");
            Console.WriteLine("  HavenCNCServer.exe --help             Show this help message");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --generate-openapi, -g    Generate OpenAPI/Swagger specification");
            Console.WriteLine("                            Starts the server, downloads the spec, then exits");
            Console.WriteLine("  --help, -h, /?            Show this help message");
            Console.WriteLine();
            Console.WriteLine("When running normally, the application provides:");
            Console.WriteLine("  - REST API server on http://localhost:5000");
            Console.WriteLine("  - Swagger UI at http://localhost:5000/swagger");
            Console.WriteLine("  - Web interface at http://localhost:5000");
        }

        private static async Task GenerateOpenApiAsync()
        {
            Console.WriteLine("HavenCNCServer OpenAPI Generator");
            Console.WriteLine("================================");
            Console.WriteLine();

            var outputFile = "openapi.json";
            
            // Create backup of existing file if it exists
            if (File.Exists(outputFile))
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFile = $"openapi.backup.{timestamp}.json";
                
                Console.WriteLine($"Creating backup of existing file...");
                File.Copy(outputFile, backupFile);
                Console.WriteLine($"✓ Backup created: {backupFile}");
                Console.WriteLine();
            }

            var host = CreateApiHost();
            
            try
            {
                Console.WriteLine("Starting API server...");
                await host.StartAsync();
                
                // Wait a moment for the server to fully initialize
                await Task.Delay(2000);
                
                Console.WriteLine("Downloading OpenAPI specification...");
                
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                
                var openApiJson = await client.GetStringAsync("http://localhost:5000/swagger/v1/swagger.json");
                
                await File.WriteAllTextAsync(outputFile, openApiJson);
                
                Console.WriteLine($"✓ OpenAPI specification saved to: {outputFile}");
                Console.WriteLine($"✓ File size: {new FileInfo(outputFile).Length} bytes");
                Console.WriteLine();
                Console.WriteLine("You can also access the live documentation at:");
                Console.WriteLine("  - Swagger UI: http://localhost:5000/swagger");
                Console.WriteLine("  - OpenAPI JSON: http://localhost:5000/swagger/v1/swagger.json");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error generating OpenAPI specification: {ex.Message}");
                Environment.Exit(1);
            }
            finally
            {
                Console.WriteLine();
                Console.WriteLine("Stopping server...");
                await host.StopAsync();
                host.Dispose();
                Console.WriteLine("✓ Done!");
            }
        }

        private static IHost CreateApiHost()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseUrls("http://localhost:5000")
                        .UseStartup<ApiStartup>()
                        .ConfigureLogging(logging =>
                        {
                            logging.ClearProviders();
                            // Suppress logging in console mode
                        });
                })
                .Build();
        }
    }
}
