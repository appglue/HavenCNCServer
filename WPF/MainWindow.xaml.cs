using System.Windows;

namespace HavenCNCServer.WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
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
}
