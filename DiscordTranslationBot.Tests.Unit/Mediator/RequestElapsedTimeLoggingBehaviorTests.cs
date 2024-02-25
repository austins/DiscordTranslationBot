using DiscordTranslationBot.Mediator;
using MediatR;

namespace DiscordTranslationBot.Tests.Unit.Mediator;

public sealed class RequestElapsedTimeLoggingBehaviorTests
{
    private readonly LoggerFake<RequestElapsedTimeLoggingBehavior<IRequest, MediatR.Unit>> _logger;
    private readonly RequestElapsedTimeLoggingBehavior<IRequest, MediatR.Unit> _sut;

    public RequestElapsedTimeLoggingBehaviorTests()
    {
        _logger = new LoggerFake<RequestElapsedTimeLoggingBehavior<IRequest, MediatR.Unit>>();
        _sut = new RequestElapsedTimeLoggingBehavior<IRequest, MediatR.Unit>(_logger);
    }

    [Test]
    public async Task Handle_Success_Logs()
    {
        // Arrange
        var request = Substitute.For<IRequest>();

        // Act
        await _sut.Handle(request, () => MediatR.Unit.Task, CancellationToken.None);

        // Assert
        _logger.Entries.Should().HaveCount(2);

        var requestName = request.GetType().Name;

        var executingLog = _logger.Entries.ElementAt(0);
        executingLog.LogLevel.Should().Be(LogLevel.Information);
        executingLog.Message.Should().Be($"Executing request '{requestName}'...");

        var executedLog = _logger.Entries.ElementAt(1);
        executedLog.LogLevel.Should().Be(LogLevel.Information);
        executedLog.Message.Should().StartWith($"Executed request '{requestName}'. Elapsed time:");
    }
}
