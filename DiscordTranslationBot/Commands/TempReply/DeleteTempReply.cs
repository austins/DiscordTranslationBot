using Discord;
using DiscordTranslationBot.Models.Discord;
using FluentValidation;

namespace DiscordTranslationBot.Commands.TempReply;

public sealed class DeleteTempReply : ITempReplyCommand
{
    public required IMessage Reply { get; init; }

    public Reaction? Reaction { get; init; }

    public required IMessage SourceMessage { get; init; }
}

public sealed class DeleteTempReplyValidator : AbstractValidator<DeleteTempReply>
{
    public DeleteTempReplyValidator()
    {
        Include(new ITempReplyCommandValidator());
        RuleFor(x => x.Reply).NotNull();
    }
}
