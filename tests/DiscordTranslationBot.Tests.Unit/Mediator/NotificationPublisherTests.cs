using DiscordTranslationBot.Mediator;
using Mediator;
using System.ComponentModel.DataAnnotations;

namespace DiscordTranslationBot.Tests.Unit.Mediator;

public sealed class NotificationPublisherTests
{
    private readonly LoggerFake<NotificationPublisher> _logger;
    private readonly NotificationPublisher _sut;

    public NotificationPublisherTests()
    {
        _logger = new LoggerFake<NotificationPublisher>();
        _sut = new NotificationPublisher(_logger);
    }

    [Fact]
    public async Task Publish_ValidNotification_Success()
    {
        // Arrange
        var handlers = new NotificationHandlers<NotificationFake>([new NotificationHandlerFake()], false);

        var notification = new NotificationFake { Value = 1 };

        // Act + Assert
        await _sut.Awaiting(x => x.Publish(handlers, notification, CancellationToken.None)).Should().NotThrowAsync();

        var notificationName = notification.GetType().Name;

        var publishingLog = _logger.Entries[0];
        publishingLog.LogLevel.Should().Be(LogLevel.Information);
        publishingLog.Message.Should().Be($"Publishing notification '{notificationName}'...");

        var handlersExecutedLog = _logger.Entries[1];
        handlersExecutedLog.LogLevel.Should().Be(LogLevel.Information);
        handlersExecutedLog
            .Message
            .Should()
            .StartWith($"Executed notification handlers for '{notificationName}'. Elapsed time:");
    }

    [Fact]
    public async Task Publish_InvalidNotification_Throws()
    {
        // Arrange
        var handlers = new NotificationHandlers<NotificationFake>([new NotificationHandlerFake()], false);

        var notification = new NotificationFake { Value = null };

        // Act + Assert
        await _sut
            .Awaiting(x => x.Publish(handlers, notification, CancellationToken.None))
            .Should()
            .ThrowAsync<MessageValidationException>();
    }

    private sealed class NotificationFake : INotification
    {
        [Required]
        public int? Value { get; init; }
    }

    private sealed class NotificationHandlerFake : INotificationHandler<NotificationFake>
    {
        public ValueTask Handle(NotificationFake notification, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
    }
}
