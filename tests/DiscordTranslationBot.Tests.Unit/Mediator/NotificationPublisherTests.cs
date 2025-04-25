using Discord;
using DiscordTranslationBot.Mediator;
using DiscordTranslationBot.Notifications.Events;
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

    [Test]
    public async Task Publish_ValidNotification_Success(CancellationToken cancellationToken)
    {
        // Arrange
        var handlers = new NotificationHandlers<NotificationFake>(
            [new NotificationHandlerFake<NotificationFake>()],
            false);

        var notification = new NotificationFake { Value = 1 };

        // Act + Assert
        await _sut.Publish(handlers, notification, cancellationToken).AsTask().ShouldNotThrowAsync();

        var notificationName = notification.GetType().Name;

        var publishingLog = _logger.Entries[0];
        publishingLog.LogLevel.ShouldBe(LogLevel.Information);
        publishingLog.Message.ShouldBe($"Publishing notification '{notificationName}'...");

        var handlersExecutedLog = _logger.Entries[1];
        handlersExecutedLog.LogLevel.ShouldBe(LogLevel.Information);
        handlersExecutedLog.Message.ShouldStartWith(
            $"Executed notification handler(s) for '{notificationName}'. Elapsed time:");
    }

    [Test]
    public async Task Publish_LogNotification_DoesNotLogPublishInfo(CancellationToken cancellationToken)
    {
        // Arrange
        var handlers = new NotificationHandlers<LogNotification>(
            [new NotificationHandlerFake<LogNotification>()],
            false);

        var notification = new LogNotification
        {
            LogMessage = new LogMessage(
                LogSeverity.Info,
                "source1",
                "message1",
                new InvalidOperationException("test"))
        };

        // Act + Assert
        await _sut.Publish(handlers, notification, cancellationToken).AsTask().ShouldNotThrowAsync();

        _logger.Entries.ShouldBeEmpty();
    }

    [Test]
    public async Task Publish_InvalidNotification_Throws(CancellationToken cancellationToken)
    {
        // Arrange
        var handlers = new NotificationHandlers<NotificationFake>(
            [new NotificationHandlerFake<NotificationFake>()],
            false);

        var notification = new NotificationFake { Value = null };

        // Act + Assert
        await _sut
            .Publish(handlers, notification, cancellationToken)
            .AsTask()
            .ShouldThrowAsync<MessageValidationException>();
    }

    private sealed class NotificationFake : INotification
    {
        [Required]
        public int? Value { get; init; }
    }

    private sealed class NotificationHandlerFake<T> : INotificationHandler<T>
        where T : INotification
    {
        public ValueTask Handle(T notification, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
    }
}
