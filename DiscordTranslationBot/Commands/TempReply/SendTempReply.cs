using Discord;
using DiscordTranslationBot.Models.Discord;
using FluentValidation;

namespace DiscordTranslationBot.Commands.TempReply;

public sealed class SendTempReply : ITempReplyCommand
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
        Include(new ITempReplyCommandValidator());
        RuleFor(x => x.Text).NotEmpty();
        RuleFor(x => x.DeletionDelayInSeconds).GreaterThan(0);
    }
}
