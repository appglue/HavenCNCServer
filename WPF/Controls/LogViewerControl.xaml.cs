using System;
using System.Linq;
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
        private const int MaxLines = 50;

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
        private int _lastDisplayedCount = 0;
        public bool IsDisposed { get; private set; }

        public WpfLogTarget(LogViewerControl control)
        {
            _control = control;
        }

        public void UpdateLog(System.Collections.Generic.IEnumerable<LoggingService.LogEntry> entries)
        {
            if (IsDisposed) return;

            try
            {
                var entryList = entries.ToList();
                int newCount = entryList.Count;

                // Only append new entries since last update
                if (newCount > _lastDisplayedCount)
                {
                    var newEntries = entryList.Skip(_lastDisplayedCount);
                    foreach (var entry in newEntries)
                    {
                        var text = entry.FormatForDisplay();
                        var color = GetColorForLogLevel(entry.Level);
                        _control.AppendLogEntry(text, color);
                    }
                    _lastDisplayedCount = newCount;
                }
                else if (newCount < _lastDisplayedCount)
                {
                    // Logs were cleared, rebuild
                    _control.Clear();
                    _lastDisplayedCount = 0;

                    foreach (var entry in entryList)
                    {
                        var text = entry.FormatForDisplay();
                        var color = GetColorForLogLevel(entry.Level);
                        _control.AppendLogEntry(text, color);
                    }
                    _lastDisplayedCount = newCount;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating WPF log target: {ex.Message}");
            }
        }

        private static System.Drawing.Color GetColorForLogLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Success => System.Drawing.Color.FromArgb(106, 230, 106),   // bright green
                LogLevel.Error => System.Drawing.Color.FromArgb(255, 100, 100),   // soft bright red
                LogLevel.Warning => System.Drawing.Color.FromArgb(255, 210, 50),   // amber/gold
                LogLevel.Info => System.Drawing.Color.FromArgb(212, 212, 212),   // light gray
                LogLevel.Debug => System.Drawing.Color.FromArgb(128, 128, 128),   // dimmed gray
                _ => System.Drawing.Color.FromArgb(212, 212, 212)
            };
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
