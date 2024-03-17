using DiscordTranslationBot.Mediator;
using MediatR;

namespace DiscordTranslationBot.Tests.Unit.Mediator;

public sealed class RequestElapsedTimeLoggingBehaviorTests
{
    private readonly LoggerFake<RequestElapsedTimeLoggingBehavior<IRequest, bool>> _logger;
    private readonly RequestElapsedTimeLoggingBehavior<IRequest, bool> _sut;

    public RequestElapsedTimeLoggingBehaviorTests()
    {
        _logger = new LoggerFake<RequestElapsedTimeLoggingBehavior<IRequest, bool>>();
        _sut = new RequestElapsedTimeLoggingBehavior<IRequest, bool>(_logger);
    }

    [Fact]
    public async Task Handle_Success_Logs()
    {
        // Arrange
        var request = Substitute.For<IRequest>();

        // Act
        await _sut.Handle(request, () => Task.FromResult(true), CancellationToken.None);

        // Assert
        _logger.Entries.Count.Should().Be(2);

        var requestName = request.GetType().Name;

        var executingLog = _logger.Entries[0];
        executingLog.LogLevel.Should().Be(LogLevel.Information);
        executingLog.Message.Should().Be($"Executing request '{requestName}'...");

        var executedLog = _logger.Entries[1];
        executedLog.LogLevel.Should().Be(LogLevel.Information);
        executedLog.Message.Should().StartWith($"Executed request '{requestName}'. Elapsed time:");
    }
}
