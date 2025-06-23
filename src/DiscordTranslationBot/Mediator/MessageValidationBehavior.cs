using DiscordTranslationBot.Extensions;

namespace DiscordTranslationBot.Mediator;

/// <summary>
/// Mediator validation behavior for messages.
/// </summary>
/// <typeparam name="TMessage">The message type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
internal sealed class MessageValidationBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    /// <summary>
    /// Validates an incoming message if it has a validator before executing its handler.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="next">
    /// Awaitable delegate for the next action in the pipeline. Eventually this delegate represents the handler.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response.</returns>
    /// <exception cref="MessageValidationException">If there are any validation errors.</exception>
    public ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!message.TryValidate(out var validationResults))
        {
            throw new MessageValidationException(message.GetType().Name, validationResults);
        }

        return next(message, cancellationToken);
    }
}
