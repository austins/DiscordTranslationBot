using DiscordTranslationBot.Mediator;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace DiscordTranslationBot.Tests.Mediator;

public sealed class ValidationBehaviorTests
{
    private readonly IRequest _request;

    private readonly ValidationBehavior<IRequest, Unit> _sut;
    private readonly IValidator<IRequest> _validator;

    public ValidationBehaviorTests()
    {
        _request = Substitute.For<IRequest>();
        _validator = Substitute.For<IValidator<IRequest>>();

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IValidator<IRequest>)).Returns(_validator);

        _sut = new ValidationBehavior<IRequest, Unit>(serviceProvider);
    }

    [Test]
    public async Task Handle_ValidRequest_Success()
    {
        // Arrange
        _validator.ValidateAsync(
                Arg.Is<IValidationContext>(x => x.InstanceToValidate == _request),
                Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        // Act & Assert
        await _sut.Invoking(x => x.Handle(_request, () => Unit.Task, CancellationToken.None)).Should().NotThrowAsync();

        await _validator.Received(1).ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_InvalidRequest_Throws()
    {
        // Arrange
        _validator.ValidateAsync(
                Arg.Is<IValidationContext>(x => x.InstanceToValidate == _request),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new ValidationException(new[] { new ValidationFailure("test", "test") }));

        // Act & Assert
        await _sut.Invoking(x => x.Handle(_request, () => Unit.Task, CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>();

        await _validator.Received(1)
            .ValidateAsync(
                Arg.Is<IValidationContext>(x => x.InstanceToValidate == _request),
                Arg.Any<CancellationToken>());
    }
}
