using DiscordTranslationBot.Notifications.Events;
using System.Diagnostics;

namespace DiscordTranslationBot.Mediator;

/// <summary>
/// Mediator behavior for logging elapsed time of messages.
/// </summary>
/// <typeparam name="TMessage">The message type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
internal sealed partial class MessageElapsedTimeLoggingBehavior<TMessage, TResponse>
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private readonly Log _log;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageElapsedTimeLoggingBehavior{TMessage,TResponse}" /> class.
    /// </summary>
    /// <param name="logger">Logger to use.</param>
    public MessageElapsedTimeLoggingBehavior(ILogger<MessageElapsedTimeLoggingBehavior<TMessage, TResponse>> logger)
    {
        _log = new Log(logger);
    }

    /// <summary>
    /// Executes messages and logs elapsed time of messages.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="next">
    /// Awaitable delegate for the next action in the pipeline. Eventually this delegate represents the
    /// handler.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response.</returns>
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        // Log notifications can be raised per received message, so skip the logging and elapsed-time
        // tracking to avoid doubling the log volume and overhead of the real notification.
        if (message is LogNotification)
        {
            return await next(message, cancellationToken);
        }

        var startingTimestamp = Stopwatch.GetTimestamp();
        var result = await next(message, cancellationToken);
        var elapsed = Stopwatch.GetElapsedTime(startingTimestamp);

        _log.MessageExecuted(message.GetType().Name, elapsed.TotalMilliseconds);

        return result;
    }

    private sealed partial class Log(ILogger logger)
    {
        [LoggerMessage(
            Level = LogLevel.Debug,
            Message = "Executed message '{messageName}'. Elapsed time: {elapsedMs}ms.")]
        public partial void MessageExecuted(string messageName, double elapsedMs);
    }
}
