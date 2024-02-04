using Discord;
using DiscordTranslationBot.Handlers;
using DiscordTranslationBot.Notifications;

namespace DiscordTranslationBot.Tests.Handlers;

[TestClass]
public sealed class RedirectLogMessageToLoggerHandlerTests
{
    private readonly ILogger<RedirectLogMessageToLoggerHandler> _logger;
    private readonly RedirectLogMessageToLoggerHandler _sut;

    public RedirectLogMessageToLoggerHandlerTests()
    {
        _logger = Substitute.For<ILogger<RedirectLogMessageToLoggerHandler>>();
        _sut = new RedirectLogMessageToLoggerHandler(_logger);
    }

    [DataTestMethod]
    [DataRow(LogSeverity.Debug, LogLevel.Trace)]
    [DataRow(LogSeverity.Verbose, LogLevel.Debug)]
    [DataRow(LogSeverity.Info, LogLevel.Information)]
    [DataRow(LogSeverity.Warning, LogLevel.Warning)]
    [DataRow(LogSeverity.Error, LogLevel.Error)]
    [DataRow(LogSeverity.Critical, LogLevel.Critical)]
    public async Task Handle_LogNotification_Success(LogSeverity severity, LogLevel expectedLevel)
    {
        // Arrange
        _logger.IsEnabled(expectedLevel).Returns(true);

        var request = new LogNotification
        {
            LogMessage = new LogMessage(severity, "source1", "message1", new InvalidOperationException("test"))
        };

        // Act
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        var logArgs = _logger.ReceivedCalls()
            .Single(call => call.GetMethodInfo().Name == nameof(ILogger.Log))
            .GetArguments();

        logArgs[0].Should().Be(expectedLevel);

        var logMessages = (IReadOnlyList<KeyValuePair<string, object>>)logArgs[2]!;
        logMessages[0].Value.Should().Be(request.LogMessage.Source);
        logMessages[1].Value.Should().Be(request.LogMessage.Message);

        logArgs[3].Should().Be(request.LogMessage.Exception);
    }
}
