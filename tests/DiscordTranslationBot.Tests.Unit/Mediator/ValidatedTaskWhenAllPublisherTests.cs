using DiscordTranslationBot.Mediator;
using Mediator;
using System.ComponentModel.DataAnnotations;

namespace DiscordTranslationBot.Tests.Unit.Mediator;

public sealed class ValidatedTaskWhenAllPublisherTests
{
    private readonly ValidatedTaskWhenAllPublisher _sut = new();

    [Fact]
    public async Task Publish_ValidNotification_Success()
    {
        // Arrange
        var handlers = new NotificationHandlers<NotificationFake>([new NotificationHandlerFake()], false);

        var notification = new NotificationFake { Value = 1 };

        // Act + Assert
        await _sut.Awaiting(x => x.Publish(handlers, notification, CancellationToken.None)).Should().NotThrowAsync();
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
