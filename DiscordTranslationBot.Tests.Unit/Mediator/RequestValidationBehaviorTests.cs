using System.ComponentModel.DataAnnotations;
using DiscordTranslationBot.Mediator;
using MediatR;

namespace DiscordTranslationBot.Tests.Unit.Mediator;

public sealed class RequestValidationBehaviorTests
{
    private readonly RequestValidationBehavior<IRequest, MediatR.Unit> _sut;

    public RequestValidationBehaviorTests()
    {
        _sut = new RequestValidationBehavior<IRequest, MediatR.Unit>();
    }

    [Fact]
    public async Task Handle_ValidRequest_Success()
    {
        // Arrange
        var request = new RequestFake
        {
            Name = "Test",
            Value = 1
        };

        // Act & Assert
        await _sut.Invoking(x => x.Handle(request, () => MediatR.Unit.Task, CancellationToken.None)).Should()
            .NotThrowAsync();
    }

    [Fact]
    public async Task Handle_InvalidRequest_Throws()
    {
        // Arrange
        var request = new RequestFake
        {
            Name = null,
            Value = 0
        };

        // Act & Assert
        await _sut.Invoking(x => x.Handle(request, () => MediatR.Unit.Task, CancellationToken.None))
            .Should()
            .ThrowAsync<RequestValidationException>()
            .Where(
                x => x.ValidationResults.Any(y => y.MemberNames.Contains(nameof(RequestFake.Name)))
                     && x.ValidationResults.Any(y => y.MemberNames.Contains(nameof(RequestFake.Value))));
    }

    private sealed class RequestFake : IRequest
    {
        [Required]
        public string? Name { get; init; }

        [Range(0, int.MaxValue, MinimumIsExclusive = true)]
        public required int Value { get; init; }
    }
}
