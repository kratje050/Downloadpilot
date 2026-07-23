using DownloadPilot.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using System.IO;

namespace DownloadPilot.App.Services;

public sealed class LocalFileLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new LocalFileLogger(categoryName);

    public void Dispose()
    {
    }

    private sealed class LocalFileLogger(string categoryName) : ILogger
    {
        private readonly string _categoryName = categoryName;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var line = $"{DateTime.Now:O} [{logLevel}] {_categoryName}: {formatter(state, exception)}";
            if (exception is not null)
            {
                line += Environment.NewLine + exception;
            }

            File.AppendAllText(SqlitePaths.LogPath, line + Environment.NewLine);
        }
    }
}
