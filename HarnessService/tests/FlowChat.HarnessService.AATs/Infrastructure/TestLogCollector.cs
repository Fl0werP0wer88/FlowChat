using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace FlowChat.HarnessService.AATs.Infrastructure;

public sealed class TestLogCollector : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _entries = new();

    public ILogger CreateLogger(string categoryName) => new CollectorLogger(_entries);

    public bool Contains(string value) =>
        _entries.Any(entry => entry.Contains(value, StringComparison.OrdinalIgnoreCase));

    public void Dispose()
    {
    }

    private sealed class CollectorLogger(ConcurrentQueue<string> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            entries.Enqueue($"{formatter(state, exception)} {exception}");
    }
}
