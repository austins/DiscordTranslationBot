using DiscordTranslationBot.Mediator;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using ValidationException = FluentValidation.ValidationException;

namespace DiscordTranslationBot.Tests.Unit.Mediator;

public sealed class RequestValidationBehaviorTests
{
    private readonly IRequest _request;
    private readonly RequestValidationBehavior<IRequest, MediatR.Unit> _sut;
    private readonly IValidator<IRequest> _validator;

    public RequestValidationBehaviorTests()
    {
        _request = Substitute.For<IRequest>();
        _validator = Substitute.For<IValidator<IRequest>>();

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IValidator<IRequest>)).Returns(_validator);

        _sut = new RequestValidationBehavior<IRequest, MediatR.Unit>(serviceProvider);
    }

    [Fact]
    public async Task Handle_ValidRequest_Success()
    {
        // Arrange
        _validator
            .ValidateAsync(
                Arg.Is<IValidationContext>(x => x.InstanceToValidate == _request),
                Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        // Act & Assert
        await _sut
            .Invoking(x => x.Handle(_request, () => MediatR.Unit.Task, CancellationToken.None))
            .Should()
            .NotThrowAsync();

        await _validator
            .Received(1)
            .ValidateAsync(
                Arg.Is<IValidationContext>(x => x.InstanceToValidate == _request),
                Arg.Any<CancellationToken>());
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
            .Invoking(x => x.Handle(_request, () => MediatR.Unit.Task, CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>();

        await _validator
            .Received(1)
            .ValidateAsync(
                Arg.Is<IValidationContext>(x => x.InstanceToValidate == _request),
                Arg.Any<CancellationToken>());
    }
}
