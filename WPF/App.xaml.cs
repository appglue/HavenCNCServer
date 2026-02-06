using System;
using System.Windows;

namespace HavenCNCServer.WPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    /// <summary>
    /// Handles application startup and adds global exception handlers
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        // Add global exception handler
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            Console.WriteLine($"Unhandled exception: {ex?.Message}");
            Console.WriteLine(ex?.StackTrace);

            // Don't show error dialogs during shutdown - we have logging
            if (!Services.ShutdownManager.IsShuttingDown)
            {
                System.Windows.MessageBox.Show($"Fatal error: {ex?.Message}\n\n{ex?.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };

        DispatcherUnhandledException += (s, args) =>
        {
            Console.WriteLine($"Dispatcher exception: {args.Exception.Message}");
            Console.WriteLine(args.Exception.StackTrace);

            // Don't show error dialogs during shutdown - we have logging
            if (!Services.ShutdownManager.IsShuttingDown)
            {
                System.Windows.MessageBox.Show($"Application error: {args.Exception.Message}\n\n{args.Exception.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            args.Handled = true;
        };

        base.OnStartup(e);
    }
}
