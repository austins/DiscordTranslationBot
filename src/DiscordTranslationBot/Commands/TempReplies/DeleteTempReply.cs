using System.ComponentModel.DataAnnotations;
using Discord;
using Discord.Net;
using DiscordTranslationBot.Discord.Models;

namespace DiscordTranslationBot.Commands.TempReplies;

/// <summary>
/// Deletes a temp reply.
/// If there is a reaction associated with the source message, it will be cleared, too.
/// </summary>
public sealed class DeleteTempReply : ICommand
{
    /// <summary>
    /// The temp reply to delete.
    /// </summary>
    [Required]
    public required IUserMessage Reply { get; init; }

    /// <summary>
    /// The source message ID that the temp reply is associated with.
    /// </summary>
    public required ulong SourceMessageId { get; init; }

    /// <summary>
    /// The reaction associated with the source message, if any.
    /// </summary>
    public ReactionInfo? ReactionInfo { get; init; }
}

public sealed partial class DeleteTempReplyHandler : ICommandHandler<DeleteTempReply>
{
    private readonly Log _log;

    public DeleteTempReplyHandler(ILogger<DeleteTempReplyHandler> logger)
    {
        _log = new Log(logger);
    }

    public async ValueTask<Unit> Handle(DeleteTempReply command, CancellationToken cancellationToken)
    {
        try
        {
            // If there is also a reaction and the source message still exists, remove the reaction from it.
            if (command.ReactionInfo is not null)
            {
                var sourceMessage = await command.Reply.Channel.GetMessageAsync(
                    command.SourceMessageId,
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
                await command.Reply.DeleteAsync(new RequestOptions { CancelToken = cancellationToken });
                _log.DeletedTempReply(command.Reply.Id);
            }
            catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.UnknownMessage)
            {
                // The message was likely already deleted.
                _log.TempReplyNotFound(command.Reply.Id);
            }
        }
        catch (Exception ex)
        {
            _log.FailedToDeleteTempReply(ex, command.Reply.Id);
            throw;
        }

        return Unit.Value;
    }

    private sealed partial class Log
    {
        private readonly ILogger _logger;

        public Log(ILogger logger)
        {
            _logger = logger;
        }

        [LoggerMessage(Level = LogLevel.Information, Message = "Deleted temp reply ID {replyId}.")]
        public partial void DeletedTempReply(ulong replyId);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Temp reply ID {replyId} was not found and likely manually deleted.")]
        public partial void TempReplyNotFound(ulong replyId);

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to delete temp reply ID {replyId}.")]
        public partial void FailedToDeleteTempReply(Exception ex, ulong replyId);
    }
}
