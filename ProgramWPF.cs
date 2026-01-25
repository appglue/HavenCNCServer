using System;
using HavenCNCServer.Centroid;
using HavenCNCServer.Services;

namespace HavenCNCServer
{
    /// <summary>
    /// Entry point for the WPF UI version of HavenCNC Server
    /// </summary>
    internal static class ProgramWPF
    {
        public static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting HavenCNC Server (WPF UI)...");

                // Initialize the same backend services as WinForms
                InitializeBackend();

                // Start the WPF application
                var app = new WPF.App();
                app.InitializeComponent();
                app.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error starting WPF UI: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Environment.Exit(1);
            }
        }

        private static void InitializeBackend()
        {
            try
            {
                // Deploy scripts on startup (same as MainForm does)
                ScriptDeploymentService.DeployScriptsToCnc12();
                Console.WriteLine("Scripts deployed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Script deployment failed: {ex.Message}");
            }

            try
            {
                // Try to connect to CNC
                _ = CNCConnectionManager.TryAutoConnectAsync();
                Console.WriteLine("CNC auto-connect initiated");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: CNC connection failed: {ex.Message}");
            }
        }
    }
}
