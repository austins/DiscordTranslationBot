using DiscordTranslationBot.Mediator;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using ValidationException = FluentValidation.ValidationException;

namespace DiscordTranslationBot.Tests.Unit.Mediator;

public sealed class RequestValidationBehaviorTests
{
    private readonly IRequest _request;
    private readonly RequestValidationBehavior<IRequest, bool> _sut;
    private readonly IValidator<IRequest> _validator;

    public RequestValidationBehaviorTests()
    {
        _request = Substitute.For<IRequest>();
        _validator = Substitute.For<IValidator<IRequest>>();

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IValidator<IRequest>)).Returns(_validator);

        _sut = new RequestValidationBehavior<IRequest, bool>(serviceProvider);
    }

    [Fact]
    public async Task Handle_ValidRequest_Success()
    {
        // Arrange
        _validator
            .ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        // Act & Assert
        await _sut
            .Invoking(x => x.Handle(_request, () => Task.FromResult(true), CancellationToken.None))
            .Should()
            .NotThrowAsync();

        await _validator.Received(1).ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_NoValidator_Success()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IValidator<IRequest>)).Returns(null);

        var sut = new RequestValidationBehavior<IRequest, bool>(serviceProvider);

        // Act & Assert
        await sut
            .Invoking(x => x.Handle(_request, () => Task.FromResult(true), CancellationToken.None))
            .Should()
            .NotThrowAsync();

        await _validator.DidNotReceive().ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidRequest_Throws()
    {
        // Arrange
        _validator
            .ValidateAsync(
                Arg.Is<IValidationContext>(x => x.InstanceToValidate == _request),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new ValidationException(new[] { new ValidationFailure("test", "test") }));

        // Act & Assert
        await _sut
            .Invoking(x => x.Handle(_request, () => Task.FromResult(true), CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>();

        await _validator
            .Received(1)
            .ValidateAsync(
                Arg.Is<IValidationContext>(x => x.InstanceToValidate == _request),
                Arg.Any<CancellationToken>());
    }
}
