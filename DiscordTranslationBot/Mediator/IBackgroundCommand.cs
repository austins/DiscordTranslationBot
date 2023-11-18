namespace DiscordTranslationBot.Mediator;

public interface IBackgroundCommand : IRequest
{
    /// <summary>
    /// Optional delay before executing the command's handler in the background.
    /// </summary>
    TimeSpan? Delay { get; }
}
