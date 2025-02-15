using Discord;
using DiscordTranslationBot.Discord.Models;
using DiscordTranslationBot.Jobs;

namespace DiscordTranslationBot.Commands.TempReplies;

/// <summary>
/// Sends a temp reply.
/// </summary>
/// <remarks>
/// This is needed in cases where it's not possible to send an ephemeral message, which are only possible to send with
/// message and slash commands, but not when sending a new message to a channel such as when we are handling a reaction.
/// </remarks>
public sealed class SendTempReply : ICommand
{
    /// <summary>
    /// The source message.
    /// </summary>
    public required IUserMessage SourceMessage { get; init; }

    /// <summary>
    /// The reaction associated with the source message, if any.
    /// </summary>
    public ReactionInfo? ReactionInfo { get; init; }

    /// <summary>
    /// The reply text.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// The delay after which the reply will be deleted.
    /// </summary>
    public TimeSpan DeletionDelay { get; init; } = TimeSpan.FromSeconds(15);
}

public sealed class SendTempReplyValidator : AbstractValidator<SendTempReply>
{
    public SendTempReplyValidator()
    {
        RuleFor(x => x.SourceMessage).NotNull();
        RuleFor(x => x.Text).NotEmpty();
        RuleFor(x => x.DeletionDelay).InclusiveBetween(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(90));
    }
}

/// <summary>
/// Handler for temp replies.
/// </summary>
public sealed partial class SendTempReplyHandler : ICommandHandler<SendTempReply>
{
    private readonly Log _log;
    private readonly IScheduler _scheduler;

    /// <summary>
    /// Initializes a new instance of the <see cref="SendTempReplyHandler" /> class.
    /// </summary>
    /// <param name="scheduler">Scheduler to use.</param>
    /// <param name="logger">Logger to use.</param>
    public SendTempReplyHandler(IScheduler scheduler, ILogger<SendTempReplyHandler> logger)
    {
        _scheduler = scheduler;
        _log = new Log(logger);
    }

    /// <summary>
    /// Sends a temp reply to another message.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async ValueTask<Unit> Handle(SendTempReply command, CancellationToken cancellationToken)
    {
        var typingState =
            command.SourceMessage.Channel.EnterTypingState(new RequestOptions { CancelToken = cancellationToken });

        IUserMessage reply;
        try
        {
            // Send reply message.
            reply = await command.SourceMessage.Channel.SendMessageAsync(
                command.Text,
                messageReference: new MessageReference(command.SourceMessage.Id),
                options: new RequestOptions { CancelToken = cancellationToken });
        }
        catch (Exception ex)
        {
            _log.FailedToSendTempReply(ex, command.SourceMessage.Id);
            throw;
        }
        finally
        {
            typingState.Dispose();
        }

        await _scheduler.ScheduleAsync(
            new DeleteTempReply
            {
                Reply = reply,
                SourceMessageId = command.SourceMessage.Id,
                ReactionInfo = command.ReactionInfo
            },
            command.DeletionDelay,
            cancellationToken);

        _log.ScheduledDeleteTempReply(reply.Id, command.DeletionDelay.TotalSeconds);

        return Unit.Value;
    }

    private sealed partial class Log(ILogger logger)
    {
        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to send temp reply to message ID {sourceMessageId}.")]
        public partial void FailedToSendTempReply(Exception ex, ulong sourceMessageId);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Temp reply ID {replyId} will be deleted in {totalSeconds}s.")]
        public partial void ScheduledDeleteTempReply(ulong replyId, double totalSeconds);
    }
}
