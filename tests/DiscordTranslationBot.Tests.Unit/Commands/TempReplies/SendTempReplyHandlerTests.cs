using Discord;
using DiscordTranslationBot.Commands.TempReplies;
using DiscordTranslationBot.Discord.Models;
using DiscordTranslationBot.Jobs;

namespace DiscordTranslationBot.Tests.Unit.Commands.TempReplies;

public sealed class SendTempReplyHandlerTests
{
    private readonly LoggerFake<SendTempReplyHandler> _logger;
    private readonly IScheduler _scheduler;
    private readonly SendTempReplyHandler _sut;

    public SendTempReplyHandlerTests()
    {
        _scheduler = Substitute.For<IScheduler>();
        _logger = new LoggerFake<SendTempReplyHandler>();

        _sut = new SendTempReplyHandler(_scheduler, _logger);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_SendTempReply_Success(bool hasReaction)
    {
        // Arrange
        var command = new SendTempReply
        {
            Text = "test",
            ReactionInfo = hasReaction
                ? new ReactionInfo
                {
                    UserId = 1,
                    Emote = Substitute.For<IEmote>()
                }
                : null,
            SourceMessage = Substitute.For<IUserMessage>()
        };

        const ulong sourceMessageId = 100UL;
        command.SourceMessage.Id.Returns(sourceMessageId);

        var reply = Substitute.For<IUserMessage>();
        command.SourceMessage.Channel.SendMessageAsync().ReturnsForAnyArgs(reply);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        command.SourceMessage.Channel.ReceivedWithAnyArgs(1).EnterTypingState();
        await command.SourceMessage.Channel.ReceivedWithAnyArgs(1).SendMessageAsync();

        _scheduler
            .Received(1)
            .Schedule(
                Arg.Is<DeleteTempReply>(
                    x => ReferenceEquals(x.Reply, reply)
                         && x.SourceMessageId == sourceMessageId
                         && ReferenceEquals(x.ReactionInfo, command.ReactionInfo)),
                command.DeletionDelay);
    }

    [Fact]
    public async Task Handle_SendTempReply_FailedToSendTempMessage()
    {
        // Arrange
        var command = new SendTempReply
        {
            Text = "test",
            ReactionInfo = null,
            SourceMessage = Substitute.For<IUserMessage>()
        };

        const ulong sourceMessageId = 100UL;
        command.SourceMessage.Id.Returns(sourceMessageId);

        var exception = new Exception();
        command.SourceMessage.Channel.SendMessageAsync().ThrowsAsyncForAnyArgs(exception);

        // Act + Assert
        await _sut.Awaiting(x => x.Handle(command, CancellationToken.None)).Should().ThrowAsync<Exception>();

        command.SourceMessage.Channel.ReceivedWithAnyArgs(1).EnterTypingState();
        await command.SourceMessage.Channel.ReceivedWithAnyArgs(1).SendMessageAsync();

        _logger
            .Entries
            .Should()
            .ContainSingle(
                x => x.LogLevel == LogLevel.Error
                     && ReferenceEquals(x.Exception, exception)
                     && x.Message.Contains($"message ID {sourceMessageId}"));

        _scheduler.ReceivedCalls().Should().BeEmpty();
    }
}
