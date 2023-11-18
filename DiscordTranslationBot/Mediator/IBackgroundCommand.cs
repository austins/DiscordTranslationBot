namespace DiscordTranslationBot.Mediator;

/// <summary>
/// A request that can be executed in the background using <see cref="IBackgroundCommandService" />.
/// </summary>
public interface IBackgroundCommand : IRequest
{
    /// <summary>
    /// Optional delay before executing the command's handler in the background.
    /// </summary>
    TimeSpan? Delay { get; }
}
