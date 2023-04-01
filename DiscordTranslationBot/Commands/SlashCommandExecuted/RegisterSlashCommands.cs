using Discord;
using Mediator;

namespace DiscordTranslationBot.Commands.SlashCommandExecuted;

/// <summary>
/// Command for registering slash commands.
/// </summary>
public sealed class RegisterSlashCommands : ICommand
{
    /// <summary>
    /// If specified, a guild to register the slash commands for,
    /// otherwise all guilds will have the slash commands registered.
    /// </summary>
    public IGuild? Guild { get; init; }
}
