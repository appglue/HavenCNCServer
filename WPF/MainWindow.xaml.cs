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

        LogInfo("MainWindow closing...", "System");

        // Allow window to close immediately - don't block UI thread
        base.OnClosing(e);

        // Trigger cleanup asynchronously without blocking
        System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                LogInfo("Starting async cleanup...", "System");
                ProgramWPF.CleanupBeforeShutdown();
            }
            catch (Exception ex)
            {
                LogError($"Error during async cleanup: {ex.Message}", "System");
            }
        });

        // Let App.Shutdown happen naturally - App_Exit will handle the rest
    }
}
