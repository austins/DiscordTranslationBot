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
                .Reply.Channel.GetMessageAsync(command.SourceMessageId, options: Arg.Any<RequestOptions?>())
                .Returns(sourceMessage);
        }

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await command.Reply.ReceivedWithAnyArgs(1).DeleteAsync();

        var deletedTempReplyLogEntry = _logger.Entries[0];
        deletedTempReplyLogEntry.LogLevel.Should().Be(LogLevel.Information);
        deletedTempReplyLogEntry
            .Message.Should()
            .Be($"Deleted temp reply ID {replyId} in channel ID {channelId} and guild ID {guildId}.");

        await command
            .Reply.Channel.Received(hasReactionInfo ? 1 : 0)
            .GetMessageAsync(command.SourceMessageId, options: Arg.Any<RequestOptions?>());

        await sourceMessage
            .Received(hasReactionInfo ? 1 : 0)
            .RemoveReactionAsync(Arg.Any<IEmote>(), Arg.Any<ulong>(), Arg.Any<RequestOptions?>());

        if (hasReactionInfo)
        {
            var removedTempReactionLogEntry = _logger.Entries[1];
            removedTempReactionLogEntry.LogLevel.Should().Be(LogLevel.Information);
            removedTempReactionLogEntry
                .Message.Should()
                .Be(
                    $"Removed temp reaction added by user ID {command.ReactionInfo!.UserId} from message ID {command.SourceMessageId} in channel ID {channelId} and guild ID {guildId}.");
        }
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
        const ulong channelId = 6UL;
        const ulong guildId = 7UL;

        command.Reply.Id.Returns(replyId);
        command.Reply.Channel.Returns(Substitute.For<IMessageChannel, IGuildChannel>());
        command.Reply.Channel.Id.Returns(channelId);
        (command.Reply.Channel as IGuildChannel)!.GuildId.Returns(guildId);

        command
            .Reply.Channel.GetMessageAsync(command.SourceMessageId, options: Arg.Any<RequestOptions?>())
            .Returns((IUserMessage?)null);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await command.Reply.ReceivedWithAnyArgs(1).DeleteAsync();

        _logger.Entries.Should().ContainSingle();
        _logger.Entries[0].LogLevel.Should().Be(LogLevel.Information);
        _logger
            .Entries[0]
            .Message.Should()
            .Be($"Deleted temp reply ID {replyId} in channel ID {channelId} and guild ID {guildId}.");

        await command
            .Reply.Channel.Received(1)
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
        const ulong channelId = 6UL;
        const ulong guildId = 7UL;

        command.Reply.Id.Returns(replyId);
        command.Reply.Channel.Returns(Substitute.For<IMessageChannel, IGuildChannel>());
        command.Reply.Channel.Id.Returns(channelId);
        (command.Reply.Channel as IGuildChannel)!.GuildId.Returns(guildId);

        command
            .Reply.DeleteAsync()
            .ThrowsAsyncForAnyArgs(
                new HttpException(
                    HttpStatusCode.NotFound,
                    Substitute.For<IRequest>(),
                    DiscordErrorCode.UnknownMessage));

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await command.Reply.ReceivedWithAnyArgs(1).DeleteAsync();

        _logger.Entries.Should().ContainSingle();
        _logger.Entries[0].LogLevel.Should().Be(LogLevel.Information);
        _logger
            .Entries[0]
            .Message.Should()
            .Be(
                $"Temp reply ID {replyId} in channel ID {channelId} and guild ID {guildId} was not found and likely manually deleted.");
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
            .Reply.Channel.GetMessageAsync(command.SourceMessageId, options: Arg.Any<RequestOptions?>())
            .Returns(sourceMessage);

        // Act + Assert
        await _sut
            .Awaiting(x => x.Handle(command, TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<Exception>();

        await command.Reply.ReceivedWithAnyArgs(1).DeleteAsync();

        _logger.Entries.Should().ContainSingle();
        _logger.Entries[0].LogLevel.Should().Be(LogLevel.Error);
        _logger.Entries[0].Exception.Should().Be(exception);
        _logger
            .Entries[0]
            .Message.Should()
            .Be($"Failed to delete temp reply ID {replyId} in channel ID {channelId} and guild ID {guildId}.");

        await command
            .Reply.Channel.DidNotReceive()
            .GetMessageAsync(Arg.Any<ulong>(), options: Arg.Any<RequestOptions?>());
    }
}
