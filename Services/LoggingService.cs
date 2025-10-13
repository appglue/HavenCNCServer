using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace HavenCNCServer.Services
{
    /// <summary>
    /// Centralized logging service for the application
    /// Manages log messages with automatic line limiting and UI updates
    /// </summary>
    public static class LoggingService
    {
        private static readonly object _lock = new object();
        private static readonly List<LogEntry> _logEntries = new List<LogEntry>();
        private static readonly HashSet<ILogTarget> _targets = new HashSet<ILogTarget>();
        
        /// <summary>
        /// Maximum number of log entries to keep in memory
        /// </summary>
        public static int MaxLogEntries { get; set; } = 2000;
        
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
                        _parentForm.Invoke(() => UpdateLog(entries));
                        return;
                    }
                    
                    // Build the complete log text
                    var logText = new StringBuilder();
                    foreach (var entry in entries)
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
                        _parentForm.Invoke(() => UpdateLog(entries));
                        return;
                    }
                    
                    // Clear and rebuild the rich text with colors
                    _richTextBox.Clear();
                    
                    foreach (var entry in entries)
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
        /// Add a log target to receive log updates
        /// </summary>
        public static void AddTarget(ILogTarget target)
        {
            if (target == null) return;
            
            lock (_lock)
            {
                _targets.Add(target);
                
                // Send current log entries to the new target
                target.UpdateLog(_logEntries);
            }
        }
        
        /// <summary>
        /// Remove a log target
        /// </summary>
        public static void RemoveTarget(ILogTarget target)
        {
            if (target == null) return;
            
            lock (_lock)
            {
                _targets.Remove(target);
            }
        }
        
        /// <summary>
        /// Log a message with specified level and source
        /// </summary>
        public static void Log(string message, LogLevel level = LogLevel.Info, string source = "System")
        {
            if (string.IsNullOrEmpty(message)) return;
            
            var entry = new LogEntry(message, level, source);
            
            lock (_lock)
            {
                // Add the new entry
                _logEntries.Add(entry);
                
                // Trim old entries if we exceed the limit
                while (_logEntries.Count > MaxLogEntries)
                {
                    _logEntries.RemoveAt(0);
                }
                
                // Clean up disposed targets
                var disposedTargets = _targets.Where(t => t.IsDisposed).ToList();
                foreach (var disposedTarget in disposedTargets)
                {
                    _targets.Remove(disposedTarget);
                }
                
                // Update all targets
                foreach (var target in _targets)
                {
                    try
                    {
                        target.UpdateLog(_logEntries);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error updating log target: {ex.Message}");
                    }
                }
            }
            
            // Fire the event
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
            lock (_lock)
            {
                return _logEntries.ToList();
            }
        }
        
        /// <summary>
        /// Clear all log entries
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _logEntries.Clear();
                
                // Update all targets
                foreach (var target in _targets)
                {
                    try
                    {
                        target.UpdateLog(_logEntries);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error updating log target: {ex.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Get log entries filtered by level
        /// </summary>
        public static IReadOnlyList<LogEntry> GetEntriesByLevel(LogLevel level)
        {
            lock (_lock)
            {
                return _logEntries.Where(e => e.Level == level).ToList();
            }
        }
        
        /// <summary>
        /// Get log entries filtered by source
        /// </summary>
        public static IReadOnlyList<LogEntry> GetEntriesBySource(string source)
        {
            lock (_lock)
            {
                return _logEntries.Where(e => string.Equals(e.Source, source, StringComparison.OrdinalIgnoreCase)).ToList();
            }
        }
    }
}