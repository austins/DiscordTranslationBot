using Discord;
using Discord.Net;
using DiscordTranslationBot.Discord.Models;
using System.ComponentModel.DataAnnotations;

namespace DiscordTranslationBot.Commands.TempReplies;

/// <summary>
/// Deletes a temp reply.
/// If there is a reaction associated with the source message, it will be cleared, too.
/// </summary>
internal sealed class DeleteTempReply : ICommand
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

internal sealed partial class DeleteTempReplyHandler : ICommandHandler<DeleteTempReply>
{
    private readonly Log _log;

    public DeleteTempReplyHandler(ILogger<DeleteTempReplyHandler> logger)
    {
        _log = new Log(logger);
    }

    public async ValueTask<Unit> Handle(DeleteTempReply command, CancellationToken cancellationToken)
    {
        // Delete the reply message.
        try
        {
            await command.Reply.DeleteAsync(new RequestOptions { CancelToken = cancellationToken });

            _log.DeletedTempReply(
                command.Reply.Id,
                command.Reply.Channel.Id,
                (command.Reply.Channel as IGuildChannel)?.GuildId);
        }
        catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.UnknownMessage)
        {
            // The message was likely already deleted.
            _log.TempReplyNotFound(
                command.Reply.Id,
                command.Reply.Channel.Id,
                (command.Reply.Channel as IGuildChannel)?.GuildId);
        }
        catch (Exception ex)
        {
            _log.FailedToDeleteTempReply(
                ex,
                command.Reply.Id,
                command.Reply.Channel.Id,
                (command.Reply.Channel as IGuildChannel)?.GuildId);

            throw;
        }

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

                _log.RemovedTempReaction(
                    command.ReactionInfo.UserId,
                    sourceMessage.Id,
                    sourceMessage.Channel.Id,
                    (sourceMessage.Channel as IGuildChannel)?.GuildId);
            }
        }

        return Unit.Value;
    }

    private sealed partial class Log(ILogger logger)
    {
        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Deleted temp reply ID {replyId} in channel ID {channelId} and guild ID {guildId}.")]
        public partial void DeletedTempReply(ulong replyId, ulong channelId, ulong? guildId);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message =
                "Removed temp reaction added by user ID {userId} from message ID {messageId} in channel ID {channelId} and guild ID {guildId}.")]
        public partial void RemovedTempReaction(ulong userId, ulong messageId, ulong channelId, ulong? guildId);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message =
                "Temp reply ID {replyId} in channel ID {channelId} and guild ID {guildId} was not found and likely manually deleted.")]
        public partial void TempReplyNotFound(ulong replyId, ulong channelId, ulong? guildId);

        [LoggerMessage(
            Level = LogLevel.Error,
            Message = "Failed to delete temp reply ID {replyId} in channel ID {channelId} and guild ID {guildId}.")]
        public partial void FailedToDeleteTempReply(Exception ex, ulong replyId, ulong channelId, ulong? guildId);
    }
}
