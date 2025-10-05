using Microsoft.Extensions.Logging;
using System;

namespace HavenCNCServer
{
    /// <summary>
    /// Logger provider that creates loggers which output to a Windows Forms control
    /// </summary>
    public class WinFormsLoggerProvider : ILoggerProvider
    {
        private readonly MainForm _mainForm;

        /// <summary>
        /// Initializes a new instance of the WinFormsLoggerProvider class
        /// </summary>
        /// <param name="mainForm">The main form to log messages to</param>
        public WinFormsLoggerProvider(MainForm mainForm)
        {
            _mainForm = mainForm;
        }

        /// <summary>
        /// Creates a new logger instance for the specified category
        /// </summary>
        /// <param name="categoryName">The category name for the logger</param>
        /// <returns>A new logger instance</returns>
        public ILogger CreateLogger(string categoryName)
        {
            return new WinFormsLogger(_mainForm, categoryName);
        }

        /// <summary>
        /// Disposes of the logger provider resources
        /// </summary>
        public void Dispose()
        {
        }
    }

    /// <summary>
    /// Logger implementation that outputs log messages to a Windows Forms control
    /// </summary>
    public class WinFormsLogger : ILogger
    {
        private readonly MainForm _mainForm;
        private readonly string _categoryName;

        /// <summary>
        /// Initializes a new instance of the WinFormsLogger class
        /// </summary>
        /// <param name="mainForm">The main form to log messages to</param>
        /// <param name="categoryName">The category name for this logger</param>
        public WinFormsLogger(MainForm mainForm, string categoryName)
        {
            _mainForm = mainForm;
            _categoryName = categoryName;
        }

        /// <summary>
        /// Begins a logical operation scope
        /// </summary>
        /// <typeparam name="TState">The type of the state</typeparam>
        /// <param name="state">The identifier for the scope</param>
        /// <returns>An IDisposable that ends the logical operation scope on dispose</returns>
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        /// <summary>
        /// Checks if the given log level is enabled
        /// </summary>
        /// <param name="logLevel">The log level to check</param>
        /// <returns>True if the log level is enabled, false otherwise</returns>
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        /// <summary>
        /// Writes a log entry
        /// </summary>
        /// <typeparam name="TState">The type of the object to be written</typeparam>
        /// <param name="logLevel">Entry will be written on this level</param>
        /// <param name="eventId">Id of the event</param>
        /// <param name="state">The entry to be written. Can be also an object</param>
        /// <param name="exception">The exception related to this entry</param>
        /// <param name="formatter">Function to create a string message of the state and exception</param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);
            var logEntry = $"[{logLevel}] {_categoryName}: {message}";
            
            if (exception != null)
            {
                logEntry += $" | Exception: {exception.Message}";
            }

            _mainForm.LogMessage(logEntry);
        }
    }
}
