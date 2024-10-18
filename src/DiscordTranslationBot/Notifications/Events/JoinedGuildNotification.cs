using Discord;
using System.ComponentModel.DataAnnotations;

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
