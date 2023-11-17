using Discord;
using DiscordTranslationBot.Models.Discord;
using FluentValidation;

namespace DiscordTranslationBot.Requests.TempReply;

public interface ITempReplyRequest : IRequest
{
    public Reaction? Reaction { get; init; }

    public IMessage SourceMessage { get; init; }
}

public sealed class ITempReplyRequestValidator : AbstractValidator<ITempReplyRequest>
{
    public ITempReplyRequestValidator()
    {
        RuleFor(x => x.SourceMessage).NotNull();
    }
}
