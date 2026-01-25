using CommunityToolkit.Mvvm.ComponentModel;

namespace HavenCNCServer.WPF.ViewModels;

/// <summary>
/// Base class for all ViewModels providing common functionality
/// </summary>
public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? statusMessage;

    /// <summary>
    /// Updates the status message displayed in the UI
    /// </summary>
    protected void UpdateStatus(string message)
    {
        StatusMessage = message;
    }
}
