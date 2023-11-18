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

        _sut = new ValidationBehavior<IRequest, Unit>(new[] { _validator });
    }

    [Fact]
    public async Task Handle_ValidRequest_Success()
    {
        // Arrange
        _validator
            .ValidateAsync(Arg.Is<IRequest>(x => x == _request), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        // Act & Assert
        await _sut.Invoking(x => x.Handle(_request, () => Unit.Task, CancellationToken.None))
            .Should()
            .NotThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_InvalidRequest_Throws()
    {
        // Arrange
        _validator
            .ValidateAsync(Arg.Is<IRequest>(x => x == _request), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { new ValidationFailure("test", "test") }));

        // Act & Assert
        await _sut.Invoking(x => x.Handle(_request, () => Unit.Task, CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>();
    }
}
