using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using HavenCNCServer.Services;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.WPF.Controls
{
    /// <summary>
    /// WPF control for displaying application logs with color coding
    /// </summary>
    public partial class LogViewerControl : System.Windows.Controls.UserControl
    {
        private const int MaxLines = 10000;

        public LogViewerControl()
        {
            InitializeComponent();

            // Register this control as a log target
            var logTarget = new WpfLogTarget(this);
            LoggingService.AddTarget(logTarget);
        }

        public void AppendLogEntry(string text, System.Drawing.Color color)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    // Convert System.Drawing.Color to WPF Color
                    var wpfColor = System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);

                    var paragraph = new Paragraph(new Run(text)
                    {
                        Foreground = new SolidColorBrush(wpfColor)
                    });
                    paragraph.Margin = new Thickness(0);
                    txtLogs.Document.Blocks.Add(paragraph);

                    // Trim old lines if we exceed max
                    while (txtLogs.Document.Blocks.Count > MaxLines)
                    {
                        txtLogs.Document.Blocks.Remove(txtLogs.Document.Blocks.FirstBlock);
                    }

                    txtLogs.ScrollToEnd();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error appending log entry: {ex.Message}");
                }
            });
        }

        public void Clear()
        {
            Dispatcher.Invoke(() =>
            {
                txtLogs.Document.Blocks.Clear();
            });
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // Could unregister log target here if needed
        }
    }

    /// <summary>
    /// Log target implementation for WPF LogViewerControl
    /// </summary>
    public class WpfLogTarget : ILogTarget
    {
        private readonly LogViewerControl _control;
        public bool IsDisposed { get; private set; }

        public WpfLogTarget(LogViewerControl control)
        {
            _control = control;
        }

        public void Log(string message, System.Drawing.Color color)
        {
            _control.AppendLogEntry(message, color);
        }

        public void UpdateLog(System.Collections.Generic.IEnumerable<LoggingService.LogEntry> entries)
        {
            // Not used for this implementation - we use direct Log() calls
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
