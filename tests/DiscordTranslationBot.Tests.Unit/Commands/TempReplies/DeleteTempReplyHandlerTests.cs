using Discord;
using Discord.Net;
using DiscordTranslationBot.Commands.TempReplies;
using DiscordTranslationBot.Discord.Models;
using System.Net;

namespace DiscordTranslationBot.Tests.Unit.Commands.TempReplies;

public sealed class DeleteTempReplyHandlerTests
{
    private readonly LoggerFake<DeleteTempReplyHandler> _logger;
    private readonly DeleteTempReplyHandler _sut;

    public DeleteTempReplyHandlerTests()
    {
        _logger = new LoggerFake<DeleteTempReplyHandler>();

        _sut = new DeleteTempReplyHandler(_logger);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_DeleteTempReply_Success(bool hasReactionInfo)
    {
        // Arrange
        var command = new DeleteTempReply
        {
            Reply = Substitute.For<IUserMessage>(),
            SourceMessageId = 1UL,
            ReactionInfo = hasReactionInfo
                ? new ReactionInfo
                {
                    UserId = 1,
                    Emote = Substitute.For<IEmote>()
                }
                : null
        };

        const ulong replyId = 5UL;
        command.Reply.Id.Returns(replyId);

        var sourceMessage = Substitute.For<IUserMessage>();
        if (hasReactionInfo)
        {
            command
                .Reply
                .Channel
                .GetMessageAsync(command.SourceMessageId, options: Arg.Any<RequestOptions?>())
                .Returns(sourceMessage);
        }

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await command.Reply.ReceivedWithAnyArgs(1).DeleteAsync();

        _logger
            .Entries
            .Should()
            .ContainSingle(x => x.LogLevel == LogLevel.Information && x.Message == $"Deleted temp reply ID {replyId}.");

        await command
            .Reply
            .Channel
            .Received(hasReactionInfo ? 1 : 0)
            .GetMessageAsync(command.SourceMessageId, options: Arg.Any<RequestOptions?>());

        await sourceMessage
            .Received(hasReactionInfo ? 1 : 0)
            .RemoveReactionAsync(Arg.Any<IEmote>(), Arg.Any<ulong>(), Arg.Any<RequestOptions?>());
    }

    [Fact]
    public async Task Handle_DeleteTempReply_NoSourceMessageFound_Success()
    {
        // Arrange
        var command = new DeleteTempReply
        {
            Reply = Substitute.For<IUserMessage>(),
            SourceMessageId = 1UL,
            ReactionInfo = new ReactionInfo
            {
                UserId = 1,
                Emote = Substitute.For<IEmote>()
            }
        };

        const ulong replyId = 5UL;
        command.Reply.Id.Returns(replyId);

        command
            .Reply
            .Channel
            .GetMessageAsync(command.SourceMessageId, options: Arg.Any<RequestOptions?>())
            .Returns((IUserMessage?)null);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await command.Reply.ReceivedWithAnyArgs(1).DeleteAsync();

        _logger
            .Entries
            .Should()
            .ContainSingle(x => x.LogLevel == LogLevel.Information && x.Message == $"Deleted temp reply ID {replyId}.");

        await command
            .Reply
            .Channel
            .Received(1)
            .GetMessageAsync(command.SourceMessageId, options: Arg.Any<RequestOptions?>());
    }

    [Fact]
    public async Task Handle_DeleteTempReply_TempMessageNotFound()
    {
        // Arrange
        var command = new DeleteTempReply
        {
            Reply = Substitute.For<IUserMessage>(),
            SourceMessageId = 1UL,
            ReactionInfo = null
        };

        const ulong replyId = 5UL;
        command.Reply.Id.Returns(replyId);

        command
            .Reply
            .DeleteAsync()
            .ThrowsAsyncForAnyArgs(
                new HttpException(
                    HttpStatusCode.NotFound,
                    Substitute.For<IRequest>(),
                    DiscordErrorCode.UnknownMessage));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await command.Reply.ReceivedWithAnyArgs(1).DeleteAsync();

        _logger
            .Entries
            .Should()
            .ContainSingle(
                x => x.LogLevel == LogLevel.Information
                     && x.Message == $"Temp reply ID {replyId} was not found and likely manually deleted.");
    }

    [Fact]
    public async Task Handle_DeleteTempReply_FailedToDeleteTempMessage()
    {
        // Arrange
        var command = new DeleteTempReply
        {
            Reply = Substitute.For<IUserMessage>(),
            SourceMessageId = 1UL,
            ReactionInfo = new ReactionInfo
            {
                UserId = 1,
                Emote = Substitute.For<IEmote>()
            }
        };

        const ulong replyId = 5UL;
        command.Reply.Id.Returns(replyId);

        var exception = new Exception();
        command.Reply.DeleteAsync().ThrowsAsyncForAnyArgs(exception);

        var sourceMessage = Substitute.For<IUserMessage>();
        command
            .Reply
            .Channel
            .GetMessageAsync(command.SourceMessageId, options: Arg.Any<RequestOptions?>())
            .Returns(sourceMessage);

        // Act + Assert
        await _sut.Awaiting(x => x.Handle(command, CancellationToken.None)).Should().ThrowAsync<Exception>();

        // Assert
        await command.Reply.ReceivedWithAnyArgs(1).DeleteAsync();

        _logger
            .Entries
            .Should()
            .ContainSingle(
                x => x.LogLevel == LogLevel.Error
                     && ReferenceEquals(x.Exception, exception)
                     && x.Message == $"Failed to delete temp reply ID {replyId}.");

        await command
            .Reply
            .Channel
            .DidNotReceive()
            .GetMessageAsync(Arg.Any<ulong>(), options: Arg.Any<RequestOptions?>());
    }
}
