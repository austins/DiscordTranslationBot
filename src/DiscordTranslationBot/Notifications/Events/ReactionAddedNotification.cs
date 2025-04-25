using Discord;
using DiscordTranslationBot.Discord.Models;
using System.ComponentModel.DataAnnotations;

namespace DiscordTranslationBot.Notifications.Events;

/// <summary>
/// Notification for the Discord ReactionAdded event.
/// </summary>
public sealed class ReactionAddedNotification : INotification
{
    /// <summary>
    /// The user message.
    /// </summary>
    [Required]
    public required Cacheable<IUserMessage, ulong> Message { get; init; }

    /// <summary>
    /// The channel.
    /// </summary>
    [Required]
    public required Cacheable<IMessageChannel, ulong> Channel { get; init; }

    /// <summary>
    /// The reaction.
    /// </summary>
    [Required]
    public required ReactionInfo ReactionInfo { get; init; }
}
