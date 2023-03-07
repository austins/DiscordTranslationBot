using Discord;
using DiscordTranslationBot.Notifications;
using Mediator;

namespace DiscordTranslationBot.Handlers;

/// <summary>
/// Handles the Log event of the Discord client.
/// </summary>
public sealed partial class LogHandler : INotificationHandler<LogNotification>
{
    private readonly ILogger<LogHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogHandler"/> class.
    /// </summary>
    /// <param name="logger">Logger to use.</param>
    public LogHandler(ILogger<LogHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Sends all Discord log messages to the logger.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public ValueTask Handle(LogNotification notification, CancellationToken cancellationToken)
    {
        // Map the Discord log message severities to the logger log levels accordingly.
        var logLevel = notification.LogMessage.Severity switch
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
            notification.LogMessage.Exception,
            notification.LogMessage.Source,
            notification.LogMessage.Message
        );

        return ValueTask.CompletedTask;
    }

    [LoggerMessage(Message = "Discord {source}: {message}")]
    private partial void LogDiscordMessage(
        LogLevel level,
        Exception ex,
        string source,
        string message
    );
}
