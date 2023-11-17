using Discord;
using DiscordTranslationBot.Models.Discord;
using FluentValidation;

namespace DiscordTranslationBot.Requests.TempReply;

public sealed class SendTempReply : ITempReplyRequest
{
    public required string Text { get; init; }

    public int DeletionDelayInSeconds { get; init; } = 10;

    public Reaction? Reaction { get; init; }

    public required IMessage SourceMessage { get; init; }
}

public sealed class SendTempReplyValidator : AbstractValidator<SendTempReply>
{
    public SendTempReplyValidator()
    {
        Include(new ITempReplyRequestValidator());
        RuleFor(x => x.Text).NotEmpty();
        RuleFor(x => x.DeletionDelayInSeconds).GreaterThan(0);
    }
}
