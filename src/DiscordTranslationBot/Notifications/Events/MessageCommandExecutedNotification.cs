using System.ComponentModel.DataAnnotations;
using Discord;

namespace DiscordTranslationBot.Notifications.Events;

/// <summary>
/// Notification for the Discord MessageCommandExecuted event.
/// </summary>
internal sealed class MessageCommandExecutedNotification : INotification
{
    /// <summary>
    /// The message command interaction.
    /// </summary>
    [Required]
    public required IMessageCommandInteraction Interaction { get; init; }
}
