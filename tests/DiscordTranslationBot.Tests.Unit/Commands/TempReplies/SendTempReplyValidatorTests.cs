using System.Globalization;
using Discord;
using DiscordTranslationBot.Commands.TempReplies;
using DiscordTranslationBot.Discord.Models;
using FluentValidation.TestHelper;

namespace DiscordTranslationBot.Tests.Unit.Commands.TempReplies;

public sealed class SendTempReplyValidatorTests
{
    private readonly SendTempReplyValidator _sut = new();

    [Theory]
    [InlineData("00:00:01")]
    [InlineData("00:00:10")]
    [InlineData("00:00:30.678")]
    [InlineData("00:01:00")]
    public async Task Valid_ValidatesWithoutErrors(string deletionDelay)
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
            DeletionDelay = TimeSpan.Parse(deletionDelay, CultureInfo.InvariantCulture)
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

    [Theory]
    [InlineData("00:00:00")] // 0 seconds
    [InlineData("00:00:00.5")] // 0.5 seconds
    [InlineData("00:01:00.1")] // 1 minute 0.1 seconds
    public async Task Invalid_DeletionDelay_HasValidationError(string deletionDelay)
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
            DeletionDelay = TimeSpan.Parse(deletionDelay, CultureInfo.InvariantCulture)
        };

        // Act
        var result = await _sut.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DeletionDelay);
    }
}
