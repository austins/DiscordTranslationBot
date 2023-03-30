using Discord.WebSocket;
using Mediator;

namespace DiscordTranslationBot.Notifications;

/// <summary>
/// Notification for the Discord joined guild event.
/// </summary>
public sealed class JoinedGuildNotification : INotification
{
    /// <summary>
    /// The guild that the bot joined.
    /// </summary>
    public required SocketGuild Guild { get; init; }
}
