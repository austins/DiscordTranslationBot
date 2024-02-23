using DiscordTranslationBot.Mediator;
using MediatR;

namespace DiscordTranslationBot.Tests.Mediator;

public sealed class NotificationPublisherTests
{
    private readonly LoggerFake<NotificationPublisher> _logger;
    private readonly NotificationPublisher _sut;

    public NotificationPublisherTests()
    {
        _logger = new LoggerFake<NotificationPublisher>();
        _sut = new NotificationPublisher(_logger);
    }

    [Test]
    public async Task Publish_SuccessAndLogs()
    {
        // Arrange
        var notification = Substitute.For<INotification>();
        var notificationName = notification.GetType().Name;

        var handler1 = Substitute.For<INotificationHandler<INotification>>();
        var handler2 = Substitute.For<INotificationHandler<INotification>>();
        var expectedHandlerName = handler1.GetType().Name;

        var expectedException = new InvalidOperationException();

        var handlers = new List<NotificationHandlerExecutor>
        {
            new(handler1, (_, _) => Task.CompletedTask),
            new(handler2, (_, _) => throw expectedException)
        };

        // Act & Assert
        await _sut.Invoking(x => x.Publish(handlers, notification, CancellationToken.None)).Should().NotThrowAsync();

        _logger.Entries.Should()
            .HaveCount(4)
            .And.Contain(
                x => x.LogLevel == LogLevel.Information
                    && x.Message
                    == $"Executing notification handler '{expectedHandlerName}' for '{notificationName}'...")
            .And.ContainSingle(
                x => x.LogLevel == LogLevel.Information
                    && x.Message.StartsWith(
                        $"Executed notification handler '{expectedHandlerName}' for '{notificationName}'. Elapsed time:"))
            .And.ContainSingle(
                x => x.LogLevel == LogLevel.Error
                    && x.Exception!.GetType() == expectedException.GetType()
                    && x.Message == "An exception has occurred in a notification handler.");
    }
}
