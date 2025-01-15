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
        _logger.Entries.Count.ShouldBe(2);

        var messageName = message.GetType().Name;

        var executingLog = _logger.Entries[0];
        executingLog.LogLevel.ShouldBe(LogLevel.Information);
        executingLog.Message.ShouldBe($"Executing message '{messageName}'...");

        var executedLog = _logger.Entries[1];
        executedLog.LogLevel.ShouldBe(LogLevel.Information);
        executedLog.Message.ShouldStartWith($"Executed message '{messageName}'. Elapsed time:");
    }
}
