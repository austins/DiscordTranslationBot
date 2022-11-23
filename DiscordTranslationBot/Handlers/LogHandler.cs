using Discord;
using DiscordTranslationBot.Notifications;
using Mediator;
using Serilog.Events;
using ILogger = Serilog.ILogger;

namespace DiscordTranslationBot.Handlers;

/// <summary>
/// Handles the Log event of the Discord client.
/// </summary>
public sealed class LogHandler : INotificationHandler<LogNotification>
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogHandler"/> class.
    /// </summary>
    /// <param name="logger">Logger to use.</param>
    public LogHandler(ILogger logger)
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
            LogSeverity.Debug => LogEventLevel.Debug,
            LogSeverity.Verbose => LogEventLevel.Verbose,
            LogSeverity.Info => LogEventLevel.Information,
            LogSeverity.Warning => LogEventLevel.Warning,
            LogSeverity.Error => LogEventLevel.Error,
            LogSeverity.Critical => LogEventLevel.Fatal,
            _ => LogEventLevel.Debug
        };

        _logger.Write(
            logLevel,
            notification.LogMessage.Exception,
            "Discord: {LogMessage}",
            notification.LogMessage
        );

        return ValueTask.CompletedTask;
    }
}
