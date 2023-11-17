using Discord;
using DiscordTranslationBot.Models.Discord;

namespace DiscordTranslationBot.Requests.TempReply;

public sealed class DeleteTempReply : ITempReplyRequest
{
    public required IMessage Reply { get; init; }

    public Reaction? Reaction { get; init; }

    public required IMessage SourceMessage { get; init; }
}
