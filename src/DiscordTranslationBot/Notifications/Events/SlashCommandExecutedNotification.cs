using Discord;
using System.ComponentModel.DataAnnotations;

namespace DiscordTranslationBot.Notifications.Events;

/// <summary>
/// Notification for the Discord SlashCommandExecuted event.
/// </summary>
internal sealed class SlashCommandExecutedNotification : INotification
{
    /// <summary>
    /// The slash command interaction.
    /// </summary>
    [Required]
    public required ISlashCommandInteraction Interaction { get; init; }
}
