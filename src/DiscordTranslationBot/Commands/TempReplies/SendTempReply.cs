using System.ComponentModel.DataAnnotations;
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
    [Required]
    public required IUserMessage SourceMessage { get; init; }

    /// <summary>
    /// The reaction associated with the source message, if any.
    /// </summary>
    public ReactionInfo? ReactionInfo { get; init; }

    /// <summary>
    /// The reply text.
    /// </summary>
    [Required]
    public required string Text { get; init; }

    /// <summary>
    /// The delay after which the reply will be deleted.
    /// </summary>
    [Range(typeof(TimeSpan), "00:00:01", "00:01:30")]
    public TimeSpan DeletionDelay { get; init; } = TimeSpan.FromSeconds(15);
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
            _log.FailedToSendTempMessage(ex, command.SourceMessage.Id, command.Text);
            throw;
        }
        finally
        {
            typingState.Dispose();
        }

        _scheduler.Schedule(
            new DeleteTempReply
            {
                Reply = reply,
                SourceMessageId = command.SourceMessage.Id,
                ReactionInfo = command.ReactionInfo
            },
            command.DeletionDelay);

        _log.DeleteTempMessageScheduled(reply.Id, command.DeletionDelay.TotalSeconds);

        return Unit.Value;
    }

    private sealed partial class Log
    {
        private readonly ILogger _logger;

        public Log(ILogger logger)
        {
            _logger = logger;
        }

        [LoggerMessage(
            Level = LogLevel.Error,
            Message = "Failed to send temp message for reaction to message ID {sourceMessageId} with text: {text}")]
        public partial void FailedToSendTempMessage(Exception ex, ulong sourceMessageId, string text);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Temp message ID {replyId} will be deleted in {totalSeconds}s.")]
        public partial void DeleteTempMessageScheduled(ulong replyId, double totalSeconds);
    }
}
