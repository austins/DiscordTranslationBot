using Discord;
using System.ComponentModel.DataAnnotations;

namespace DiscordTranslationBot.Notifications.Events;

/// <summary>
/// Notification for the Discord MessageCommandExecuted event.
/// </summary>
public sealed class MessageCommandExecutedNotification : INotification
{
    /// <summary>
    /// The message command interaction.
    /// </summary>
    [Required]
    public required IMessageCommandInteraction Interaction { get; init; }
}
