using Discord;

namespace DiscordTranslationBot.Discord.Events;

/// <summary>
/// Notification for the Discord SlashCommandExecuted event.
/// </summary>
public sealed class SlashCommandExecutedEvent : INotification
{
    /// <summary>
    /// The slash command.
    /// </summary>
    public required ISlashCommandInteraction SlashCommand { get; init; }
}
