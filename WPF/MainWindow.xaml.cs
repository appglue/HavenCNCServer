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

        LogInfo("*** MainWindow closing - starting shutdown sequence ***", "System");

        // Run shutdown on background thread to avoid UI thread blocking issues
        var shutdownThread = new System.Threading.Thread(() =>
        {
            try
            {
                ProgramWPF.CleanupBeforeShutdown();
            }
            catch (Exception ex)
            {
                LogError($"Error during shutdown: {ex.Message}", "System");
                Services.ShutdownManager.ForceShutdown();
            }
        })
        {
            Name = "Shutdown-Thread",
            IsBackground = false // Not a background thread - we want to wait for it
        };

        shutdownThread.Start();

        // This code will never be reached because shutdown calls Application.Shutdown()
    }
}
