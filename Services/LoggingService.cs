using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace HavenCNCServer.Services
{
    /// <summary>
    /// Centralized logging service for the application.
    /// Log() is lock-free: entries are enqueued and written to file immediately.
    /// A background timer dispatches entries to UI targets without holding any lock.
    /// </summary>
    public static class LoggingService
    {
        // Lock-free queue - Log() just enqueues, nothing else
        private static readonly ConcurrentQueue<LogEntry> _pendingEntries = new ConcurrentQueue<LogEntry>();

        // Only ever touched by the dispatch timer thread - no locks needed
        private static readonly List<LogEntry> _logEntries = new List<LogEntry>();
        private static readonly List<ILogTarget> _targets = new List<ILogTarget>();

        // File writer - only written by the dispatch timer thread
        private static StreamWriter? _fileWriter;

        // Timer to dispatch pending entries to UI targets
        private static readonly System.Threading.Timer _dispatchTimer;

        static LoggingService()
        {
            // Set up file logging
            try
            {
                var logDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "HavenCNCServer", "Logs");
                Directory.CreateDirectory(logDir);
                var logFile = Path.Combine(logDir, $"havenCNC_{DateTime.Now:yyyyMMdd}.log");
                _fileWriter = new StreamWriter(logFile, append: true, encoding: Encoding.UTF8) { AutoFlush = true };
            }
            catch { /* file logging unavailable */ }

            // Dispatch pending entries to UI every 100ms - no locks anywhere
            _dispatchTimer = new System.Threading.Timer(DispatchPendingEntries, null, 100, 100);
        }

        /// <summary>
        /// Drains the pending queue, writes to file, updates _logEntries and all targets.
        /// Only this timer callback touches _logEntries, _targets and _fileWriter - no locks needed.
        /// </summary>
        private static void DispatchPendingEntries(object? _)
        {
            if (_pendingEntries.IsEmpty) return;

            while (_pendingEntries.TryDequeue(out var entry))
            {
                // Write to file
                try { _fileWriter?.WriteLine(entry.FormatForDisplay()); }
                catch { }

                // Add to in-memory list
                _logEntries.Add(entry);
                while (_logEntries.Count > MaxLogEntries)
                    _logEntries.RemoveAt(0);
            }

            // Remove disposed targets
            _targets.RemoveAll(t => t.IsDisposed);

            // Update all targets - no lock held
            foreach (var target in _targets)
            {
                try { target.UpdateLog(_logEntries); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error updating log target: {ex.Message}"); }
            }
        }

        /// <summary>
        /// Maximum number of log entries to keep in memory
        /// </summary>
        public static int MaxLogEntries { get; set; } = 2000;

        /// <summary>
        /// Maximum number of log entries to display in UI (to prevent flickering)
        /// </summary>
        public static int MaxDisplayEntries { get; set; } = 50;

        /// <summary>
        /// Event fired when a new log entry is added
        /// </summary>
        public static event Action<LogEntry>? LogEntryAdded;

        /// <summary>
        /// Represents a single log entry
        /// </summary>
        public class LogEntry
        {
            /// <summary>
            /// Gets or sets the timestamp when the log entry was created
            /// </summary>
            public DateTime Timestamp { get; set; }

            /// <summary>
            /// Gets or sets the log message content
            /// </summary>
            public string Message { get; set; }

            /// <summary>
            /// Gets or sets the log level of this entry
            /// </summary>
            public LogLevel Level { get; set; }

            /// <summary>
            /// Gets or sets the source component that generated this log entry
            /// </summary>
            public string Source { get; set; }

            /// <summary>
            /// Initializes a new instance of the LogEntry class
            /// </summary>
            /// <param name="message">The log message</param>
            /// <param name="level">The log level</param>
            /// <param name="source">The source component</param>
            public LogEntry(string message, LogLevel level = LogLevel.Info, string source = "System")
            {
                Timestamp = DateTime.Now;
                Message = message ?? string.Empty;
                Level = level;
                Source = source;
            }

            /// <summary>
            /// Format the log entry for display
            /// </summary>
            public string FormatForDisplay()
            {
                var levelIcon = Level switch
                {
                    LogLevel.Error => "✗",
                    LogLevel.Warning => "⚠",
                    LogLevel.Success => "✓",
                    LogLevel.Info => "ℹ",
                    LogLevel.Debug => "🔍",
                    _ => ""
                };

                var timestamp = Timestamp.ToString("HH:mm:ss");
                var sourcePrefix = !string.IsNullOrEmpty(Source) && Source != "System" ? $"[{Source}] " : "";

                return $"[{timestamp}] {levelIcon} {sourcePrefix}{Message}";
            }
        }

        /// <summary>
        /// Log level enumeration
        /// </summary>
        public enum LogLevel
        {
            /// <summary>
            /// Debug level for detailed diagnostic information
            /// </summary>
            Debug,

            /// <summary>
            /// Information level for general application flow
            /// </summary>
            Info,

            /// <summary>
            /// Success level for successful operations
            /// </summary>
            Success,

            /// <summary>
            /// Warning level for potentially harmful situations
            /// </summary>
            Warning,

            /// <summary>
            /// Error level for error events
            /// </summary>
            Error
        }

        /// <summary>
        /// Interface for log targets (UI controls that display logs)
        /// </summary>
        public interface ILogTarget
        {
            /// <summary>
            /// Updates the log display with the provided entries
            /// </summary>
            /// <param name="entries">The log entries to display</param>
            void UpdateLog(IEnumerable<LogEntry> entries);

            /// <summary>
            /// Gets a value indicating whether this log target has been disposed
            /// </summary>
            bool IsDisposed { get; }
        }

        /// <summary>
        /// TextBox log target implementation
        /// </summary>
        public class TextBoxLogTarget : ILogTarget
        {
            private readonly TextBox _textBox;
            private readonly Form _parentForm;

            /// <summary>
            /// Gets a value indicating whether this log target has been disposed
            /// </summary>
            public bool IsDisposed => _textBox.IsDisposed || _parentForm.IsDisposed;

            /// <summary>
            /// Initializes a new instance of the TextBoxLogTarget class
            /// </summary>
            /// <param name="textBox">The TextBox control to display logs in</param>
            /// <param name="parentForm">The parent form containing the TextBox</param>
            public TextBoxLogTarget(TextBox textBox, Form parentForm)
            {
                _textBox = textBox ?? throw new ArgumentNullException(nameof(textBox));
                _parentForm = parentForm ?? throw new ArgumentNullException(nameof(parentForm));
            }

            /// <summary>
            /// Updates the TextBox with the provided log entries
            /// </summary>
            /// <param name="entries">The log entries to display</param>
            public void UpdateLog(IEnumerable<LogEntry> entries)
            {
                if (IsDisposed) return;

                try
                {
                    if (_parentForm.InvokeRequired)
                    {
                        // Use BeginInvoke for non-blocking async UI update
                        _parentForm.BeginInvoke(() => UpdateLog(entries));
                        return;
                    }

                    // Only display the last MaxDisplayEntries entries to prevent flickering
                    var displayEntries = entries.TakeLast(MaxDisplayEntries);

                    // Build the complete log text
                    var logText = new StringBuilder();
                    foreach (var entry in displayEntries)
                    {
                        logText.AppendLine(entry.FormatForDisplay());
                    }

                    // Update the textbox
                    _textBox.Text = logText.ToString();

                    // Scroll to bottom
                    _textBox.SelectionStart = _textBox.Text.Length;
                    _textBox.ScrollToCaret();
                }
                catch (Exception ex)
                {
                    // Avoid infinite recursion if logging fails
                    System.Diagnostics.Debug.WriteLine($"Error updating log target: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// RichTextBox log target implementation with color support
        /// </summary>
        public class RichTextBoxLogTarget : ILogTarget
        {
            private readonly RichTextBox _richTextBox;
            private readonly Form _parentForm;

            /// <summary>
            /// Gets a value indicating whether this log target has been disposed
            /// </summary>
            public bool IsDisposed => _richTextBox.IsDisposed || _parentForm.IsDisposed;

            /// <summary>
            /// Initializes a new instance of the RichTextBoxLogTarget class
            /// </summary>
            /// <param name="richTextBox">The RichTextBox control to display logs in</param>
            /// <param name="parentForm">The parent form containing the RichTextBox</param>
            public RichTextBoxLogTarget(RichTextBox richTextBox, Form parentForm)
            {
                _richTextBox = richTextBox ?? throw new ArgumentNullException(nameof(richTextBox));
                _parentForm = parentForm ?? throw new ArgumentNullException(nameof(parentForm));
            }

            /// <summary>
            /// Updates the RichTextBox with the provided log entries with color coding
            /// </summary>
            /// <param name="entries">The log entries to display</param>
            public void UpdateLog(IEnumerable<LogEntry> entries)
            {
                if (IsDisposed) return;

                try
                {
                    if (_parentForm.InvokeRequired)
                    {
                        // Use BeginInvoke for non-blocking async UI update
                        _parentForm.BeginInvoke(() => UpdateLog(entries));
                        return;
                    }

                    // Only display the last MaxDisplayEntries entries to prevent flickering
                    var displayEntries = entries.TakeLast(MaxDisplayEntries).ToList();

                    // Clear and rebuild the rich text with colors
                    _richTextBox.Clear();

                    foreach (var entry in displayEntries)
                    {
                        // Set color based on log level
                        var color = GetColorForLogLevel(entry.Level);

                        // Add the text with color
                        var text = entry.FormatForDisplay() + Environment.NewLine;
                        AppendColoredText(text, color);
                    }

                    // Scroll to bottom
                    _richTextBox.SelectionStart = _richTextBox.Text.Length;
                    _richTextBox.ScrollToCaret();
                }
                catch (Exception ex)
                {
                    // Avoid infinite recursion if logging fails
                    System.Diagnostics.Debug.WriteLine($"Error updating rich text log target: {ex.Message}");
                }
            }

            /// <summary>
            /// Appends colored text to the RichTextBox
            /// </summary>
            private void AppendColoredText(string text, Color color)
            {
                _richTextBox.SelectionStart = _richTextBox.TextLength;
                _richTextBox.SelectionLength = 0;
                _richTextBox.SelectionColor = color;
                _richTextBox.AppendText(text);
                _richTextBox.SelectionColor = _richTextBox.ForeColor; // Reset to default
            }

            /// <summary>
            /// Gets the color for a specific log level
            /// </summary>
            private static Color GetColorForLogLevel(LogLevel level)
            {
                return level switch
                {
                    LogLevel.Success => Color.Green,
                    LogLevel.Error => Color.Red,
                    LogLevel.Warning => Color.Orange,
                    LogLevel.Info => Color.Black,
                    LogLevel.Debug => Color.Gray,
                    _ => Color.Black
                };
            }
        }

        /// <summary>
        /// Flicker-free log target implementation optimized for append-only updates
        /// </summary>
        public class FlickerFreeLogTarget : ILogTarget
        {
            private readonly Components.FlickerFreeLogViewer _logViewer;
            private readonly Form _parentForm;

            /// <summary>
            /// Gets a value indicating whether this log target has been disposed
            /// </summary>
            public bool IsDisposed => _logViewer.IsDisposed || _parentForm.IsDisposed;

            /// <summary>
            /// Initializes a new instance of the FlickerFreeLogTarget class
            /// </summary>
            /// <param name="logViewer">The FlickerFreeLogViewer control to display logs in</param>
            /// <param name="parentForm">The parent form containing the log viewer</param>
            public FlickerFreeLogTarget(Components.FlickerFreeLogViewer logViewer, Form parentForm)
            {
                _logViewer = logViewer ?? throw new ArgumentNullException(nameof(logViewer));
                _parentForm = parentForm ?? throw new ArgumentNullException(nameof(parentForm));
            }

            /// <summary>
            /// Updates the log viewer with the provided log entries using efficient append-only updates
            /// </summary>
            /// <param name="entries">The log entries to display</param>
            public void UpdateLog(IEnumerable<LogEntry> entries)
            {
                if (IsDisposed) return;

                try
                {
                    _logViewer.UpdateLogEntries(
                        entries,
                        entry => GetColorForLogLevel(entry.Level),
                        entry => entry.FormatForDisplay()
                    );
                }
                catch (Exception ex)
                {
                    // Avoid infinite recursion if logging fails
                    System.Diagnostics.Debug.WriteLine($"Error updating flicker-free log target: {ex.Message}");
                }
            }

            /// <summary>
            /// Gets the color for a specific log level
            /// </summary>
            private static Color GetColorForLogLevel(LogLevel level)
            {
                return level switch
                {
                    LogLevel.Success => Color.Green,
                    LogLevel.Error => Color.Red,
                    LogLevel.Warning => Color.Orange,
                    LogLevel.Info => Color.Black,
                    LogLevel.Debug => Color.Gray,
                    _ => Color.Black
                };
            }
        }

        /// <summary>
        /// Add a log target to receive log updates
        /// </summary>
        public static void AddTarget(ILogTarget target)
        {
            if (target == null) return;
            _targets.Add(target);
            target.UpdateLog(_logEntries);
        }

        /// <summary>
        /// Remove a log target
        /// </summary>
        public static void RemoveTarget(ILogTarget target)
        {
            if (target == null) return;
            _targets.Remove(target);
        }

        /// <summary>
        /// Log a message - lock-free. Writes to console and file immediately,
        /// enqueues for UI dispatch via background timer.
        /// </summary>
        public static void Log(string message, LogLevel level = LogLevel.Info, string source = "System")
        {
            if (string.IsNullOrEmpty(message)) return;

            var entry = new LogEntry(message, level, source);

            var levelPrefix = level switch
            {
                LogLevel.Error => "[ERROR]",
                LogLevel.Warning => "[WARN]",
                LogLevel.Success => "[OK]",
                LogLevel.Debug => "[DEBUG]",
                _ => "[INFO]"
            };
            Console.WriteLine($"{entry.Timestamp:HH:mm:ss.fff} {levelPrefix} [{source}] {message}");

            // Enqueue - timer will write to file and update UI
            _pendingEntries.Enqueue(entry);

            LogEntryAdded?.Invoke(entry);
        }

        /// <summary>
        /// Log an informational message
        /// </summary>
        public static void LogInfo(string message, string source = "System")
        {
            Log(message, LogLevel.Info, source);
        }

        /// <summary>
        /// Log a success message
        /// </summary>
        public static void LogSuccess(string message, string source = "System")
        {
            Log(message, LogLevel.Success, source);
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        public static void LogWarning(string message, string source = "System")
        {
            Log(message, LogLevel.Warning, source);
        }

        /// <summary>
        /// Log an error message
        /// </summary>
        public static void LogError(string message, string source = "System")
        {
            Log(message, LogLevel.Error, source);
        }

        /// <summary>
        /// Log a debug message
        /// </summary>
        public static void LogDebug(string message, string source = "System")
        {
            Log(message, LogLevel.Debug, source);
        }

        /// <summary>
        /// Get all current log entries
        /// </summary>
        public static IReadOnlyList<LogEntry> GetAllEntries()
        {
            return _logEntries.ToList();
        }

        /// <summary>
        /// Clear all log entries
        /// </summary>
        public static void Clear()
        {
            _logEntries.Clear();
            foreach (var target in _targets)
            {
                try { target.UpdateLog(Array.Empty<LogEntry>()); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error updating log target: {ex.Message}"); }
            }
        }

        /// <summary>
        /// Get log entries filtered by level
        /// </summary>
        public static IReadOnlyList<LogEntry> GetEntriesByLevel(LogLevel level)
        {
            return _logEntries.Where(e => e.Level == level).ToList();
        }

        /// <summary>
        /// Get log entries filtered by source
        /// </summary>
        public static IReadOnlyList<LogEntry> GetEntriesBySource(string source)
        {
            return _logEntries.Where(e => string.Equals(e.Source, source, StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }
}