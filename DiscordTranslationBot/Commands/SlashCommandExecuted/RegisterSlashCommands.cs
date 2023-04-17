using Discord;

namespace DiscordTranslationBot.Commands.SlashCommandExecuted;

/// <summary>
/// Command for registering slash commands.
/// </summary>
public sealed class RegisterSlashCommands : ICommand, IRegisterCommands
{
    /// <inheritdoc cref="IRegisterCommands.Guild" />
    public IGuild? Guild { get; init; }
}
