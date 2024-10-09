using System.ComponentModel.DataAnnotations;
using Discord;
using DiscordTranslationBot.Discord.Models;

namespace DiscordTranslationBot.Notifications.Events;

/// <summary>
/// Notification for the Discord ReactionAdded event.
/// </summary>
internal sealed class ReactionAddedNotification : INotification
{
    /// <summary>
    /// The user message.
    /// </summary>
    [Required]
    public required IUserMessage Message { get; init; }

    /// <summary>
    /// The channel.
    /// </summary>
    [Required]
    public required IMessageChannel Channel { get; init; }

    /// <summary>
    /// The reaction.
    /// </summary>
    [Required]
    public required ReactionInfo ReactionInfo { get; init; }
}
