using AsyncAwaitBestPractices;

namespace DiscordTranslationBot.Mediator;

public static class MediatorExtensions
{
    public static Task SendInBackgroundAsync(
        this IMediator mediator,
        IRequest request,
        Action<Exception>? onException,
        CancellationToken cancellationToken,
        TimeSpan? delay = null
    )
    {
        SendAsync().SafeFireAndForget(onException);
        return Task.CompletedTask;

        async Task SendAsync()
        {
            if (delay != null)
            {
                await Task.Delay(delay.Value, cancellationToken);
            }

            await mediator.Send(request, cancellationToken);
        }
    }
}
