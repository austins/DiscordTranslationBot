using System.ComponentModel.DataAnnotations;
using Discord;

namespace DiscordTranslationBot.Notifications.Events;

/// <summary>
/// Notification for the Discord MessageCommandExecuted event.
/// </summary>
public sealed class MessageCommandExecutedNotification : INotification
{
    /// <summary>
    /// The message command.
    /// </summary>
    [Required]
    public required IMessageCommandInteraction MessageCommand { get; init; }
}
