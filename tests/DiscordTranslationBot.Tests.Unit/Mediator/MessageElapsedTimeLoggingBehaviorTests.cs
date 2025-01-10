using DiscordTranslationBot.Mediator;
using Mediator;

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
        _logger.Entries.Count.Should().Be(2);

        var messageName = message.GetType().Name;

        var executingLog = _logger.Entries[0];
        executingLog.LogLevel.Should().Be(LogLevel.Information);
        executingLog.Message.Should().Be($"Executing message '{messageName}'...");

        var executedLog = _logger.Entries[1];
        executedLog.LogLevel.Should().Be(LogLevel.Information);
        executedLog.Message.Should().StartWith($"Executed message '{messageName}'. Elapsed time:");
    }
}
