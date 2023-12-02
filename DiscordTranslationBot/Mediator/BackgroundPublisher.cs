using AsyncAwaitBestPractices;

namespace DiscordTranslationBot.Mediator;

/// <summary>
/// Mediator notification publisher that publishes notifications in the background.
/// </summary>
internal sealed partial class BackgroundPublisher : INotificationPublisher
{
    private readonly Log _log;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundPublisher" /> class.
    /// </summary>
    /// <param name="logger">Logger to use.</param>
    internal BackgroundPublisher(ILogger<BackgroundPublisher> logger)
    {
        _log = new Log(logger);
    }

    /// <summary>
    /// Publishes a notification to any notification handlers in the background.
    /// </summary>
    /// <param name="handlerExecutors">The notification handlers.</param>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public Task Publish(
        IEnumerable<NotificationHandlerExecutor> handlerExecutors,
        INotification notification,
        CancellationToken cancellationToken)
    {
        foreach (var handler in handlerExecutors)
        {
            handler.HandlerCallback(notification, cancellationToken)
                .SafeFireAndForget(ex => _log.FailureInNotificationHandler(ex));
        }

        return Task.CompletedTask;
    }

    private sealed partial class Log
    {
        private readonly ILogger<BackgroundPublisher> _logger;

        public Log(ILogger<BackgroundPublisher> logger)
        {
            _logger = logger;
        }

        [LoggerMessage(Level = LogLevel.Error, Message = "An exception has occurred in a notification handler.")]
        public partial void FailureInNotificationHandler(Exception ex);
    }
}
