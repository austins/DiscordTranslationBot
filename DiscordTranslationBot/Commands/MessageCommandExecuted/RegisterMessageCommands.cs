using Discord;
using Mediator;

namespace DiscordTranslationBot.Commands.MessageCommandExecuted;

/// <summary>
/// Command for registering message commands.
/// </summary>
public sealed class RegisterMessageCommands : ICommand, IRegisterCommands
{
    /// <inheritdoc cref="IRegisterCommands.Guild"/>
    public IGuild? Guild { get; init; }
}
