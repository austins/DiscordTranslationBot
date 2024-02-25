using Discord;
using DiscordTranslationBot.Commands.Logging;

namespace DiscordTranslationBot.Tests.Commands.Logging;

public sealed class RedirectLogMessageToLoggerHandlerTests
{
    private readonly LoggerFake<RedirectLogMessageToLoggerHandler> _logger;
    private readonly RedirectLogMessageToLoggerHandler _sut;

    public RedirectLogMessageToLoggerHandlerTests()
    {
        _logger = new LoggerFake<RedirectLogMessageToLoggerHandler>(true);
        _sut = new RedirectLogMessageToLoggerHandler(_logger);
    }

    [TestCase(LogSeverity.Debug, LogLevel.Trace)]
    [TestCase(LogSeverity.Verbose, LogLevel.Debug)]
    [TestCase(LogSeverity.Info, LogLevel.Information)]
    [TestCase(LogSeverity.Warning, LogLevel.Warning)]
    [TestCase(LogSeverity.Error, LogLevel.Error)]
    [TestCase(LogSeverity.Critical, LogLevel.Critical)]
    public async Task Handle_RedirectLogMessageToLogger_Success(LogSeverity severity, LogLevel expectedLevel)
    {
        // Arrange
        var request = new RedirectLogMessageToLogger
        {
            LogMessage = new LogMessage(severity, "source1", "message1", new InvalidOperationException("test"))
        };

        // Act
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        var entry = _logger.Entries.First();
        entry.LogLevel.Should().Be(expectedLevel);
        entry.Message.Should().Be($"Discord {request.LogMessage.Source}: {request.LogMessage.Message}");
        entry.Exception.Should().Be(request.LogMessage.Exception);
    }
}
