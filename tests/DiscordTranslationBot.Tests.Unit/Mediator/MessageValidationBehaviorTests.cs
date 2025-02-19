using DiscordTranslationBot.Mediator;
using FluentValidation;
using FluentValidation.Results;
using Mediator;

namespace DiscordTranslationBot.Tests.Unit.Mediator;

public sealed class MessageValidationBehaviorTests
{
    private readonly IMessage _message;
    private readonly MessageValidationBehavior<IMessage, bool> _sut;
    private readonly IValidator<IMessage> _validator;

    public MessageValidationBehaviorTests()
    {
        _message = Substitute.For<IMessage>();
        _validator = Substitute.For<IValidator<IMessage>>();

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IValidator<IMessage>)).Returns(_validator);

        _sut = new MessageValidationBehavior<IMessage, bool>(serviceProvider);
    }

    [Test]
    public async Task Handle_ValidMessage_NoValidator_Success(CancellationToken cancellationToken)
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IValidator<IMessage>)).Returns(null);

        var sut = new MessageValidationBehavior<IMessage, bool>(serviceProvider);

        // Act & Assert
        await Should.NotThrowAsync(
            async () => await sut.Handle(_message, (_, _) => ValueTask.FromResult(true), cancellationToken));

        await _validator.DidNotReceive().ValidateAsync(Arg.Any<IValidationContext>(), cancellationToken);
    }

    [Test]
    public async Task Handle_ValidMessage_Success(CancellationToken cancellationToken)
    {
        // Arrange
        _validator.ValidateAsync(Arg.Any<IValidationContext>(), cancellationToken).Returns(new ValidationResult());

        // Act & Assert
        await Should.NotThrowAsync(
            async () => await _sut.Handle(_message, (_, _) => ValueTask.FromResult(true), cancellationToken));

        await _validator.Received(1).ValidateAsync(Arg.Any<IValidationContext>(), cancellationToken);
    }

    [Test]
    public async Task Handle_InvalidMessage_Throws(CancellationToken cancellationToken)
    {
        // Arrange
        _validator
            .ValidateAsync(Arg.Is<IValidationContext>(x => x.InstanceToValidate == _message), cancellationToken)
            .ThrowsAsync(new ValidationException([new ValidationFailure("test", "test")]));

        // Act & Assert
        await Should.ThrowAsync<ValidationException>(
            async () => await _sut.Handle(_message, (_, _) => ValueTask.FromResult(true), cancellationToken));

        await _validator
            .Received(1)
            .ValidateAsync(Arg.Is<IValidationContext>(x => x.InstanceToValidate == _message), cancellationToken);
    }
}
