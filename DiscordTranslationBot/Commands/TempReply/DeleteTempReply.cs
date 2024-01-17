using Discord;
using DiscordTranslationBot.Mediator;
using DiscordTranslationBot.Models.Discord;
using FluentValidation;

namespace DiscordTranslationBot.Commands.TempReply;

/// <summary>
/// Command to delete a temp reply.
/// </summary>
public sealed class DeleteTempReply : ITempReplyCommand, IBackgroundCommand
{
    /// <summary>
    /// The reply message to delete.
    /// </summary>
    public required IMessage Reply { get; init; }

    /// <inheritdoc cref="IBackgroundCommand.Delay" />
    public TimeSpan? Delay { get; init; }

    /// <inheritdoc cref="ITempReplyCommand.Reaction" />
    public Reaction? Reaction { get; init; }

    /// <inheritdoc cref="ITempReplyCommand.SourceMessage" />
    public required IMessage SourceMessage { get; init; }
}

/// <summary>
/// Validator for <see cref="DeleteTempReply" />.
/// </summary>
public sealed class DeleteTempReplyValidator : AbstractValidator<DeleteTempReply>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteTempReplyValidator" /> class.
    /// </summary>
    public DeleteTempReplyValidator()
    {
        Include(new TempReplyCommandValidator());
        RuleFor(x => x.Reply).NotNull();
    }
}
