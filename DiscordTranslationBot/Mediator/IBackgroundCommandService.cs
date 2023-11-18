namespace DiscordTranslationBot.Mediator;

/// <summary>
/// Interface for a background command service.
/// </summary>
public interface IBackgroundCommandService
{
    /// <summary>
    /// Sends a command in the background to be invoked immediately by Mediator.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    void Invoke(IRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Schedules a command in the background to be invoked after a delay by the Mediator.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="delay">The delay before invoking the command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    void Schedule(IRequest request, TimeSpan delay, CancellationToken cancellationToken);
}
