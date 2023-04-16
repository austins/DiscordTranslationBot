using Discord;
using DiscordTranslationBot.Commands;
using DiscordTranslationBot.Handlers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace DiscordTranslationBot.Tests.Handlers;

public sealed class LogHandlerTests
{
    private readonly ILogger<LogHandler> _logger;
    private readonly LogHandler _sut;

    public LogHandlerTests()
    {
        _logger = Substitute.For<ILogger<LogHandler>>();
        _sut = new LogHandler(_logger);
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
        _logger.IsEnabled(expectedLevel).Returns(true);

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
        var logArgs = _logger.ReceivedCalls()
            .Single(call => call.GetMethodInfo().Name == nameof(ILogger.Log))
            .GetArguments();

        logArgs[0].Should().Be(expectedLevel);

        var logMessages = (IReadOnlyList<KeyValuePair<string, object>>)logArgs[2]!;
        logMessages[0].Value.Should().Be(command.LogMessage.Source);
        logMessages[1].Value.Should().Be(command.LogMessage.Message);

        logArgs[3].Should().Be(command.LogMessage.Exception);
    }
}
