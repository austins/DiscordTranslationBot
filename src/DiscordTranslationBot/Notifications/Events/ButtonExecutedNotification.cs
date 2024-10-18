using Discord;
using System.ComponentModel.DataAnnotations;

namespace DiscordTranslationBot.Notifications.Events;

/// <summary>
/// Notification for the Discord ButtonExecuted event.
/// </summary>
public sealed class ButtonExecutedNotification : INotification
{
    /// <summary>
    /// The button component interaction.
    /// </summary>
    [Required]
    public required IComponentInteraction Interaction { get; init; }
}
