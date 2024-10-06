using System.ComponentModel.DataAnnotations;
using Discord;
using Discord.Net;
using DiscordTranslationBot.Discord.Models;
using IMessage = Discord.IMessage;

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

    /// <summary>
    /// Initializes a new instance of the <see cref="SendTempReplyHandler" /> class.
    /// </summary>
    /// <param name="logger">Logger to use.</param>
    public SendTempReplyHandler(ILogger<SendTempReplyHandler> logger)
    {
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

        _log.WaitingToDeleteTempMessage(reply.Id, command.DeletionDelay.TotalSeconds);
        await Task.Delay(command.DeletionDelay, cancellationToken);
        await DeleteTempReplyAsync(reply, command, cancellationToken);

        return Unit.Value;
    }

    /// <summary>
    /// Deletes a temp reply. If there is a reaction associated with the source message, it will be cleared, too.
    /// </summary>
    /// <param name="reply">The reply to delete.</param>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task DeleteTempReplyAsync(IMessage reply, SendTempReply command, CancellationToken cancellationToken)
    {
        try
        {
            // If there is also a reaction and the source message still exists, remove the reaction from it.
            if (command.ReactionInfo is not null)
            {
                var sourceMessage = await reply.Channel.GetMessageAsync(
                    command.SourceMessage.Id,
                    options: new RequestOptions { CancelToken = cancellationToken });

                if (sourceMessage is not null)
                {
                    await sourceMessage.RemoveReactionAsync(
                        command.ReactionInfo.Emote,
                        command.ReactionInfo.UserId,
                        new RequestOptions { CancelToken = cancellationToken });
                }
            }

            // Delete the reply message.
            try
            {
                await reply.DeleteAsync(new RequestOptions { CancelToken = cancellationToken });
                _log.DeletedTempMessage(reply.Id);
            }
            catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.UnknownMessage)
            {
                // The message was already deleted.
            }
        }
        catch (Exception ex)
        {
            _log.FailedToDeleteTempMessage(ex, reply.Id);
            throw;
        }
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
            Message = "Failed to send temp message for reaction to message ID {referencedMessageId} with text: {text}")]
        public partial void FailedToSendTempMessage(Exception ex, ulong referencedMessageId, string text);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Temp message ID {replyId} will be deleted in {totalSeconds}s.")]
        public partial void WaitingToDeleteTempMessage(ulong replyId, double totalSeconds);

        [LoggerMessage(Level = LogLevel.Information, Message = "Deleted temp message ID {replyId}.")]
        public partial void DeletedTempMessage(ulong replyId);

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to delete temp message ID {replyId}.")]
        public partial void FailedToDeleteTempMessage(Exception ex, ulong replyId);
    }
}
