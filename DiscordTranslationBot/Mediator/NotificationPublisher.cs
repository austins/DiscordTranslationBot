namespace DiscordTranslationBot.Mediator;

/// <summary>
/// Mediator notification publisher that runs notification handlers concurrently and outputs logs on failure.
/// </summary>
public sealed partial class NotificationPublisher : INotificationPublisher
{
    private readonly ILogger<NotificationPublisher> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationPublisher" /> class.
    /// </summary>
    /// <param name="logger">Logger to use.</param>
    public NotificationPublisher(ILogger<NotificationPublisher> logger)
    {
        _logger = logger;
    }

    public Task Publish(
        IEnumerable<NotificationHandlerExecutor> handlerExecutors,
        INotification notification,
        CancellationToken cancellationToken)
    {
        var tasks = handlerExecutors.Select(
            async handler =>
            {
                try
                {
                    await handler.HandlerCallback(notification, cancellationToken);
                }
                catch (Exception ex)
                {
                    LogFailureInNotificationHandler(
                        ex,
                        handler.HandlerInstance.GetType().Name,
                        notification.GetType().Name);
                }
            });

        return Task.WhenAll(tasks);
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "An exception has occurred in handler '{handlerName}' for notification '{notificationName}'.")]
    private partial void LogFailureInNotificationHandler(Exception ex, string handlerName, string notificationName);
}
