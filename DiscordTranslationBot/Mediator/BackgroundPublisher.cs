using AsyncAwaitBestPractices;

namespace DiscordTranslationBot.Mediator;

public sealed partial class BackgroundPublisher : INotificationPublisher
{
    private readonly Log _log;

    public BackgroundPublisher(ILogger<BackgroundPublisher> logger)
    {
        _log = new Log(logger);
    }

    public Task Publish(
        IEnumerable<NotificationHandlerExecutor> handlerExecutors,
        INotification notification,
        CancellationToken cancellationToken
    )
    {
        foreach (var handler in handlerExecutors)
        {
            handler
                .HandlerCallback(notification, cancellationToken)
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
