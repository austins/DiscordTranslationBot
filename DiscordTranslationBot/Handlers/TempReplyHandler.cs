using Discord;
using DiscordTranslationBot.Commands.TempReply;

namespace DiscordTranslationBot.Handlers;

/// <summary>
/// Handler for temp replies.
/// </summary>
public sealed partial class TempReplyHandler : IRequestHandler<DeleteTempReply>, IRequestHandler<SendTempReply>
{
    private readonly Log _log;
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="TempReplyHandler" /> class.
    /// </summary>
    /// <param name="mediator">Mediator to use.</param>
    /// <param name="logger">Logger to use.</param>
    public TempReplyHandler(IMediator mediator, ILogger<TempReplyHandler> logger)
    {
        _mediator = mediator;
        _log = new Log(logger);
    }

    /// <summary>
    /// Deletes a temp reply. If there is a reaction associated with the source message, it will be cleared, too.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task Handle(DeleteTempReply request, CancellationToken cancellationToken)
    {
        try
        {
            // If there is also a reaction and the source message still exists, remove the reaction from it.
            if (request.Reaction != null)
            {
                var sourceMessage = await request.Reply.Channel.GetMessageAsync(
                    request.SourceMessage.Id,
                    options: new RequestOptions { CancelToken = cancellationToken });

                if (sourceMessage != null)
                {
                    await sourceMessage.RemoveReactionAsync(
                        request.Reaction.Emote,
                        request.Reaction.UserId,
                        new RequestOptions { CancelToken = cancellationToken });
                }
            }

            // Delete the reply message.
            await request.Reply.DeleteAsync(new RequestOptions { CancelToken = cancellationToken });
        }
        catch (Exception ex)
        {
            _log.FailedToDeleteTempMessage(ex, request.Reply.Id);
            throw;
        }
    }

    /// <summary>
    /// Sends a temp reply to another message.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task Handle(SendTempReply request, CancellationToken cancellationToken)
    {
        try
        {
            using var _ =
                request.SourceMessage.Channel.EnterTypingState(new RequestOptions { CancelToken = cancellationToken });

            // Send reply message.
            var reply = await request.SourceMessage.Channel.SendMessageAsync(
                request.Text,
                messageReference: new MessageReference(request.SourceMessage.Id),
                options: new RequestOptions { CancelToken = cancellationToken });

            // Delete the temp reply in the background with a delay as to not block the request
            // and to clear the typing state scope by allowing it to dispose after the reply is sent.
            await _mediator.Send(
                new DeleteTempReply
                {
                    Reply = reply,
                    Reaction = request.Reaction,
                    SourceMessage = request.SourceMessage,
                    Delay = TimeSpan.FromSeconds(request.DeletionDelayInSeconds)
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _log.FailedToSendTempMessage(ex, request.SourceMessage.Id, request.Text);
            throw;
        }
    }

    private sealed partial class Log
    {
        private readonly ILogger<TempReplyHandler> _logger;

        public Log(ILogger<TempReplyHandler> logger)
        {
            _logger = logger;
        }

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to delete temp message for reply ID {replyId}.")]
        public partial void FailedToDeleteTempMessage(Exception ex, ulong replyId);

        [LoggerMessage(
            Level = LogLevel.Error,
            Message = "Failed to send temp message for reaction to message ID {referencedMessageId} with text: {text}")]
        public partial void FailedToSendTempMessage(Exception ex, ulong referencedMessageId, string text);
    }
}
