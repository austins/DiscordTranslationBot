using System.ComponentModel.DataAnnotations;
using Discord;

namespace DiscordTranslationBot.Notifications.Events;

/// <summary>
/// Notification for the Discord SlashCommandExecuted event.
/// </summary>
public sealed class SlashCommandExecutedNotification : INotification
{
    /// <summary>
    /// The slash command interaction.
    /// </summary>
    [Required]
    public required ISlashCommandInteraction Interaction { get; init; }
}
