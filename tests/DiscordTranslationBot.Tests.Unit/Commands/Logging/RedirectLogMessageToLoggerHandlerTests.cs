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

    [Test]
    [Arguments(LogSeverity.Debug, LogLevel.Trace)]
    [Arguments(LogSeverity.Verbose, LogLevel.Debug)]
    [Arguments(LogSeverity.Info, LogLevel.Information)]
    [Arguments(LogSeverity.Warning, LogLevel.Warning)]
    [Arguments(LogSeverity.Error, LogLevel.Error)]
    [Arguments(LogSeverity.Critical, LogLevel.Critical)]
    public async Task Handle_RedirectLogMessageToLogger_Success(
        LogSeverity severity,
        LogLevel expectedLevel,
        CancellationToken cancellationToken)
    {
        // Arrange
        var command = new RedirectLogMessageToLogger
        {
            LogMessage = new LogMessage(severity, "source1", "message1", new InvalidOperationException("test"))
        };

        // Act
        await _sut.Handle(command, cancellationToken);

        // Assert
        var logEntry = _logger.Entries[0];
        logEntry.LogLevel.ShouldBe(expectedLevel);
        logEntry.Message.ShouldBe($"Discord: [{command.LogMessage.Source}] {command.LogMessage.Message}");
        logEntry.Exception.ShouldBe(command.LogMessage.Exception);
    }

    [Test]
    public async Task Handle_RedirectLogMessageToLogger_GatewayReconnectException_ChangesLogLevel(
        CancellationToken cancellationToken)
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
        await _sut.Handle(command, cancellationToken);

        // Assert
        var logEntry = _logger.Entries[0];
        logEntry.LogLevel.ShouldBe(expectedLevel);
        logEntry.Message.ShouldBe($"Discord: [{command.LogMessage.Source}] {command.LogMessage.Message}");
        logEntry.Exception.ShouldBe(command.LogMessage.Exception);
    }

    [Test]
    public async Task Handle_RedirectLogMessageToLogger_NullMessage(CancellationToken cancellationToken)
    {
        // Arrange
        var command = new RedirectLogMessageToLogger { LogMessage = new LogMessage(LogSeverity.Info, "source1", null) };

        // Act
        await _sut.Handle(command, cancellationToken);

        // Assert
        var logEntry = _logger.Entries[0];
        logEntry.LogLevel.ShouldBe(LogLevel.Information);
        logEntry.Message.ShouldBe($"Discord: [{command.LogMessage.Source}] ");
        logEntry.Exception.ShouldBe(command.LogMessage.Exception);
    }
}
