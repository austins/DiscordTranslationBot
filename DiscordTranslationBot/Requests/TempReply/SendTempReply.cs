using Discord;
using DiscordTranslationBot.Models.Discord;

namespace DiscordTranslationBot.Requests.TempReply;

public sealed class SendTempReply : ITempReplyRequest
{
    public required string Text { get; init; }

    public int DeletionDelayInSeconds { get; init; } = 10;

    public Reaction? Reaction { get; init; }

    public required IMessage SourceMessage { get; init; }
}
