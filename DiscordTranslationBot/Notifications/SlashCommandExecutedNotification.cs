using Discord;

namespace DiscordTranslationBot.Notifications;

/// <summary>
/// Notification for the Discord SlashCommandExecuted event.
/// </summary>
public sealed class SlashCommandExecutedNotification : INotification
{
    /// <summary>
    /// The slash command.
    /// </summary>
    public required ISlashCommandInteraction Command { get; init; }
}
