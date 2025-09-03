using Microsoft.Extensions.Logging;
using System;

namespace HavenCNCServer
{
    public class WinFormsLoggerProvider : ILoggerProvider
    {
        private readonly MainForm _mainForm;

        public WinFormsLoggerProvider(MainForm mainForm)
        {
            _mainForm = mainForm;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new WinFormsLogger(_mainForm, categoryName);
        }

        public void Dispose()
        {
        }
    }

    public class WinFormsLogger : ILogger
    {
        private readonly MainForm _mainForm;
        private readonly string _categoryName;

        public WinFormsLogger(MainForm mainForm, string categoryName)
        {
            _mainForm = mainForm;
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state) => null!;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

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
