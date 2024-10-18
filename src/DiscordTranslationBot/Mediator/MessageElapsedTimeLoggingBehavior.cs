using DiscordTranslationBot.Commands.Logging;
using System.Diagnostics;

namespace DiscordTranslationBot.Mediator;

/// <summary>
/// Mediator behavior for logging elapsed time of messages.
/// </summary>
/// <typeparam name="TMessage">The message type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed partial class MessageElapsedTimeLoggingBehavior<TMessage, TResponse>
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
        // If this is the log command, skip logging elapsed time as it pollutes the logs
        if (message is RedirectLogMessageToLogger)
        {
            return await next(message, cancellationToken);
        }

        var messageName = message.GetType().Name;
        _log.MessageExecuting(messageName);

        var stopwatch = Stopwatch.StartNew();
        var result = await next(message, cancellationToken);
        stopwatch.Stop();

        _log.MessageExecuted(messageName, stopwatch.ElapsedMilliseconds);

        return result;
    }

    private sealed partial class Log(ILogger logger)
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Executing message '{messageName}'...")]
        public partial void MessageExecuting(string messageName);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Executed message '{messageName}'. Elapsed time: {elapsedMilliseconds}ms.")]
        public partial void MessageExecuted(string messageName, long elapsedMilliseconds);
    }
}
