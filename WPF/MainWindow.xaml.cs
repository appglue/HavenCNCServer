using System;
using System.Windows;
using System.ComponentModel;
using System.Threading;
using HavenCNCServer.Services;
using HavenCNCServer.Centroid;
using HavenCNCServer.Centroid.Events;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private static bool _isShuttingDown = false;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Position on right side of screen, full height (matching WinForms MainForm)
        var workingArea = SystemParameters.WorkArea;

        this.Width = 650;
        this.Height = workingArea.Height;
        this.Left = workingArea.Width - 650;
        this.Top = 0;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Prevent multiple shutdown attempts
        if (_isShuttingDown)
        {
            base.OnClosing(e);
            return;
        }

        _isShuttingDown = true;

        // Perform synchronous shutdown (same pattern as WinForms)
        try
        {
            LogInfo("Application shutdown initiated", "System");

            // Get the cancellation token source from ProgramWPF
            ProgramWPF.CancelAllOperations();

            // Stop CNC Job Info Listener with timeout
            var shutdownTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            CNCJobInfoListener.Stop(shutdownTokenSource.Token);

            // Clear all event listeners
            CNCJobInfoListener.ClearAllListeners();

            // Stop API manager synchronously
            try
            {
                var stopTask = ApiManager.StopAsync(CancellationToken.None);
                if (!stopTask.Wait(TimeSpan.FromSeconds(3)))
                {
                    LogWarning("API manager stop timed out", "System");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error stopping API manager: {ex.Message}", "System");
            }

            // Cleanup the CNC connection manager
            CNCConnectionManager.Disconnect();

            LogSuccess("Application shutdown completed", "System");

            // Small delay to let logs flush
            System.Threading.Thread.Sleep(200);
        }
        catch (Exception ex)
        {
            LogError($"Error during shutdown: {ex.Message}", "System");
        }

        base.OnClosing(e);

        // Force application exit after cleanup
        Environment.Exit(0);
    }
}
