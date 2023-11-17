using AsyncAwaitBestPractices;

namespace DiscordTranslationBot.Mediator;

public sealed partial class BackgroundPublisher : ValidateMediatorCallsBase, INotificationPublisher
{
    private readonly Log _log;

    public BackgroundPublisher(IServiceProvider serviceProvider, ILogger<BackgroundPublisher> logger)
        : base(serviceProvider)
    {
        _log = new Log(logger);
    }

    public async Task Publish(
        IEnumerable<NotificationHandlerExecutor> handlerExecutors,
        INotification notification,
        CancellationToken cancellationToken
    )
    {
        await ValidateOrThrowAsync(notification, cancellationToken);

        foreach (var handler in handlerExecutors)
        {
            handler
                .HandlerCallback(notification, cancellationToken)
                .SafeFireAndForget(ex => _log.FailureInNotificationHandler(ex));
        }
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
