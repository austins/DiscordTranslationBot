using Discord;

namespace DiscordTranslationBot.Commands;

/// <summary>
/// Interface for a command to register Discord commands.
/// </summary>
public interface IRegisterCommands
{
    /// <summary>
    /// If specified, a guild to register the commands for, otherwise all guilds will have the commands registered.
    /// </summary>
    public IGuild? Guild { get; init; }
}
