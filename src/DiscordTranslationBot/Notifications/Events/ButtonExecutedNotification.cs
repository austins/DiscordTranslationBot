using System.ComponentModel.DataAnnotations;
using Discord;

namespace DiscordTranslationBot.Notifications.Events;

/// <summary>
/// Notification for the Discord ButtonExecuted event.
/// </summary>
public sealed class ButtonExecutedNotification : INotification
{
    /// <summary>
    /// The message component.
    /// </summary>
    [Required]
    public required IComponentInteraction Interaction { get; init; }
}
