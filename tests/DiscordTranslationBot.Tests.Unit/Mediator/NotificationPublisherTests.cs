using DiscordTranslationBot.Mediator;
using FluentValidation;
using FluentValidation.Results;
using FluentValidation.TestHelper;
using Mediator;

namespace DiscordTranslationBot.Tests.Unit.Mediator;

public sealed class NotificationPublisherTests
{
    private readonly LoggerFake<NotificationPublisher> _logger;
    private readonly NotificationPublisher _sut;

    public NotificationPublisherTests()
    {
        _logger = new LoggerFake<NotificationPublisher>();

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IValidator<NotificationFake>)).Returns(new NotificationFakeValidator());

        _sut = new NotificationPublisher(serviceProvider, _logger);
    }

    [Test]
    public async Task Publish_ValidNotification_Success(CancellationToken cancellationToken)
    {
        // Arrange
        var handlers = new NotificationHandlers<NotificationFake>([new NotificationHandlerFake()], false);

        var notification = new NotificationFake { Value = 1 };

        // Act + Assert
        await Should.NotThrowAsync(async () => await _sut.Publish(handlers, notification, cancellationToken));

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
    public async Task Publish_InvalidNotification_Throws(CancellationToken cancellationToken)
    {
        // Arrange
        var handlers = new NotificationHandlers<NotificationFake>([new NotificationHandlerFake()], false);

        var notification = new NotificationFake { Value = null };

        // Act + Assert
        var exception = await Should.ThrowAsync<ValidationException>(
            async () => await _sut.Publish(handlers, notification, cancellationToken));

        var validationResult = new TestValidationResult<NotificationFake>(new ValidationResult(exception.Errors));
        validationResult.ShouldHaveValidationErrorFor(x => x.Value);
    }

    private sealed class NotificationFake : INotification
    {
        public int? Value { get; init; }
    }

    private sealed class NotificationFakeValidator : AbstractValidator<NotificationFake>
    {
#pragma warning disable S1144
        public NotificationFakeValidator()
#pragma warning restore S1144
        {
            RuleFor(x => x.Value).NotEmpty();
        }
    }

    private sealed class NotificationHandlerFake : INotificationHandler<NotificationFake>
    {
        public ValueTask Handle(NotificationFake notification, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
    }
}
