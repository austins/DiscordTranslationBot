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

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Handle_DeleteTempReply_Success(bool hasReactionInfo, CancellationToken cancellationToken)
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
        const ulong channelId = 6UL;
        const ulong guildId = 7UL;

        command.Reply.Id.Returns(replyId);
        command.Reply.Channel.Returns(Substitute.For<IMessageChannel, IGuildChannel>());
        command.Reply.Channel.Id.Returns(channelId);
        (command.Reply.Channel as IGuildChannel)!.GuildId.Returns(guildId);

        var sourceMessage = Substitute.For<IUserMessage>();
        if (hasReactionInfo)
        {
            sourceMessage.Id.Returns(command.SourceMessageId);
            sourceMessage.Channel.Returns(Substitute.For<IMessageChannel, IGuildChannel>());
            sourceMessage.Channel.Id.Returns(channelId);
            (sourceMessage.Channel as IGuildChannel)!.GuildId.Returns(guildId);

            command
                .Reply
                .Channel
                .GetMessageAsync(command.SourceMessageId, options: Arg.Any<RequestOptions?>())
                .Returns(sourceMessage);
        }

        // Act
        await _sut.Handle(command, cancellationToken);

        // Assert
        await command.Reply.ReceivedWithAnyArgs(1).DeleteAsync();

        var deletedTempReplyLogEntry = _logger.Entries[0];
        deletedTempReplyLogEntry.LogLevel.ShouldBe(LogLevel.Information);
        deletedTempReplyLogEntry.Message.ShouldBe(
            $"Deleted temp reply ID {replyId} in channel ID {channelId} and guild ID {guildId}.");

        await command
            .Reply
            .Channel
            .Received(hasReactionInfo ? 1 : 0)
            .GetMessageAsync(command.SourceMessageId, options: Arg.Any<RequestOptions?>());

        await sourceMessage
            .Received(hasReactionInfo ? 1 : 0)
            .RemoveReactionAsync(Arg.Any<IEmote>(), Arg.Any<ulong>(), Arg.Any<RequestOptions?>());

        if (hasReactionInfo)
        {
            var removedTempReactionLogEntry = _logger.Entries[1];
            removedTempReactionLogEntry.LogLevel.ShouldBe(LogLevel.Information);
            removedTempReactionLogEntry.Message.ShouldBe(
                $"Removed temp reaction added by user ID {command.ReactionInfo!.UserId} from message ID {command.SourceMessageId} in channel ID {channelId} and guild ID {guildId}.");
        }
    }

    [Test]
    public async Task Handle_DeleteTempReply_NoSourceMessageFound_Success(CancellationToken cancellationToken)
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
        const ulong channelId = 6UL;
        const ulong guildId = 7UL;

        command.Reply.Id.Returns(replyId);
        command.Reply.Channel.Returns(Substitute.For<IMessageChannel, IGuildChannel>());
        command.Reply.Channel.Id.Returns(channelId);
        (command.Reply.Channel as IGuildChannel)!.GuildId.Returns(guildId);

        command
            .Reply
            .Channel
            .GetMessageAsync(command.SourceMessageId, options: Arg.Any<RequestOptions?>())
            .Returns((IUserMessage?)null);

        // Act
        await _sut.Handle(command, cancellationToken);

        // Assert
        await command.Reply.ReceivedWithAnyArgs(1).DeleteAsync();

        var logEntry = _logger.Entries.ShouldHaveSingleItem();
        logEntry.LogLevel.ShouldBe(LogLevel.Information);
        logEntry.Message.ShouldBe($"Deleted temp reply ID {replyId} in channel ID {channelId} and guild ID {guildId}.");

        await command
            .Reply
            .Channel
            .Received(1)
            .GetMessageAsync(command.SourceMessageId, options: Arg.Any<RequestOptions?>());
    }

    [Test]
    public async Task Handle_DeleteTempReply_TempMessageNotFound(CancellationToken cancellationToken)
    {
        // Arrange
        var command = new DeleteTempReply
        {
            Reply = Substitute.For<IUserMessage>(),
            SourceMessageId = 1UL,
            ReactionInfo = null
        };

        const ulong replyId = 5UL;
        const ulong channelId = 6UL;
        const ulong guildId = 7UL;

        command.Reply.Id.Returns(replyId);
        command.Reply.Channel.Returns(Substitute.For<IMessageChannel, IGuildChannel>());
        command.Reply.Channel.Id.Returns(channelId);
        (command.Reply.Channel as IGuildChannel)!.GuildId.Returns(guildId);

        command
            .Reply
            .DeleteAsync()
            .ThrowsAsyncForAnyArgs(
                new HttpException(
                    HttpStatusCode.NotFound,
                    Substitute.For<IRequest>(),
                    DiscordErrorCode.UnknownMessage));

        // Act
        await _sut.Handle(command, cancellationToken);

        // Assert
        await command.Reply.ReceivedWithAnyArgs(1).DeleteAsync();

        var logEntry = _logger.Entries.ShouldHaveSingleItem();
        logEntry.LogLevel.ShouldBe(LogLevel.Information);
        logEntry.Message.ShouldBe(
            $"Temp reply ID {replyId} in channel ID {channelId} and guild ID {guildId} was not found and likely manually deleted.");
    }

    [Test]
    public async Task Handle_DeleteTempReply_FailedToDeleteTempMessage(CancellationToken cancellationToken)
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
        const ulong channelId = 6UL;
        const ulong guildId = 7UL;

        command.Reply.Id.Returns(replyId);
        command.Reply.Channel.Returns(Substitute.For<IMessageChannel, IGuildChannel>());
        command.Reply.Channel.Id.Returns(channelId);
        (command.Reply.Channel as IGuildChannel)!.GuildId.Returns(guildId);

        var exception = new Exception();
        command.Reply.DeleteAsync().ThrowsAsyncForAnyArgs(exception);

        var sourceMessage = Substitute.For<IUserMessage>();
        command
            .Reply
            .Channel
            .GetMessageAsync(command.SourceMessageId, options: Arg.Any<RequestOptions?>())
            .Returns(sourceMessage);

        // Act + Assert
        await _sut.Handle(command, cancellationToken).AsTask().ShouldThrowAsync<Exception>();

        await command.Reply.ReceivedWithAnyArgs(1).DeleteAsync();

        var logEntry = _logger.Entries.ShouldHaveSingleItem();
        logEntry.LogLevel.ShouldBe(LogLevel.Error);
        logEntry.Exception.ShouldBe(exception);
        logEntry.Message.ShouldBe(
            $"Failed to delete temp reply ID {replyId} in channel ID {channelId} and guild ID {guildId}.");

        await command
            .Reply
            .Channel
            .DidNotReceive()
            .GetMessageAsync(Arg.Any<ulong>(), options: Arg.Any<RequestOptions?>());
    }
}
