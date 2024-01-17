using Discord;
using DiscordTranslationBot.Notifications;
using FluentValidation;

namespace DiscordTranslationBot.Handlers;

/// <summary>
/// Handler for redirecting Discord log messages to the logger.
/// </summary>
public sealed partial class RedirectLogMessageToLoggerHandler : INotificationHandler<LogNotification>
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
    /// <param name="notification">The request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task Handle(LogNotification notification, CancellationToken cancellationToken)
    {
        // Map the Discord log message severities to the logger log levels accordingly.
        var logLevel = notification.LogMessage.Exception switch
        {
            ValidationException => LogLevel.Error,
            _ => notification.LogMessage.Severity switch
            {
                LogSeverity.Debug => LogLevel.Debug,
                LogSeverity.Verbose => LogLevel.Trace,
                LogSeverity.Info => LogLevel.Information,
                LogSeverity.Warning => LogLevel.Warning,
                LogSeverity.Error => LogLevel.Error,
                LogSeverity.Critical => LogLevel.Critical,
                _ => LogLevel.Debug
            }
        };

        LogDiscordMessage(
            logLevel,
            notification.LogMessage.Exception,
            notification.LogMessage.Source,
            notification.LogMessage.Message);

        return Task.CompletedTask;
    }

    [LoggerMessage(Message = "Discord {source}: {message}")]
    private partial void LogDiscordMessage(LogLevel level, Exception ex, string source, string message);
}
