using Discord;
using DiscordTranslationBot.Commands.TempReplies;
using DiscordTranslationBot.Discord.Models;
using FluentValidation.TestHelper;

namespace DiscordTranslationBot.Tests.Unit.Commands.TempReplies;

public sealed class SendTempReplyValidatorTests
{
    private readonly SendTempReplyValidator _sut = new();

    [Fact]
    public async Task Valid_ValidatesWithoutErrors()
    {
        // Arrange
        var command = new SendTempReply
        {
            Text = "test",
            ReactionInfo = new ReactionInfo
            {
                UserId = 1,
                Emote = Substitute.For<IEmote>()
            },
            SourceMessage = Substitute.For<IUserMessage>(),
            DeletionDelay = TimeSpan.FromSeconds(10)
        };

        // Act
        var result = await _sut.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Invalid_Text_HasValidationError(string? text)
    {
        // Arrange
        var command = new SendTempReply
        {
            Text = text!,
            ReactionInfo = null,
            SourceMessage = Substitute.For<IUserMessage>()
        };

        // Act
        var result = await _sut.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Text);
    }

    [Fact]
    public async Task Invalid_DeletionDelay_HasValidationError()
    {
        // Arrange
        var command = new SendTempReply
        {
            Text = "test",
            ReactionInfo = new ReactionInfo
            {
                UserId = 1,
                Emote = Substitute.For<IEmote>()
            },
            SourceMessage = Substitute.For<IUserMessage>(),
            DeletionDelay = TimeSpan.Zero
        };

        // Act
        var result = await _sut.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DeletionDelay);
    }
}
