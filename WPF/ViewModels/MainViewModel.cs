using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HavenCNCServer.Centroid;
using HavenCNCServer.Centroid.Events;
using HavenCNCServer.Services;
using static HavenCNCServer.Services.LoggingService;
using HavenCNCServer.Models;
using HavenCNCServer.WPF.Views;

namespace HavenCNCServer.WPF.ViewModels;

/// <summary>
/// ViewModel for the main application window
/// </summary>
public partial class MainViewModel : BaseViewModel, IEventSubscriber
{
    [ObservableProperty]
    private bool isConnected;

    [ObservableProperty]
    private bool isCnc12Running;

    [ObservableProperty]
    private int connectionRetryCount;

    [ObservableProperty]
    private bool isAlwaysOnTop;

    [ObservableProperty]
    private bool isToolCheckActive;

    [ObservableProperty]
    private bool isFeedHoldActive;

    [ObservableProperty]
    private bool isMachineHomed;

    [ObservableProperty]
    private string resetButtonLabel = "ESTOP";

    [ObservableProperty]
    private bool requiresReset;

    // CNC12 control computed properties
    public bool CanStartCnc12 => !IsCnc12Running;
    public bool CanRestartCnc12 => IsCnc12Running;
    public bool CanShutdownCnc12 => IsCnc12Running;

    // Timer to monitor CNC12 process status
    private DispatcherTimer? _cnc12MonitorTimer;

    // When true, the user explicitly shut down CNC12 — suppress auto-restart
    private bool _userInitiatedShutdown = false;

    partial void OnRequiresResetChanged(bool value)
    {
        ResetButtonLabel = value ? "RESET" : "ESTOP";
    }

    partial void OnIsCnc12RunningChanged(bool value)
    {
        // Update computed properties when CNC12 running state changes
        OnPropertyChanged(nameof(CanStartCnc12));
        OnPropertyChanged(nameof(CanRestartCnc12));
        OnPropertyChanged(nameof(CanShutdownCnc12));
    }

    public MainViewModel()
    {
        try
        {
            // Subscribe to CNC connection events
            CNCConnectionManager.ConnectionStatusChanged += OnConnectionStatusChanged;

            // Subscribe to CNCEventBus for server status updates
            CNCEventBus.Instance.Subscribe(this);

            // Start timer to monitor CNC12 process status every 2 seconds
            _cnc12MonitorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _cnc12MonitorTimer.Tick += Cnc12MonitorTimer_Tick;
            _cnc12MonitorTimer.Start();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MainViewModel constructor error: {ex}");
            Console.WriteLine($"MainViewModel constructor error: {ex}");
            throw new Exception($"MainViewModel initialization failed: {ex.Message}", ex);
        }
    }

    private async void Cnc12MonitorTimer_Tick(object? sender, EventArgs e)
    {
        bool wasRunning = IsCnc12Running;

        // Refresh running state from the actual process list
        await UpdateCnc12RunningState();

        // If CNC12 just stopped and the user did NOT intentionally shut it down, restart it
        if (wasRunning && !IsCnc12Running && !_userInitiatedShutdown)
        {
            Log("CNC12 stopped unexpectedly — auto-restarting...", LoggingService.LogLevel.Warning, "MainViewModel");
            UpdateStatus("CNC12 stopped unexpectedly. Restarting...");

            var started = await Task.Run(() => CNCUtils.LaunchCNC12());
            if (started)
            {
                UpdateStatus("CNC12 restarted automatically");
                await Task.Delay(3000);
                await UpdateCnc12RunningState();
                await CNCConnectionManager.ConnectAsync();
            }
            else
            {
                UpdateStatus("Failed to auto-restart CNC12");
            }
        }
    }

