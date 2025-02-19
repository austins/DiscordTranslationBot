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

    [Test]
    public async Task Handle_ValidMessage_NotValidatable_Success(CancellationToken cancellationToken)
    {
        // Arrange
        var message = Substitute.For<IMessage>();

        // Act & Assert
        await _sut
            .Handle(message, (_, _) => ValueTask.FromResult(true), cancellationToken)
            .AsTask()
            .ShouldNotThrowAsync();
    }

    [Test]
    public async Task Handle_ValidMessage_Success(CancellationToken cancellationToken)
    {
        // Arrange
        var message = new MessageFake
        {
            Name = "Test",
            Value = 1
        };

        // Act & Assert
        await _sut
            .Handle(message, (_, _) => ValueTask.FromResult(true), cancellationToken)
            .AsTask()
            .ShouldNotThrowAsync();
    }

    [Test]
    public async Task Handle_InvalidMessage_Throws(CancellationToken cancellationToken)
    {
        // Arrange
        var message = new MessageFake
        {
            Name = null,
            Value = 0
        };

        // Act & Assert
        var exception = await Should.ThrowAsync<MessageValidationException>(
            async () => await _sut.Handle(message, (_, _) => ValueTask.FromResult(true), cancellationToken));

        exception.ValidationResults.Count.ShouldBe(2);
        exception.ValidationResults.ShouldContain(x => x.MemberNames.Contains(nameof(message.Name)), 1);
        exception.ValidationResults.ShouldContain(x => x.MemberNames.Contains(nameof(message.Value)), 1);
    }

    private sealed class MessageFake : IMessage
    {
        [Required]
        public string? Name { get; init; }

        [Range(0, int.MaxValue, MinimumIsExclusive = true)]
        public required int Value { get; init; }
    }
}
