using Discord;
using Discord.WebSocket;

namespace DiscordTranslationBot.Discord.Events;

/// <summary>
/// Notification for the Discord ReactionAdded event.
/// </summary>
public sealed class ReactionAddedEvent : INotification
{
    /// <summary>
    /// The user message.
    /// </summary>
    public required Cacheable<IUserMessage, ulong> Message { get; init; }

    /// <summary>
    /// The channel.
    /// </summary>
    public required Cacheable<IMessageChannel, ulong> Channel { get; init; }

    /// <summary>
    /// The reaction.
    /// </summary>
    public required SocketReaction Reaction { get; init; }
}
