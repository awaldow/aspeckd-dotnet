using Microsoft.Extensions.Logging;

namespace Aspeckd.Tests.Helpers;

/// <summary>
/// A minimal <see cref="ILoggerProvider"/> that accumulates formatted log messages into a
/// caller-supplied list.  Used in description-warning tests to verify that Aspeckd emits
/// the expected diagnostic messages via <c>ILogger</c>.
/// </summary>
public sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly IList<(LogLevel Level, string Message)> _sink;

    public CapturingLoggerProvider(IList<(LogLevel Level, string Message)> sink)
    {
        _sink = sink;
    }

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(_sink);
    public void Dispose() { }

    private sealed class CapturingLogger : ILogger
    {
        private readonly IList<(LogLevel Level, string Message)> _sink;

        public CapturingLogger(IList<(LogLevel Level, string Message)> sink) => _sink = sink;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _sink.Add((logLevel, formatter(state, exception)));
        }
    }
}
