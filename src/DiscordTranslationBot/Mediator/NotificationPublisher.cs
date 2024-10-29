using DiscordTranslationBot.Extensions;
using System.Diagnostics;

namespace DiscordTranslationBot.Mediator;

public sealed partial class NotificationPublisher : INotificationPublisher
{
    private readonly Log _log;
    private readonly TaskWhenAllPublisher _taskWhenAllPublisher = new();

    public NotificationPublisher(ILogger<NotificationPublisher> logger)
    {
        _log = new Log(logger);
    }

    public async ValueTask Publish<TNotification>(
        NotificationHandlers<TNotification> handlers,
        TNotification notification,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        if (!notification.TryValidate(out var validationResults))
        {
            throw new MessageValidationException(notification.GetType().Name, validationResults);
        }

        var notificationName = notification.GetType().Name;
        _log.PublishingNotification(notificationName);

        var startingTimestamp = Stopwatch.GetTimestamp();
        await _taskWhenAllPublisher.Publish(handlers, notification, cancellationToken);
        var elapsed = Stopwatch.GetElapsedTime(startingTimestamp);

        _log.NotificationHandlersExecuted(notificationName, elapsed.TotalMilliseconds);
    }

    private sealed partial class Log(ILogger logger)
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Publishing notification '{notificationName}'...")]
        public partial void PublishingNotification(string notificationName);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Executed notification handler(s) for '{notificationName}'. Elapsed time: {elapsedMs}ms.")]
        public partial void NotificationHandlersExecuted(string notificationName, double elapsedMs);
    }
}
