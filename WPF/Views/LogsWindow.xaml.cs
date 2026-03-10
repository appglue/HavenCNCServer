using System;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using HavenCNCServer.Services;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.WPF.Views
{
    public partial class LogsWindow : Window
    {
        private const int MaxLines = 10000; // Large limit for log window
        private LogsWindowTarget? _logTarget;

        public LogsWindow()
        {
            InitializeComponent();

            // Register this window as a log target
            _logTarget = new LogsWindowTarget(this);
            LoggingService.AddTarget(_logTarget);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Set window to screen height and position at top-left
            var screenHeight = SystemParameters.WorkArea.Height;
            var screenLeft = SystemParameters.WorkArea.Left;
            var screenTop = SystemParameters.WorkArea.Top;

            this.Height = screenHeight - 50;
            this.Width = 1000;
            this.Left = screenLeft + 50; // Offset from left edge
            this.Top = screenTop; // At the top of work area

            // Scroll to bottom when window opens
            txtLogs.ScrollToEnd();
        }

        private void BtnCopyLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var text = new System.Windows.Documents.TextRange(
                    txtLogs.Document.ContentStart,
                    txtLogs.Document.ContentEnd).Text;

                if (!string.IsNullOrWhiteSpace(text))
                {
                    System.Windows.Clipboard.SetText(text);
                    LogInfo("Logs copied to clipboard", "LogsWindow");
                    System.Windows.MessageBox.Show("Logs copied to clipboard", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show("No logs to copy", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                LogError($"Error copying logs: {ex.Message}", "LogsWindow");
                System.Windows.MessageBox.Show($"Failed to copy logs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClearLogs_Click(object sender, RoutedEventArgs e)
        {
            // Clear all logs from LoggingService (this will trigger target updates)
            LoggingService.Clear();
            LogInfo("All logs cleared by user", "LogsWindow");
        }

        public void AppendLogEntry(string text, System.Drawing.Color color)
        {
            // CRITICAL FIX: Use BeginInvoke to prevent deadlocks
            Dispatcher.BeginInvoke(() =>
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
            // CRITICAL FIX: Use BeginInvoke to prevent deadlocks
            Dispatcher.BeginInvoke(() =>
            {
                txtLogs.Document.Blocks.Clear();
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            // Unregister log target when window closes
            if (_logTarget != null)
            {
                _logTarget.Dispose();
                _logTarget = null;
            }
            base.OnClosed(e);
        }
    }

    /// <summary>
    /// Log target implementation for LogsWindow
    /// </summary>
    public class LogsWindowTarget : ILogTarget
    {
        private readonly LogsWindow _window;
        private int _lastDisplayedCount = 0;
        public bool IsDisposed { get; private set; }

        public LogsWindowTarget(LogsWindow window)
        {
            _window = window;
        }

        public void ResetCount()
        {
            _lastDisplayedCount = 0;
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
                        _window.AppendLogEntry(text, color);
                    }
                    _lastDisplayedCount = newCount;
                }
                else if (newCount < _lastDisplayedCount)
                {
                    // Logs were cleared, rebuild
                    _window.Clear();
                    _lastDisplayedCount = 0;

                    foreach (var entry in entryList)
                    {
                        var text = entry.FormatForDisplay();
                        var color = GetColorForLogLevel(entry.Level);
                        _window.AppendLogEntry(text, color);
                    }
                    _lastDisplayedCount = newCount;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating LogsWindow target: {ex.Message}");
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
