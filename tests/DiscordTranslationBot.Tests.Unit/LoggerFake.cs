namespace DiscordTranslationBot.Tests.Unit;

internal class LoggerFake : ILogger
{
    private readonly IList<LogEntry> _entries = [];
    private readonly bool _logTrace;

    protected LoggerFake(bool logTrace = false)
    {
        _logTrace = logTrace;
    }

    public IReadOnlyList<LogEntry> Entries => _entries.AsReadOnly();

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

        _entries.Add(
            new LogEntry
            {
                LogLevel = logLevel,
                EventId = eventId,
                State = state,
                Exception = exception,
                Message = formatter(state, exception)
            });
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.Trace || _logTrace;
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }
}

internal sealed class LoggerFake<TCategoryName>(bool logTrace = false) : LoggerFake(logTrace),
    ILogger<TCategoryName>;

internal sealed class LogEntry
{
    public required LogLevel LogLevel { get; init; }

    public required EventId EventId { get; init; }

    public required object? State { get; init; }

    public required Exception? Exception { get; init; }

    public required string Message { get; init; }
}
