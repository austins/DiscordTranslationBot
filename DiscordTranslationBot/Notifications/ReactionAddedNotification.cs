using Discord;
using DiscordTranslationBot.Models.Discord;

namespace DiscordTranslationBot.Notifications;

/// <summary>
/// Notification for the Discord ReactionAdded event.
/// </summary>
public sealed class ReactionAddedNotification : INotification
{
    /// <summary>
    /// The user message.
    /// </summary>
    public required IUserMessage Message { get; init; }

    /// <summary>
    /// The reaction.
    /// </summary>
    public required Reaction Reaction { get; init; }
}
