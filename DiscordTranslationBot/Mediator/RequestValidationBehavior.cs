using DiscordTranslationBot.Extensions;

namespace DiscordTranslationBot.Mediator;

/// <summary>
/// Mediator validation behavior for requests.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class RequestValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>
    /// Validates an incoming request if it has a validator before executing its handler.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="next">
    /// Awaitable delegate for the next action in the pipeline. Eventually this delegate represents the
    /// handler.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response.</returns>
    /// <exception cref="RequestValidationException">If there are any validation errors.</exception>
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!request.TryValidateObject(out var validationResults))
        {
            throw new RequestValidationException(request.GetType().Name, validationResults);
        }

        return next();
    }
}