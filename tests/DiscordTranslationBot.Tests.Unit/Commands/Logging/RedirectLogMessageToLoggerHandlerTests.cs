using Discord;
using DiscordTranslationBot.Commands.Logging;

namespace DiscordTranslationBot.Tests.Unit.Commands.Logging;

public sealed class RedirectLogMessageToLoggerHandlerTests
{
    private readonly LoggerFake<RedirectLogMessageToLoggerHandler> _logger;
    private readonly RedirectLogMessageToLoggerHandler _sut;

    public RedirectLogMessageToLoggerHandlerTests()
    {
        _logger = new LoggerFake<RedirectLogMessageToLoggerHandler>(true);
        _sut = new RedirectLogMessageToLoggerHandler(_logger);
    }

    [Theory]
    [InlineData(LogSeverity.Debug, LogLevel.Trace)]
    [InlineData(LogSeverity.Verbose, LogLevel.Debug)]
    [InlineData(LogSeverity.Info, LogLevel.Information)]
    [InlineData(LogSeverity.Warning, LogLevel.Warning)]
    [InlineData(LogSeverity.Error, LogLevel.Error)]
    [InlineData(LogSeverity.Critical, LogLevel.Critical)]
    public async Task Handle_RedirectLogMessageToLogger_Success(LogSeverity severity, LogLevel expectedLevel)
    {
        // Arrange
        var command = new RedirectLogMessageToLogger
        {
            LogMessage = new LogMessage(severity, "source1", "message1", new InvalidOperationException("test"))
        };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        var logEntry = _logger.Entries[0];
        logEntry.LogLevel.Should().Be(expectedLevel);
        logEntry.Message.Should().Be($"Discord {command.LogMessage.Source}: {command.LogMessage.Message}");
        logEntry.Exception.Should().Be(command.LogMessage.Exception);
    }
}
