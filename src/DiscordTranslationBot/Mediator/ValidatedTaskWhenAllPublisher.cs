using DiscordTranslationBot.Extensions;

namespace DiscordTranslationBot.Mediator;

public sealed class ValidatedTaskWhenAllPublisher : INotificationPublisher
{
    private readonly TaskWhenAllPublisher _taskWhenAllPublisher = new();

    public ValueTask Publish<TNotification>(
        NotificationHandlers<TNotification> handlers,
        TNotification notification,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        return !notification.TryValidate(out var validationResults)
            ? throw new MessageValidationException(notification.GetType().Name, validationResults)
            : _taskWhenAllPublisher.Publish(handlers, notification, cancellationToken);
    }
}
