using Discord;
using DiscordTranslationBot.Commands;

namespace DiscordTranslationBot.Handlers;

/// <summary>
/// Handles the Log event of the Discord client.
/// </summary>
public sealed partial class LogHandler : IRequestHandler<LogDiscordMessage>
{
    private readonly ILogger<LogHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogHandler" /> class.
    /// </summary>
    /// <param name="logger">Logger to use.</param>
    public LogHandler(ILogger<LogHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Logs Discord log messages to the app's logger.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task Handle(LogDiscordMessage request, CancellationToken cancellationToken)
    {
        // Map the Discord log message severities to the logger log levels accordingly.
        var logLevel = request.LogMessage.Severity switch
        {
            LogSeverity.Debug => LogLevel.Debug,
            LogSeverity.Verbose => LogLevel.Trace,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Critical => LogLevel.Critical,
            _ => LogLevel.Debug
        };

        LogDiscordMessage(
            logLevel,
            request.LogMessage.Exception,
            request.LogMessage.Source,
            request.LogMessage.Message
        );

        return Task.CompletedTask;
    }

    [LoggerMessage(Message = "Discord {source}: {message}")]
    private partial void LogDiscordMessage(LogLevel level, Exception ex, string source, string message);
}
