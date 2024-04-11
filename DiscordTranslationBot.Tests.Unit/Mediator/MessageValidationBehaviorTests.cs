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

    [Fact]
    public async Task Handle_ValidMessage_Success()
    {
        // Arrange
        _validator
            .ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        // Act & Assert
        await _sut
            .Awaiting(x => x.Handle(_message, (_, _) => ValueTask.FromResult(true), CancellationToken.None))
            .Should()
            .NotThrowAsync();

        await _validator.Received(1).ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidMessage_NoValidator_Success()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IValidator<IMessage>)).Returns(null);

        var sut = new MessageValidationBehavior<IMessage, bool>(serviceProvider);

        // Act & Assert
        await sut
            .Awaiting(x => x.Handle(_message, (_, _) => ValueTask.FromResult(true), CancellationToken.None))
            .Should()
            .NotThrowAsync();

        await _validator.DidNotReceive().ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidMessage_Throws()
    {
        // Arrange
        _validator
            .ValidateAsync(
                Arg.Is<IValidationContext>(x => x.InstanceToValidate == _message),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new ValidationException(new[] { new ValidationFailure("test", "test") }));

        // Act & Assert
        await _sut
            .Awaiting(x => x.Handle(_message, (_, _) => ValueTask.FromResult(true), CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>();

        await _validator
            .Received(1)
            .ValidateAsync(
                Arg.Is<IValidationContext>(x => x.InstanceToValidate == _message),
                Arg.Any<CancellationToken>());
    }
}
