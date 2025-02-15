namespace DiscordTranslationBot.Mediator;

/// <summary>
/// Mediator validation behavior for messages.
/// </summary>
/// <typeparam name="TMessage">The message type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class MessageValidationBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private readonly IValidator<TMessage>? _validator;

    public MessageValidationBehavior(IServiceProvider serviceProvider)
    {
        _validator = serviceProvider.GetService<IValidator<TMessage>>();
    }

    /// <summary>
    /// Validates an incoming message if it has a validator before executing its handler.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="next">
    /// Awaitable delegate for the next action in the pipeline. Eventually this delegate represents the handler.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response.</returns>
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validator is not null)
        {
            await _validator.ValidateAndThrowAsync(message, cancellationToken);
        }

        return await next(message, cancellationToken);
    }
}
