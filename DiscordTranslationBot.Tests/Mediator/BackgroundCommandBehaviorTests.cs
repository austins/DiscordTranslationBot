using DiscordTranslationBot.Mediator;
using MediatR;

namespace DiscordTranslationBot.Tests.Mediator;

public sealed class BackgroundCommandBehaviorTests
{
    [Fact]
    public async Task Handle_NotABackgroundCommand_Success()
    {
        // Arrange
        var sut = new BackgroundCommandBehavior<IRequest<int>, int>(Substitute.For<ILogger<BackgroundCommandBehavior<IRequest<int>, int>>>());

        var request = Substitute.For<IRequest<int>>();

        const int expectedResult = 0;

        // Act
        var result = await sut.Handle(request, () => Task.FromResult(expectedResult), CancellationToken.None);

        // Act & Assert
        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task Handle_BackgroundCommand_InvalidDelay_Throws(int seconds)
    {
        // Arrange
        var sut = new BackgroundCommandBehavior<IRequest, Unit>(Substitute.For<ILogger<BackgroundCommandBehavior<IRequest, Unit>>>());

        var request = Substitute.For<IBackgroundCommand>();
        request.Delay.Returns(TimeSpan.FromSeconds(seconds));

        // Act & Assert
        await sut.Invoking(x => x.Handle(request, () => Unit.Task, CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>();
    }
}
