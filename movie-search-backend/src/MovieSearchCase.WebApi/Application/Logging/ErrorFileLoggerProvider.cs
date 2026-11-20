using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace MovieSearchCase.WebApi.Application.Logging;

/// <summary>Queues error and critical events for background JSONL file writing.</summary>
public sealed class ErrorFileLoggerProvider(ErrorLogQueue queue) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new ErrorFileLogger(categoryName, queue);

    public void Dispose()
    {
    }

    private sealed class ErrorFileLogger(string categoryName, ErrorLogQueue queue) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var entry = new ErrorLogEntry(
                DateTimeOffset.UtcNow,
                logLevel.ToString(),
                categoryName,
                eventId.Id,
                formatter(state, exception),
                exception?.GetType().FullName,
                exception?.StackTrace);

            queue.TryEnqueue(JsonSerializer.Serialize(entry));
        }
    }

    private sealed record ErrorLogEntry(
        DateTimeOffset TimestampUtc,
        string Level,
        string Category,
        int EventId,
        string Message,
        string? ExceptionType,
        string? StackTrace);
}
