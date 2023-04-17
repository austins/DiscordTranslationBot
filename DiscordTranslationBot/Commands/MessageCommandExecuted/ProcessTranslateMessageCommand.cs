using Discord;

namespace DiscordTranslationBot.Commands.MessageCommandExecuted;

/// <summary>
/// Command to process a translate message command.
/// </summary>
public sealed class ProcessTranslateMessageCommand : ICommand
{
    /// <summary>
    /// The message command from the event.
    /// </summary>
    public required IMessageCommandInteraction Command { get; init; }
}
