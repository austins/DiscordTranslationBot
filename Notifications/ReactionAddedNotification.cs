using Discord;
using Discord.WebSocket;
using MediatR;

namespace DiscordTranslationBot.Notifications;

internal sealed class ReactionAddedNotification : INotification
{
    public Cacheable<IUserMessage, ulong> Message { get; set; }

    public Cacheable<IMessageChannel, ulong> Channel { get; set; }

    public SocketReaction? Reaction { get; set; }
}