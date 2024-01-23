using Discord;
using DiscordTranslationBot.Handlers;
using DiscordTranslationBot.Notifications;

namespace DiscordTranslationBot.Tests.Handlers;

public sealed class RedirectLogMessageToLoggerHandlerTests : TestBase
{
    private readonly ICacheLogger<RedirectLogMessageToLoggerHandler> _logger;
    private readonly RedirectLogMessageToLoggerHandler _sut;

    public RedirectLogMessageToLoggerHandlerTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
        _logger = CreateLogger<RedirectLogMessageToLoggerHandler>(LogLevel.Trace);
        _sut = new RedirectLogMessageToLoggerHandler(_logger);
    }

    [Theory]
    [InlineData(LogSeverity.Debug, LogLevel.Trace)]
    [InlineData(LogSeverity.Verbose, LogLevel.Debug)]
    [InlineData(LogSeverity.Info, LogLevel.Information)]
    [InlineData(LogSeverity.Warning, LogLevel.Warning)]
    [InlineData(LogSeverity.Error, LogLevel.Error)]
    [InlineData(LogSeverity.Critical, LogLevel.Critical)]
    public async Task Handle_LogNotification_Success(LogSeverity severity, LogLevel expectedLevel)
    {
        // Arrange
        var request = new LogNotification
        {
            LogMessage = new LogMessage(severity, "source1", "message1", new InvalidOperationException("test"))
        };

        // Act
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        _logger.Count.Should().Be(1);
        _logger.Last!.LogLevel.Should().Be(expectedLevel);
        _logger.Last.Exception.Should().Be(request.LogMessage.Exception);
        _logger.Last.Message.Should().Be($"Discord {request.LogMessage.Source}: {request.LogMessage.Message}");
    }
}
