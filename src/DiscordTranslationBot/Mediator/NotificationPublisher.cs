using System.Diagnostics;

namespace DiscordTranslationBot.Mediator;

public sealed partial class NotificationPublisher : INotificationPublisher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Log _log;
    private readonly TaskWhenAllPublisher _taskWhenAllPublisher = new();

    public NotificationPublisher(IServiceProvider serviceProvider, ILogger<NotificationPublisher> logger)
    {
        _serviceProvider = serviceProvider;
        _log = new Log(logger);
    }

    public async ValueTask Publish<TNotification>(
        NotificationHandlers<TNotification> handlers,
        TNotification notification,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        var validator = _serviceProvider.GetService<IValidator<TNotification>>();
        if (validator is not null)
        {
            await validator.ValidateAndThrowAsync(notification, cancellationToken);
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
