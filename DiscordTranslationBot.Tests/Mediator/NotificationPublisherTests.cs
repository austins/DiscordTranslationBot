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
        var notification = new NotificationFake();

        var successHandler = new SuccessNotificationHandlerFake();

        var expectedException = new InvalidOperationException();
        var failHandler = new FailNotificationHandlerFake(expectedException);

        var handlers = new List<NotificationHandlerExecutor>
        {
            new(successHandler, (_, cancellationToken) => successHandler.Handle(notification, cancellationToken)),
            new(failHandler, (_, cancellationToken) => failHandler.Handle(notification, cancellationToken))
        };

        // Act & Assert
        await _sut.Invoking(x => x.Publish(handlers, notification, CancellationToken.None)).Should().NotThrowAsync();

        _logger.Entries.Should()
            .HaveCount(4)
            .And
            .ContainSingle(
                x => x.LogLevel == LogLevel.Information
                    && x.Message
                    == $"Executing notification handler '{nameof(SuccessNotificationHandlerFake)}' for '{nameof(NotificationFake)}'...")
            .And.ContainSingle(
                x => x.LogLevel == LogLevel.Information
                    && x.Message.StartsWith(
                        $"Executed notification handler '{nameof(SuccessNotificationHandlerFake)}' for '{nameof(NotificationFake)}'. Elapsed time:"))
            .And.ContainSingle(
                x => x.LogLevel == LogLevel.Information
                    && x.Message
                    == $"Executing notification handler '{nameof(FailNotificationHandlerFake)}' for '{nameof(NotificationFake)}'...")
            .And.ContainSingle(
                x => x.LogLevel == LogLevel.Error
                    && x.Exception!.GetType() == expectedException.GetType()
                    && x.Message
                    == $"An exception has occurred in notification handler '{nameof(FailNotificationHandlerFake)}'.");
    }

    private sealed class NotificationFake : INotification
    {
    }

    private sealed class SuccessNotificationHandlerFake : INotificationHandler<NotificationFake>
    {
        public Task Handle(NotificationFake notification, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FailNotificationHandlerFake : INotificationHandler<NotificationFake>
    {
        private readonly Exception _expectedException;

        public FailNotificationHandlerFake(Exception expectedException)
        {
            _expectedException = expectedException;
        }

        public Task Handle(NotificationFake notification, CancellationToken cancellationToken)
        {
            throw _expectedException;
        }
    }
}
