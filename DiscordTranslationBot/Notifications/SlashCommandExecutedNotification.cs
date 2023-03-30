using Discord.WebSocket;
using Mediator;

namespace DiscordTranslationBot.Notifications;

/// <summary>
/// Notification for the Discord SlashCommandExecuted event.
/// </summary>
public sealed class SlashCommandExecutedNotification : INotification
{
    /// <summary>
    /// The slash command.
    /// </summary>
    public required SocketSlashCommand Command { get; init; }
}
