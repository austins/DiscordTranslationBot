using System.ComponentModel.DataAnnotations;
using Discord;

namespace DiscordTranslationBot.Notifications.Events;

/// <summary>
/// Notification for the Discord joined guild event.
/// </summary>
public sealed class JoinedGuildNotification : INotification
{
    /// <summary>
    /// The guild that the bot joined.
    /// </summary>
    [Required]
    public required IGuild Guild { get; init; }
}
