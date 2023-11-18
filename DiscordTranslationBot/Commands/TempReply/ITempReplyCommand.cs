using Discord;
using DiscordTranslationBot.Models.Discord;
using FluentValidation;

namespace DiscordTranslationBot.Commands.TempReply;

public interface ITempReplyCommand : IRequest
{
    public Reaction? Reaction { get; init; }

    public IMessage SourceMessage { get; init; }
}

public sealed class ITempReplyCommandValidator : AbstractValidator<ITempReplyCommand>
{
    public ITempReplyCommandValidator()
    {
        RuleFor(x => x.SourceMessage).NotNull();
    }
}
