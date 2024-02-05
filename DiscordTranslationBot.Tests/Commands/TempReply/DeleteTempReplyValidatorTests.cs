using Discord;
using DiscordTranslationBot.Commands.TempReply;
using DiscordTranslationBot.Models.Discord;
using FluentValidation.TestHelper;

namespace DiscordTranslationBot.Tests.Commands.TempReply;

public sealed class DeleteTempReplyValidatorTests
{
    private readonly DeleteTempReplyValidator _sut;

    public DeleteTempReplyValidatorTests()
    {
        _sut = new DeleteTempReplyValidator();
    }

    [Test]
    public async Task Valid_ValidatesWithoutErrors()
    {
        // Arrange
        var command = new DeleteTempReply
        {
            Reply = Substitute.For<IMessage>(),
            Reaction = new Reaction
            {
                UserId = 1,
                Emote = Substitute.For<IEmote>()
            },
            SourceMessage = Substitute.For<IMessage>()
        };

        // Act
        var result = await _sut.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task Invalid_Reply_HasValidationErrors()
    {
        // Arrange
        var command = new DeleteTempReply
        {
            Reply = null!,
            Reaction = null,
            SourceMessage = null!
        };

        // Act
        var result = await _sut.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Reply);
        result.ShouldNotHaveValidationErrorFor(x => x.Reaction);
        result.ShouldHaveValidationErrorFor(x => x.SourceMessage);
    }
}
