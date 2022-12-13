using Discord;
using DiscordTranslationBot.Notifications;
using Mediator;
using Serilog;
using Serilog.Events;
using ILogger = Serilog.ILogger;

namespace DiscordTranslationBot.Handlers;

/// <summary>
/// Handles the Log event of the Discord client.
/// </summary>
public sealed class LogHandler : INotificationHandler<LogNotification>
{
    private static readonly ILogger Logger = Log.ForContext<LogHandler>();

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

        Logger.Write(
            logLevel,
            notification.LogMessage.Exception,
            "Discord: {LogMessage}",
            notification.LogMessage
        );

        return ValueTask.CompletedTask;
    }
}
