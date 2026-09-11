using Discord;
using DiscordTranslationBot.Mediator;
using DiscordTranslationBot.Notifications.Events;
using IMessage = Mediator.IMessage;

namespace DiscordTranslationBot.Tests.Unit.Mediator;

public sealed class MessageElapsedTimeLoggingBehaviorTests
{
    private readonly LoggerFake<MessageElapsedTimeLoggingBehavior<IMessage, bool>> _logger;
    private readonly MessageElapsedTimeLoggingBehavior<IMessage, bool> _sut;

    public MessageElapsedTimeLoggingBehaviorTests()
    {
        _logger = new LoggerFake<MessageElapsedTimeLoggingBehavior<IMessage, bool>>();
        _sut = new MessageElapsedTimeLoggingBehavior<IMessage, bool>(_logger);
    }

    [Fact]
    public async Task Handle_Success_Logs()
    {
        // Arrange
        var message = Substitute.For<IMessage>();

        // Act
        await _sut.Handle(message, (_, _) => ValueTask.FromResult(true), TestContext.Current.CancellationToken);

        // Assert
        _logger.Entries.Count.Should().Be(1);

        var messageName = message.GetType().Name;

        var executedLog = _logger.Entries[0];
        executedLog.LogLevel.Should().Be(LogLevel.Debug);
        executedLog.Message.Should().StartWith($"Executed message '{messageName}'. Elapsed time:");
    }

    [Fact]
    public async Task Handle_LogNotification_DoesNotLog()
    {
        // Arrange
        var message = new LogNotification
        {
            LogMessage = new LogMessage(LogSeverity.Info, "source1", "message1", null)
        };

        // Act
        await _sut.Handle(message, (_, _) => ValueTask.FromResult(true), TestContext.Current.CancellationToken);

        // Assert
        _logger.Entries.Should().BeEmpty();
    }
}
