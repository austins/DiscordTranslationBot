using System.ComponentModel.DataAnnotations;
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
    [Required]
    public required IUserMessage Message { get; init; }

    /// <summary>
    /// The reaction.
    /// </summary>
    [Required]
    public required Reaction Reaction { get; init; }
}
