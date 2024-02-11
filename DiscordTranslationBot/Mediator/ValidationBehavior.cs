using FluentValidation;

namespace DiscordTranslationBot.Mediator;

/// <summary>
/// Mediator validation behavior for requests.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IValidator<TRequest>? _validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationBehavior{TRequest,TResponse}" /> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider to use.</param>
    public ValidationBehavior(IServiceProvider serviceProvider)
    {
        _validator = serviceProvider.GetService<IValidator<TRequest>>();
    }

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
    /// <exception cref="ValidationException">If there are any validation errors.</exception>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validator is not null)
        {
            await _validator.ValidateAndThrowAsync(request, cancellationToken);
        }

        return await next();
    }
}
