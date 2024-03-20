namespace DiscordTranslationBot.Mediator;

/// <summary>
/// Mediator notification publisher that runs notification handlers concurrently and outputs logs on failure.
/// </summary>
public sealed partial class NotificationPublisher : INotificationPublisher
{
    private readonly Log _log;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationPublisher" /> class.
    /// </summary>
    /// <param name="logger">Logger to use.</param>
    public NotificationPublisher(ILogger<NotificationPublisher> logger)
    {
        _log = new Log(logger);
    }

    public Task Publish(
        IEnumerable<NotificationHandlerExecutor> handlerExecutors,
        INotification notification,
        CancellationToken cancellationToken)
    {
        var notificationName = notification.GetType().Name;

        var tasks = handlerExecutors.Select(
            async handler =>
            {
                try
                {
                    _log.PublishingNotification(notificationName);
                    await handler.HandlerCallback(notification, cancellationToken);
                }
                catch (Exception ex)
                {
                    _log.FailureInNotificationHandler(ex, handler.HandlerInstance.GetType().Name, notificationName);
                }
            });

        return Task.WhenAll(tasks);
    }

    private sealed partial class Log
    {
        private readonly ILogger _logger;

        public Log(ILogger logger)
        {
            _logger = logger;
        }

        [LoggerMessage(Level = LogLevel.Information, Message = "Publishing notification '{eventName}'...")]
        public partial void PublishingNotification(string eventName);

        [LoggerMessage(
            Level = LogLevel.Error,
            Message = "An exception has occurred in handler '{handlerName}' for notification '{notificationName}'.")]
        public partial void FailureInNotificationHandler(Exception ex, string handlerName, string notificationName);
    }
}
