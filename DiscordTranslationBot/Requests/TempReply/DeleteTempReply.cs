using Discord;
using DiscordTranslationBot.Models.Discord;
using FluentValidation;

namespace DiscordTranslationBot.Requests.TempReply;

public sealed class DeleteTempReply : ITempReplyRequest
{
    public required IMessage Reply { get; init; }

    public Reaction? Reaction { get; init; }

    public required IMessage SourceMessage { get; init; }
}

public sealed class DeleteTempReplyValidator : AbstractValidator<DeleteTempReply>
{
    public DeleteTempReplyValidator()
    {
        Include(new ITempReplyRequestValidator());
        RuleFor(x => x.Reply).NotNull();
    }
}
