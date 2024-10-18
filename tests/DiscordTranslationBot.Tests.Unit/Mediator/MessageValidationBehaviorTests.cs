using DiscordTranslationBot.Mediator;
using Mediator;
using System.ComponentModel.DataAnnotations;

namespace DiscordTranslationBot.Tests.Unit.Mediator;

public sealed class MessageValidationBehaviorTests
{
    private readonly MessageValidationBehavior<IMessage, bool> _sut;

    public MessageValidationBehaviorTests()
    {
        _sut = new MessageValidationBehavior<IMessage, bool>();
    }

    [Fact]
    public async Task Handle_ValidMessage_NotValidatable_Success()
    {
        // Arrange
        var message = Substitute.For<IMessage>();

        // Act & Assert
        await _sut
            .Awaiting(x => x.Handle(message, (_, _) => ValueTask.FromResult(true), CancellationToken.None))
            .Should()
            .NotThrowAsync();
    }

    [Fact]
    public async Task Handle_ValidMessage_Success()
    {
        // Arrange
        var message = new MessageFake
        {
            Name = "Test",
            Value = 1
        };

        // Act & Assert
        await _sut
            .Awaiting(x => x.Handle(message, (_, _) => ValueTask.FromResult(true), CancellationToken.None))
            .Should()
            .NotThrowAsync();
    }

    [Fact]
    public async Task Handle_InvalidMessage_Throws()
    {
        // Arrange
        var message = new MessageFake
        {
            Name = null,
            Value = 0
        };

        // Act & Assert
        await _sut
            .Awaiting(x => x.Handle(message, (_, _) => ValueTask.FromResult(true), CancellationToken.None))
            .Should()
            .ThrowAsync<MessageValidationException>()
            .Where(
                x => x.ValidationResults.Any(y => y.MemberNames.Contains(nameof(message.Name)))
                     && x.ValidationResults.Any(y => y.MemberNames.Contains(nameof(message.Value))));
    }

    private sealed class MessageFake : IMessage
    {
        [Required]
        public string? Name { get; init; }

        [Range(0, int.MaxValue, MinimumIsExclusive = true)]
        public required int Value { get; init; }
    }
}
