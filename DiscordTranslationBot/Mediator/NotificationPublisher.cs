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

    public async Task Publish(
        IEnumerable<NotificationHandlerExecutor> handlerExecutors,
        INotification notification,
        CancellationToken cancellationToken)
    {
        var tasks = handlerExecutors.Select(handler => handler.HandlerCallback(notification, cancellationToken));

        var whenAllTask = Task.WhenAll(tasks);
        try
        {
            await whenAllTask;
        }
        catch (Exception)
        {
            if (whenAllTask.Exception is null)
            {
                throw;
            }

            foreach (var innerException in whenAllTask.Exception.InnerExceptions)
            {
                LogFailureInNotificationHandler(innerException, notification.GetType().Name);
            }
        }
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "An exception has occurred in notification handler '{handlerName}'.")]
    private partial void LogFailureInNotificationHandler(Exception ex, string handlerName);
}
