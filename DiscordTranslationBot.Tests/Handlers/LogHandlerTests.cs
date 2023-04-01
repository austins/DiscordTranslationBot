using Discord;
using DiscordTranslationBot.Commands;
using DiscordTranslationBot.Handlers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DiscordTranslationBot.Tests.Handlers;

public sealed class LogHandlerTests
{
    private readonly Mock<ILogger<LogHandler>> _logger;
    private readonly LogHandler _sut;

    public LogHandlerTests()
    {
        _logger = new Mock<ILogger<LogHandler>>(MockBehavior.Strict);
        _sut = new LogHandler(_logger.Object);
    }

    [Theory]
    [InlineData(LogSeverity.Debug, LogLevel.Debug)]
    [InlineData(LogSeverity.Verbose, LogLevel.Trace)]
    [InlineData(LogSeverity.Info, LogLevel.Information)]
    [InlineData(LogSeverity.Warning, LogLevel.Warning)]
    [InlineData(LogSeverity.Error, LogLevel.Error)]
    [InlineData(LogSeverity.Critical, LogLevel.Critical)]
    public async Task Handle_LogDiscordMessage_Success(LogSeverity severity, LogLevel expectedLevel)
    {
        // Arrange
        _logger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        _logger.Setup(
            x =>
                x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                )
        );

        var command = new LogDiscordMessage
        {
            LogMessage = new LogMessage(
                severity,
                "source",
                "message",
#pragma warning disable CA2201
                new Exception("test")
#pragma warning restore CA2201
            )
        };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _logger.Verify(
            x =>
                x.Log(
                    It.Is<LogLevel>(logLevel => logLevel == expectedLevel),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.Is<Exception?>(ex => ex == command.LogMessage.Exception),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }
}
