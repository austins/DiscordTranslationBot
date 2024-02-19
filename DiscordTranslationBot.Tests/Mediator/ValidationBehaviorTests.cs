using System.ComponentModel.DataAnnotations;
using DiscordTranslationBot.Mediator;
using MediatR;

namespace DiscordTranslationBot.Tests.Mediator;

public sealed class ValidationBehaviorTests
{
    private readonly ValidationBehavior<IRequest, Unit> _sut;

    public ValidationBehaviorTests()
    {
        _sut = new ValidationBehavior<IRequest, Unit>();
    }

    [Test]
    public async Task Handle_ValidRequest_Success()
    {
        // Arrange
        var request = new RequestFake
        {
            Name = "Test",
            Value = 1
        };

        // Act & Assert
        await _sut.Invoking(x => x.Handle(request, () => Unit.Task, CancellationToken.None)).Should().NotThrowAsync();
    }

    [Test]
    public async Task Handle_InvalidRequest_Throws()
    {
        // Arrange
        var request = new RequestFake
        {
            Name = null,
            Value = 0
        };

        // Act & Assert
        await _sut.Invoking(x => x.Handle(request, () => Unit.Task, CancellationToken.None))
            .Should()
            .ThrowAsync<RequestValidationException>()
            .Where(
                x => x.Results.Any(y => y.MemberNames.Contains(nameof(RequestFake.Name)))
                    && x.Results.Any(y => y.MemberNames.Contains(nameof(RequestFake.Value))));
    }

    private sealed class RequestFake : IRequest
    {
        [Required]
        public string? Name { get; init; }

        [System.ComponentModel.DataAnnotations.Range(0, int.MaxValue, MinimumIsExclusive = true)]
        public required int Value { get; init; }
    }
}
