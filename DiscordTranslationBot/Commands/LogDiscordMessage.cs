using Discord;
using Mediator;

namespace DiscordTranslationBot.Commands;

/// <summary>
/// Command to log a Discord log message.
/// </summary>
public sealed class LogDiscordMessage : ICommand
{
    /// <summary>
    /// The Discord log message.
    /// </summary>
    public required LogMessage LogMessage { get; init; }
}
