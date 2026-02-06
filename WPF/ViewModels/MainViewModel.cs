using System;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HavenCNCServer.Centroid;
using HavenCNCServer.Services;
using HavenCNCServer.Models;
using HavenCNCServer.WPF.Views;

namespace HavenCNCServer.WPF.ViewModels;

/// <summary>
/// ViewModel for the main application window
/// </summary>
public partial class MainViewModel : BaseViewModel
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

    partial void OnRequiresResetChanged(bool value)
    {
        ResetButtonLabel = value ? "RESET" : "ESTOP";
    }

    public MainViewModel()
    {
        try
        {
            // Subscribe to CNC connection events
            CNCConnectionManager.ConnectionStatusChanged += OnConnectionStatusChanged;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MainViewModel constructor error: {ex}");
            Console.WriteLine($"MainViewModel constructor error: {ex}");
            throw new Exception($"MainViewModel initialization failed: {ex.Message}", ex);
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
}