    [RelayCommand]
    private void ToggleAlwaysOnTop()
    {
        IsAlwaysOnTop = !IsAlwaysOnTop;
        if (System.Windows.Application.Current.MainWindow != null)
        {
            System.Windows.Application.Current.MainWindow.Topmost = IsAlwaysOnTop;
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        try
        {
            var window = new SettingsWindow { Owner = System.Windows.Application.Current.MainWindow };
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error opening settings: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ShowLogs()
    {
        try
        {
            var window = new LogsWindow { Owner = System.Windows.Application.Current.MainWindow };
            window.Show();
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error opening logs: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ShowMessages()
    {
        try
        {
            var window = new MessagesWindow { Owner = System.Windows.Application.Current.MainWindow };
            window.Show();
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error opening messages: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ShowGCode()
    {
        try
        {
            var window = new GCodeWindow { Owner = System.Windows.Application.Current.MainWindow };
            window.Show();
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error opening G-Code viewer: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ClearMessages()
    {
        try
        {
            // Find the MessageDisplayControl in the main window and clear it
            if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
            {
                var messageControl = FindVisualChild<HavenCNCServer.WPF.Controls.MessageDisplayControl>(mainWindow);
                messageControl?.ClearMessages();
                UpdateStatus("Messages cleared");
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error clearing messages: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ClearLogs()
    {
        try
        {
            // Find the LogViewerControl in the main window and clear it
            if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
            {
                var logControl = FindVisualChild<HavenCNCServer.WPF.Controls.LogViewerControl>(mainWindow);
                logControl?.Clear();
                UpdateStatus("Logs cleared");
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error clearing logs: {ex.Message}");
        }
    }

    /// <summary>
    /// Helper method to find a visual child of a specific type
    /// </summary>
    private static T? FindVisualChild<T>(System.Windows.DependencyObject parent) where T : System.Windows.DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
            {
                return typedChild;
            }

            var result = FindVisualChild<T>(child);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }

    [RelayCommand]
    private void OpenBrowserUI()
    {
        try
        {
            var window = new BrowserWindow { Owner = System.Windows.Application.Current.MainWindow };
            window.Show();
            UpdateStatus("Browser window opened");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Failed to open browser: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenSwagger()
    {
        try
        {
            var url = "http://localhost:5000/swagger";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            UpdateStatus($"Opening Swagger UI: {url}");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Failed to open Swagger: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ShowGCodeTest()
    {
        try
        {
            var window = new GCodeTestWindow { Owner = System.Windows.Application.Current.MainWindow };
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error opening G-Code test: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenDataFolder()
    {
        try
        {
            // Use centralized data directory from settings
            var dataPath = Services.SettingsManager.Settings.Files.DataDirectory;
            if (string.IsNullOrEmpty(dataPath))
            {
                dataPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "data");
            }

            if (!System.IO.Directory.Exists(dataPath))
            {
                System.IO.Directory.CreateDirectory(dataPath);
            }
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = dataPath,
                UseShellExecute = true
            });
            UpdateStatus($"Opening data folder: {dataPath}");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Failed to open data folder: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ResetButton()
    {
        if (RequiresReset)
        {
            // Machine needs reset - trigger RESET_UI (56)
            await Task.Run(() => CNCUtils.StartSkinEvent(SkinEvent.ResetButtonPressed));
            await Task.Delay(100);
            await Task.Run(() => CNCUtils.StopSkinEvent(SkinEvent.ResetButtonPressed));
        }
        else
        {
            // Machine is running - trigger ESTOP_UI (58)
            await Task.Run(() => CNCUtils.StartSkinEvent((SkinEvent)58));
            await Task.Delay(100);
            await Task.Run(() => CNCUtils.StopSkinEvent((SkinEvent)58));
        }
    }

    [RelayCommand]
    private async Task StopButton()
    {
        await Task.Run(() => CNCUtils.StartSkinEvent((SkinEvent)47)); // STOP_UI
        await Task.Delay(100);
        await Task.Run(() => CNCUtils.StopSkinEvent((SkinEvent)47));
    }

    [RelayCommand]
    private async Task CancelCycleButton()
    {
        await Task.Run(() => CNCUtils.StartSkinEvent(SkinEvent.CycleCancel)); // Event 46
        await Task.Delay(100);
        await Task.Run(() => CNCUtils.StopSkinEvent(SkinEvent.CycleCancel));
    }

    [RelayCommand]
    private async Task StartCycleButton()
    {
        // Show warning dialog
        var result = System.Windows.MessageBox.Show(
            "START CYCLE is generally not used like it is in CNC12 and can cause unexpected side effects.\n\n" +
            "It is provided here only for special cases where it may be needed.\n\n" +
            "Do you want to start the cycle anyway?",
            "Start Cycle Warning",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (result != MessageBoxResult.Yes)
        {
            return; // User cancelled
        }

        await Task.Run(() => CNCUtils.StartSkinEvent(SkinEvent.CycleStart)); // Event 50
        await Task.Delay(100);
        await Task.Run(() => CNCUtils.StopSkinEvent(SkinEvent.CycleStart));
    }

    [RelayCommand]
    private async Task ToolCheckToggle()
    {
        if (IsToolCheckActive)
        {
            await Task.Run(() => CNCUtils.StartSkinEvent((SkinEvent)48)); // TOOL_CHECK_UI
        }
        else
        {
            await Task.Run(() => CNCUtils.StopSkinEvent((SkinEvent)48));
        }
    }

    [RelayCommand]
    private async Task FeedHoldToggle()
    {
        if (IsFeedHoldActive)
        {
            await Task.Run(() => CNCUtils.StartSkinEvent((SkinEvent)49)); // FEED_HOLD_UI
        }
        else
        {
            await Task.Run(() => CNCUtils.StopSkinEvent((SkinEvent)49));
        }
    }

    [RelayCommand]
    private async Task HomedToggle()
    {
        if (IsMachineHomed)
        {
            await Task.Run(() => CNCUtils.StartSkinEvent((SkinEvent)57)); // MACHINE_HOMED_UI
        }
        else
        {
            await Task.Run(() => CNCUtils.StopSkinEvent((SkinEvent)57));
        }
    }

    [RelayCommand]
    private async Task StartCnc12()
    {
        try
        {
            UpdateStatus("Starting CNC12...");

            var success = await Task.Run(() => CNCUtils.LaunchCNC12());

            if (success)
            {
                UpdateStatus("CNC12 started successfully");
                // Give CNC12 time to initialize
                await Task.Delay(3000);

                // Update the running state
                await UpdateCnc12RunningState();

                // Try to connect
                UpdateStatus("Connecting to CNC12...");
                await CNCConnectionManager.ConnectAsync();
            }
            else
            {
                UpdateStatus("Failed to start CNC12");
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error starting CNC12: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ShutdownCnc12()
    {
        try
        {
            // Flag that this is an intentional shutdown so the monitor does not auto-restart
            _userInitiatedShutdown = true;

            UpdateStatus("Shutting down CNC12...");

            var success = await Task.Run(() => CNCUtils.ExitCNC12());

            if (success)
            {
                UpdateStatus("CNC12 shutdown command sent");
                // Give it time to shut down
                await Task.Delay(2000);

                // Update the running state
                await UpdateCnc12RunningState();
            }
            else
            {
                UpdateStatus("Failed to send shutdown command to CNC12");
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error shutting down CNC12: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task RestartCnc12()
    {
        try
        {
            var result = System.Windows.MessageBox.Show(
                "Are you sure you want to restart CNC12?\n\nThis will close and reopen the CNC12 application.",
                "Confirm Restart",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            UpdateStatus("Restarting CNC12...");

            // Suppress auto-restart while we intentionally stop it
            _userInitiatedShutdown = true;

            // First, shut down CNC12
            var shutdownSuccess = await Task.Run(() => CNCUtils.ExitCNC12());

            if (shutdownSuccess)
            {
                UpdateStatus("CNC12 shutdown, waiting before restart...");
                // Wait for shutdown to complete
                await Task.Delay(3000);

                // Update the running state
                await UpdateCnc12RunningState();

                // Relaunch — this is not a user-initiated permanent shutdown
                _userInitiatedShutdown = false;

                // Now start it again
                UpdateStatus("Starting CNC12...");
                var startSuccess = await Task.Run(() => CNCUtils.LaunchCNC12());

                if (startSuccess)
                {
                    UpdateStatus("CNC12 restarted successfully");
                    // Give CNC12 time to initialize
                    await Task.Delay(3000);

                    // Update the running state
                    await UpdateCnc12RunningState();

                    // Try to connect
                    UpdateStatus("Connecting to CNC12...");
                    await CNCConnectionManager.ConnectAsync();
                }
                else
                {
                    UpdateStatus("Failed to restart CNC12");
                }
            }
            else
            {
                UpdateStatus("Failed to shutdown CNC12 for restart");
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error restarting CNC12: {ex.Message}");
        }
    }

    /// <summary>
    /// Update the CNC12 running state by checking the process directly
    /// </summary>
    private async Task UpdateCnc12RunningState()
    {
        await Task.Run(() =>
        {
            var cnc12ProcessName = SettingsManager.Settings.Cnc.Cnc12ProcessName;
            var processes = System.Diagnostics.Process.GetProcessesByName(cnc12ProcessName);
            bool isRunning = processes.Length > 0;

            // Clean up process handles
            foreach (var p in processes)
            {
                try { p.Dispose(); } catch { }
            }

            // Update on UI thread
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                IsCnc12Running = isRunning;
            });
        });
    }

    [RelayCommand]
    private void Exit()
    {
        System.Windows.Application.Current.Shutdown();
    }

    private void OnConnectionStatusChanged(bool connected, string message)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            IsConnected = connected;
            UpdateStatus(message);

            // Update CNC12 running status
            IsCnc12Running = CNCConnectionManager.IsCnc12ProcessRunning;
            ConnectionRetryCount = CNCConnectionManager.ConnectionRetryCount;
        });
    }

    // IEventSubscriber implementation
    public EventTypeFlags GetSubscribedEvents()
    {
        return EventTypeFlags.ServerStatus;
    }

    public void OnPositionUpdate(DROEvent position)
    {
        // Not interested in position updates
    }

    public void OnLogMessage(LogEvent log)
    {
        // Not interested in log messages
    }

    public void OnCNCMessage(ICentroidEvent message)
    {
        // Not interested in CNC messages
    }

    public void OnServerStatus(ServerStatusEvent status)
    {
        // Update RequiresReset based on server status
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            RequiresReset = status.RequiresReset;
        });
    }
}
