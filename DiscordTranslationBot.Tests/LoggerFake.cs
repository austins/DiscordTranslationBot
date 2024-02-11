namespace DiscordTranslationBot.Tests;

internal class LoggerFake : ILogger
{
    private readonly string? _categoryName;
    private readonly IList<LogEntry> _entries = [];
    private readonly bool _logTrace;

    protected LoggerFake(bool logTrace = false, string? categoryName = null)
    {
        _logTrace = logTrace;
        _categoryName = categoryName;
    }

    public IReadOnlyCollection<LogEntry> Entries => _entries.AsReadOnly();

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel))
        {
            var entry = new LogEntry(logLevel, eventId, state, exception, formatter(state, exception));
            _entries.Add(entry);

            var output =
                $"Logger output at {DateTime.Now:HH:mm:ss}, {entry.LogLevel}, {_categoryName}[{entry.EventId}]:\n  Message: {entry.Message}";

            if (exception is not null)
            {
                output += $"\n  Exception: {exception}";
            }

            TestContext.Progress.WriteLine(output);
        }
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

internal sealed class LoggerFake<TCategoryName> : LoggerFake, ILogger<TCategoryName>
{
    public LoggerFake(bool logTrace = false)
        : base(logTrace, typeof(TCategoryName).FullName)
    {
    }
}

internal sealed record LogEntry(
    LogLevel LogLevel,
    EventId EventId,
    object? State,
    Exception? Exception,
    string Message);
