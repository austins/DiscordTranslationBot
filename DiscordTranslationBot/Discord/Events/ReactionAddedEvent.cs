using Discord;
using DiscordTranslationBot.Discord.Models;

namespace DiscordTranslationBot.Discord.Events;

/// <summary>
/// Notification for the Discord ReactionAdded event.
/// </summary>
public sealed class ReactionAddedEvent : INotification
{
    /// <summary>
    /// The user message.
    /// </summary>
    public required IUserMessage Message { get; init; }

    /// <summary>
    /// The channel.
    /// </summary>
    public required IMessageChannel Channel { get; init; }

    /// <summary>
    /// The reaction.
    /// </summary>
    public required ReactionInfo ReactionInfo { get; init; }
}
