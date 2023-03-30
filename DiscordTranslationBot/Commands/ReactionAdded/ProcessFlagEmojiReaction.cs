using Discord;
using DiscordTranslationBot.Models;
using DiscordTranslationBot.Models.Discord;
using Mediator;

namespace DiscordTranslationBot.Commands.ReactionAdded;

/// <summary>
/// Command for processing a flag emoji reaction.
/// </summary>
public sealed class ProcessFlagEmojiReaction : ICommand
{
    /// <summary>
    /// The user message.
    /// </summary>
    public required IUserMessage Message { get; init; }

    /// <summary>
    /// The reaction.
    /// </summary>
    public required Reaction Reaction { get; init; }

    /// <summary>
    /// The country.
    /// </summary>
    public required Country Country { get; init; }
}
