namespace DiscordTranslationBot.Mediator;

public sealed class ValidationBehavior<TRequest, TResponse>
    : ValidateMediatorCallsBase,
        IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public ValidationBehavior(IServiceProvider serviceProvider)
        : base(serviceProvider) { }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        await ValidateOrThrowAsync(request, cancellationToken);

        return await next();
    }
}
