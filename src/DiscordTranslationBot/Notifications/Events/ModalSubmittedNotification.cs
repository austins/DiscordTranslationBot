using Discord;
using System.ComponentModel.DataAnnotations;

namespace DiscordTranslationBot.Notifications.Events;

/// <summary>
/// Notification for the Discord ModalSubmitted event.
/// </summary>
internal sealed class ModalSubmittedNotification : INotification
{
    /// <summary>
    /// The modal interaction.
    /// </summary>
    [Required]
    public required IModalInteraction Interaction { get; init; }
}
