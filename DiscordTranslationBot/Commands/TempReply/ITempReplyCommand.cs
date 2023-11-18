using Discord;
using DiscordTranslationBot.Models.Discord;
using FluentValidation;

namespace DiscordTranslationBot.Commands.TempReply;

/// <summary>
/// Interface for a temp reply command.
/// </summary>
public interface ITempReplyCommand : IRequest
{
    /// <summary>
    /// The reaction associated with the source message, if any.
    /// </summary>
    public Reaction? Reaction { get; init; }

    /// <summary>
    /// The source message.
    /// </summary>
    public IMessage SourceMessage { get; init; }
}

/// <summary>
/// Validator for <see cref="ITempReplyCommand" />.
/// </summary>
public sealed class ITempReplyCommandValidator : AbstractValidator<ITempReplyCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ITempReplyCommandValidator" /> class.
    /// </summary>
    public ITempReplyCommandValidator()
    {
        RuleFor(x => x.SourceMessage).NotNull();
    }
}
