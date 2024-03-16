using Discord;

namespace DiscordTranslationBot.Commands.Logging;

public sealed class RedirectLogMessageToLogger : IRequest
{
    /// <summary>
    /// The Discord log message.
    /// </summary>
    public required LogMessage LogMessage { get; init; }
}

/// <summary>
/// Handler for redirecting Discord log messages to the logger.
/// </summary>
public sealed partial class RedirectLogMessageToLoggerHandler : IRequestHandler<RedirectLogMessageToLogger>
{
    private readonly ILogger<RedirectLogMessageToLoggerHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedirectLogMessageToLoggerHandler" /> class.
    /// </summary>
    /// <param name="logger">Logger to use.</param>
    public RedirectLogMessageToLoggerHandler(ILogger<RedirectLogMessageToLoggerHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Redirect Discord log messages to the app's logger.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task Handle(RedirectLogMessageToLogger request, CancellationToken cancellationToken)
    {
        // Map the Discord log message severities to the logger log levels accordingly.
        var logLevel = request.LogMessage.Severity switch
        {
            LogSeverity.Debug => LogLevel.Trace,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Critical => LogLevel.Critical,
            _ => LogLevel.Trace
        };

        LogDiscordMessage(
            logLevel,
            request.LogMessage.Exception,
            request.LogMessage.Source,
            request.LogMessage.Message);

        return Task.CompletedTask;
    }

    [LoggerMessage(Message = "Discord {source}: {message}")]
    private partial void LogDiscordMessage(LogLevel level, Exception ex, string source, string message);
}
