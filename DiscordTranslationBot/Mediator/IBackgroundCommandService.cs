namespace DiscordTranslationBot.Mediator;

public interface IBackgroundCommandService
{
    void Invoke(IRequest request, CancellationToken cancellationToken);

    void Schedule(IRequest request, TimeSpan delay, CancellationToken cancellationToken);
}
