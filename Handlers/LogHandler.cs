using Discord;
using DiscordTranslationBot.Notifications;
using MediatR;

namespace DiscordTranslationBot.Handlers;

/// <summary>Handles the Log event of the Discord client.</summary>
internal sealed class LogHandler : INotificationHandler<LogNotification>
{
    private readonly ILogger<LogHandler> _logger;

    /// <summary>Initializes the handler.</summary>
    /// <param name="logger">Logger to use.</param>
    public LogHandler(ILogger<LogHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>Sends all Discord log messages to the logger.</summary>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task Handle(LogNotification notification, CancellationToken cancellationToken)
    {
        // Map the Discord log message severities to the logger log levels accordingly.
        var logLevel = notification.LogMessage.Severity switch
        {
            LogSeverity.Debug => LogLevel.Trace,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Critical => LogLevel.Critical,
            _ => LogLevel.Trace
        };

        _logger.Log(logLevel, notification.LogMessage.Exception, notification.LogMessage.ToString());

        return Task.CompletedTask;
    }
}