using Discord;
using Discord.WebSocket;
using MediatR;

namespace DiscordTranslationBot.Notifications;

/// <summary>
/// Notification for the Discord ReactionAdded event.
/// </summary>
public sealed class ReactionAddedNotification : INotification
{
    /// <summary>
    /// The user message.
    /// </summary>
    public Cacheable<IUserMessage, ulong> Message { get; set; }

    /// <summary>
    /// The message channel.
    /// </summary>
    public Cacheable<IMessageChannel, ulong> Channel { get; set; }

    /// <summary>
    /// The reaction.
    /// </summary>
    public SocketReaction? Reaction { get; set; }
}