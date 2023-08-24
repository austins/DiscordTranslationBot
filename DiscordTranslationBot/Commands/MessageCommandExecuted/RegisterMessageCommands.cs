using Discord;

namespace DiscordTranslationBot.Commands.MessageCommandExecuted;

/// <summary>
/// Command for registering message commands.
/// </summary>
public sealed class RegisterMessageCommands : IRequest, IRegisterCommands
{
    /// <inheritdoc cref="IRegisterCommands.Guild" />
    public IGuild? Guild { get; init; }
}
