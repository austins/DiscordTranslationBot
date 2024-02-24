using Discord.WebSocket;

namespace DiscordTranslationBot.Discord.Events;

/// <summary>
/// Notification for the Discord MessageCommandExecuted event.
/// </summary>
public sealed class MessageCommandExecutedEvent : INotification
{
    /// <summary>
    /// The slash command.
    /// </summary>
    public required SocketMessageCommand MessageCommand { get; init; }
}
