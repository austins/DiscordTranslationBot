using Discord;
using Discord.WebSocket;
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
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var logEntry = _logger.Entries[0];
        logEntry.LogLevel.ShouldBe(expectedLevel);
        logEntry.Message.ShouldBe($"Discord: [{command.LogMessage.Source}] {command.LogMessage.Message}");
        logEntry.Exception.ShouldBe(command.LogMessage.Exception);
    }

    [Fact]
    public async Task Handle_RedirectLogMessageToLogger_GatewayReconnectException_ChangesLogLevel()
    {
        // Arrange
        var command = new RedirectLogMessageToLogger
        {
            LogMessage = new LogMessage(
                LogSeverity.Error,
                "source1",
                "message1",
                new GatewayReconnectException("test"))
        };

        const LogLevel expectedLevel = LogLevel.Information;

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var logEntry = _logger.Entries[0];
        logEntry.LogLevel.ShouldBe(expectedLevel);
        logEntry.Message.ShouldBe($"Discord: [{command.LogMessage.Source}] {command.LogMessage.Message}");
        logEntry.Exception.ShouldBe(command.LogMessage.Exception);
    }

    [Fact]
    public async Task Handle_RedirectLogMessageToLogger_NullMessage()
    {
        // Arrange
        var command = new RedirectLogMessageToLogger { LogMessage = new LogMessage(LogSeverity.Info, "source1", null) };

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var logEntry = _logger.Entries[0];
        logEntry.LogLevel.ShouldBe(LogLevel.Information);
        logEntry.Message.ShouldBe($"Discord: [{command.LogMessage.Source}] ");
        logEntry.Exception.ShouldBe(command.LogMessage.Exception);
    }
}
