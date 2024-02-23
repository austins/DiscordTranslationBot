using System.Diagnostics;
using DiscordTranslationBot.Notifications;

namespace DiscordTranslationBot.Mediator;

/// <summary>
/// Mediator notification publisher that runs notification handlers concurrently and outputs logs of performance.
/// </summary>
internal sealed partial class NotificationPublisher : INotificationPublisher
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

    /// <summary>
    /// Publishes a notification to any notification handlers in the background.
    /// </summary>
    /// <param name="handlerExecutors">The notification handlers.</param>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task Publish(
        IEnumerable<NotificationHandlerExecutor> handlerExecutors,
        INotification notification,
        CancellationToken cancellationToken)
    {
        var tasks = handlerExecutors.Select(
            async handler =>
            {
                if (notification is LogNotification)
                {
                    await handler.HandlerCallback(notification, cancellationToken);
                }
                else
                {
                    var handlerName = handler.HandlerInstance.GetType().Name;
                    var notificationName = notification.GetType().Name;
                    _log.NotificationHandlerExecuting(handlerName, notificationName);

                    var stopwatch = Stopwatch.StartNew();
                    await handler.HandlerCallback(notification, cancellationToken);
                    stopwatch.Stop();

                    _log.NotificationHandlerExecuted(handlerName, notificationName, stopwatch.ElapsedMilliseconds);
                }
            });

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
                _log.FailureInNotificationHandler(innerException);
            }
        }
    }

    private sealed partial class Log
    {
        private readonly ILogger<NotificationPublisher> _logger;

        public Log(ILogger<NotificationPublisher> logger)
        {
            _logger = logger;
        }

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Executing notification handler '{handlerName}' for '{notificationName}'...")]
        public partial void NotificationHandlerExecuting(string handlerName, string notificationName);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message =
                "Executed notification handler '{handlerName}' for '{notificationName}'. Elapsed time: {elapsedMilliseconds}ms.")]
        public partial void NotificationHandlerExecuted(
            string handlerName,
            string notificationName,
            long elapsedMilliseconds);

        [LoggerMessage(Level = LogLevel.Error, Message = "An exception has occurred in a notification handler.")]
        public partial void FailureInNotificationHandler(Exception ex);
    }
}
