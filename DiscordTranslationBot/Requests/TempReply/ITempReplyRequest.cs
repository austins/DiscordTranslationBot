using Discord;
using DiscordTranslationBot.Models.Discord;

namespace DiscordTranslationBot.Requests.TempReply;

public interface ITempReplyRequest : IRequest
{
    public Reaction? Reaction { get; init; }

    public IMessage SourceMessage { get; init; }
}
