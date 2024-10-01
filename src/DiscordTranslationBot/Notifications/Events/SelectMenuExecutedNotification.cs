using System.ComponentModel.DataAnnotations;
using Discord;

namespace DiscordTranslationBot.Notifications.Events;

/// <summary>
/// Notification for the Discord SelectMenuExecuted event.
/// </summary>
public sealed class SelectMenuExecutedNotification : INotification
{
    /// <summary>
    /// The message component.
    /// </summary>
    [Required]
    public required IComponentInteraction MessageComponent { get; init; }
}
