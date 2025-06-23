using Discord;
using Discord.WebSocket;
using DiscordTranslationBot.Notifications.Events;

namespace DiscordTranslationBot.Notifications.Handlers;

/// <summary>
/// Handler for redirecting Discord log messages to the logger.
/// </summary>
internal sealed partial class RedirectLogMessageToLoggerHandler : INotificationHandler<LogNotification>
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
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public ValueTask Handle(LogNotification notification, CancellationToken cancellationToken)
    {
        // Map the Discord log message severities to the logger log levels accordingly.
        var logLevel = notification.LogMessage.Exception switch
        {
            GatewayReconnectException => LogLevel.Information, // Most likely Discord requested the client to reconnect.
            _ => notification.LogMessage.Severity switch
            {
                LogSeverity.Debug => LogLevel.Trace,
                LogSeverity.Verbose => LogLevel.Debug,
                LogSeverity.Info => LogLevel.Information,
                LogSeverity.Warning => LogLevel.Warning,
                LogSeverity.Error => LogLevel.Error,
                LogSeverity.Critical => LogLevel.Critical,
                _ => LogLevel.Trace
            }
        };

        LogDiscordMessage(
            logLevel,
            notification.LogMessage.Exception,
            notification.LogMessage.Source,
            notification.LogMessage.Message ?? notification.LogMessage.Exception?.Message ?? string.Empty);

        return ValueTask.CompletedTask;
    }

    [LoggerMessage(Message = "Discord: [{source}] {message}")]
    private partial void LogDiscordMessage(LogLevel level, Exception? ex, string source, string message);
}
