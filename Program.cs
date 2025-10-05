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
