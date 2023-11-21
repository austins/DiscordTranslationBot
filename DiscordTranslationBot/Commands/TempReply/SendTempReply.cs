using Discord;
using DiscordTranslationBot.Models.Discord;
using FluentValidation;

namespace DiscordTranslationBot.Commands.TempReply;

/// <summary>
/// Sends a temp reply.
/// </summary>
public sealed class SendTempReply : ITempReplyCommand
{
    /// <summary>
    /// The reply text.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// The delay in seconds after which the reply will be deleted.
    /// </summary>
    public int DeletionDelayInSeconds { get; init; } = 10;

    /// <inheritdoc cref="ITempReplyCommand.Reaction" />
    public Reaction? Reaction { get; init; }

    /// <inheritdoc cref="ITempReplyCommand.SourceMessage" />
    public required IMessage SourceMessage { get; init; }
}

/// <summary>
/// Validator for <see cref="SendTempReply" />.
/// </summary>
public sealed class SendTempReplyValidator : AbstractValidator<SendTempReply>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SendTempReplyValidator" /> class.
    /// </summary>
    public SendTempReplyValidator()
    {
        Include(new TempReplyCommandValidator());
        RuleFor(x => x.Text).NotEmpty();
        RuleFor(x => x.DeletionDelayInSeconds).GreaterThan(0);
    }
}
