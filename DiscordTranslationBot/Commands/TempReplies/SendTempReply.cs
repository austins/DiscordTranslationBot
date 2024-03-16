using Discord;
using Discord.Net;
using DiscordTranslationBot.Discord.Models;
using FluentValidation;
using IRequest = MediatR.IRequest;

namespace DiscordTranslationBot.Commands.TempReplies;

/// <summary>
/// Sends a temp reply.
/// </summary>
public sealed class SendTempReply : IRequest
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
    public TimeSpan DeletionDelay { get; init; } = TimeSpan.FromSeconds(10);
}

public sealed class SendTempReplyValidator : AbstractValidator<SendTempReply>
{
    public SendTempReplyValidator()
    {
        RuleFor(x => x.Text).NotEmpty();
        RuleFor(x => x.DeletionDelay).GreaterThan(TimeSpan.Zero);
    }
}

/// <summary>
/// Handler for temp replies.
/// </summary>
public sealed partial class SendTempReplyHandler : IRequestHandler<SendTempReply>
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
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task Handle(SendTempReply request, CancellationToken cancellationToken)
    {
        var typingState =
            request.SourceMessage.Channel.EnterTypingState(new RequestOptions { CancelToken = cancellationToken });

        IUserMessage reply;
        try
        {
            // Send reply message.
            reply = await request.SourceMessage.Channel.SendMessageAsync(
                request.Text,
                messageReference: new MessageReference(request.SourceMessage.Id),
                options: new RequestOptions { CancelToken = cancellationToken });
        }
        catch (Exception ex)
        {
            _log.FailedToSendTempMessage(ex, request.SourceMessage.Id, request.Text);
            throw;
        }
        finally
        {
            typingState.Dispose();
        }

        _log.WaitingToDeleteTempMessage(reply.Id, request.DeletionDelay.TotalSeconds);
        await Task.Delay(request.DeletionDelay, cancellationToken);
        await DeleteTempReplyAsync(reply, request, cancellationToken);
    }

    /// <summary>
    /// Deletes a temp reply. If there is a reaction associated with the source message, it will be cleared, too.
    /// </summary>
    /// <param name="reply">The reply to delete.</param>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task DeleteTempReplyAsync(IMessage reply, SendTempReply request, CancellationToken cancellationToken)
    {
        try
        {
            // If there is also a reaction and the source message still exists, remove the reaction from it.
            if (request.ReactionInfo is not null)
            {
                var sourceMessage = await reply.Channel.GetMessageAsync(
                    request.SourceMessage.Id,
                    options: new RequestOptions { CancelToken = cancellationToken });

                if (sourceMessage is not null)
                {
                    await sourceMessage.RemoveReactionAsync(
                        request.ReactionInfo.Emote,
                        request.ReactionInfo.UserId,
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
        private readonly ILogger<SendTempReplyHandler> _logger;

        public Log(ILogger<SendTempReplyHandler> logger)
        {
            _logger = logger;
        }

        [LoggerMessage(
            Level = LogLevel.Error,
            Message = "Failed to send temp message for reaction to message ID {referencedMessageId} with text: {text}")]
        public partial void FailedToSendTempMessage(Exception ex, ulong referencedMessageId, string text);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Temp message with message ID {replyId} will be deleted in {totalSeconds}s.")]
        public partial void WaitingToDeleteTempMessage(ulong replyId, double totalSeconds);

        [LoggerMessage(Level = LogLevel.Information, Message = "Deleted temp message with message ID {replyId}.")]
        public partial void DeletedTempMessage(ulong replyId);

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to delete temp message with message ID {replyId}.")]
        public partial void FailedToDeleteTempMessage(Exception ex, ulong replyId);
    }
}
