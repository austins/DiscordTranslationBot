namespace DiscordTranslationBot.Mediator;

/// <summary>
/// Interface for a background command service.
/// </summary>
public interface IBackgroundCommandService
{
    /// <summary>
    /// Runs a command in the background to be invoked by the Mediator.
    /// It may have an delay, otherwise its handler will be run immediately.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    void Schedule(IBackgroundCommand request, CancellationToken cancellationToken);
}
