using System.ComponentModel.DataAnnotations;
using Discord;

namespace DiscordTranslationBot.Notifications.Events;

/// <summary>
/// Notification for the Discord SelectMenuExecuted event.
/// </summary>
public sealed class SelectMenuExecutedNotification : INotification
{
    /// <summary>
    /// The select menu component interaction.
    /// </summary>
    [Required]
    public required IComponentInteraction Interaction { get; init; }
}
