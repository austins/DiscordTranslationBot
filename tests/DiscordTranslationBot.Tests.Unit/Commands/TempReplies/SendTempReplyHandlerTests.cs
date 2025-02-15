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

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task Handle_SendTempReply_Success(bool hasReaction, CancellationToken cancellationToken)
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
        await _sut.Handle(command, cancellationToken);

        // Assert
        command.SourceMessage.Channel.ReceivedWithAnyArgs(1).EnterTypingState();
        await command.SourceMessage.Channel.ReceivedWithAnyArgs(1).SendMessageAsync();

        await _scheduler
            .Received(1)
            .ScheduleAsync(
                Arg.Is<DeleteTempReply>(
                    x => ReferenceEquals(x.Reply, reply)
                         && x.SourceMessageId == sourceMessageId
                         && ReferenceEquals(x.ReactionInfo, command.ReactionInfo)),
                command.DeletionDelay,
                cancellationToken);
    }

    [Test]
    public async Task Handle_SendTempReply_FailedToSendTempMessage(CancellationToken cancellationToken)
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
        await Should.ThrowAsync<Exception>(async () => await _sut.Handle(command, cancellationToken));

        command.SourceMessage.Channel.ReceivedWithAnyArgs(1).EnterTypingState();
        await command.SourceMessage.Channel.ReceivedWithAnyArgs(1).SendMessageAsync();

        var logEntry = _logger.Entries.ShouldHaveSingleItem();
        logEntry.LogLevel.ShouldBe(LogLevel.Error);
        logEntry.Exception.ShouldBe(exception);
        logEntry.Message.ShouldContain($"message ID {sourceMessageId}");

        _scheduler.ReceivedCalls().ShouldBeEmpty();
    }
}
