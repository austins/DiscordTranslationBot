using DiscordTranslationBot.Mediator;
using FluentValidation;
using FluentValidation.Results;
using Mediator;

namespace DiscordTranslationBot.Tests.Unit.Mediator;

public sealed class NotificationPublisherTests
{
    private readonly INotificationHandler<INotification> _handler;
    private readonly INotification _notification;
    private readonly IValidator<INotification> _validator;
    private readonly LoggerFake<NotificationPublisher> _logger;
    private readonly NotificationPublisher _sut;

    public NotificationPublisherTests()
    {
        _handler = Substitute.For<INotificationHandler<INotification>>();
        _notification = Substitute.For<INotification>();
        _validator = Substitute.For<IValidator<INotification>>();

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IValidator<INotification>)).Returns(_validator);

        _logger = new LoggerFake<NotificationPublisher>();

        _sut = new NotificationPublisher(serviceProvider, _logger);
    }

    [Test]
    public async Task Publish_Notification_NoValidator_Success(CancellationToken cancellationToken)
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IValidator<INotification>)).Returns(null);

        var sut = new NotificationPublisher(serviceProvider, _logger);

        // Act + Assert
        await Should.NotThrowAsync(
            async () => await sut.Publish(
                new NotificationHandlers<INotification>([_handler], false),
                _notification,
                cancellationToken));

        var notificationName = _notification.GetType().Name;

        var publishingLog = _logger.Entries[0];
        publishingLog.LogLevel.ShouldBe(LogLevel.Information);
        publishingLog.Message.ShouldBe($"Publishing notification '{notificationName}'...");

        var handlersExecutedLog = _logger.Entries[1];
        handlersExecutedLog.LogLevel.ShouldBe(LogLevel.Information);
        handlersExecutedLog.Message.ShouldStartWith(
            $"Executed notification handler(s) for '{notificationName}'. Elapsed time:");
    }

    [Test]
    public async Task Publish_ValidNotification_Success(CancellationToken cancellationToken)
    {
        // Arrange
        _validator.ValidateAsync(Arg.Any<IValidationContext>(), cancellationToken).Returns(new ValidationResult());

        // Act + Assert
        await Should.NotThrowAsync(
            async () => await _sut.Publish(
                new NotificationHandlers<INotification>([_handler], false),
                _notification,
                cancellationToken));

        var notificationName = _notification.GetType().Name;

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
        _validator
            .ValidateAsync(Arg.Is<IValidationContext>(x => x.InstanceToValidate == _notification), cancellationToken)
            .ThrowsAsync(new ValidationException([new ValidationFailure("test", "test")]));

        // Act + Assert
        await Should.ThrowAsync<ValidationException>(
            async () => await _sut.Publish(
                new NotificationHandlers<INotification>([_handler], false),
                _notification,
                cancellationToken));

        await _validator
            .Received(1)
            .ValidateAsync(Arg.Is<IValidationContext>(x => x.InstanceToValidate == _notification), cancellationToken);
    }
}
