using Discord;
using Mediator;

namespace DiscordTranslationBot.Commands.SlashCommandExecuted;

/// <summary>
/// Command to process a translate slash command.
/// </summary>
public sealed class ProcessTranslateCommand : ICommand
{
    /// <summary>
    /// The slash command from the event.
    /// </summary>
    public required ISlashCommandInteraction Command { get; init; }
}
