using Discord;

namespace DiscordTranslationBot.Commands.SlashCommandExecuted;

/// <summary>
/// Command to process a translate slash command.
/// </summary>
public sealed class ProcessTranslateSlashCommand : IRequest
{
    /// <summary>
    /// The slash command from the event.
    /// </summary>
    public required ISlashCommandInteraction Command { get; init; }
}
