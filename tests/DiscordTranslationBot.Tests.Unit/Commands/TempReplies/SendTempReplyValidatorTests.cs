using Discord;
using DiscordTranslationBot.Commands.TempReplies;
using DiscordTranslationBot.Discord.Models;
using FluentValidation.TestHelper;
using System.Globalization;

namespace DiscordTranslationBot.Tests.Unit.Commands.TempReplies;

public sealed class SendTempReplyValidatorTests
{
    private readonly SendTempReplyValidator _sut = new();

    [Test]
    [Arguments("00:00:01")]
    [Arguments("00:00:10")]
    [Arguments("00:00:30.678")]
    [Arguments("00:01:00")]
    [Arguments("00:01:30")]
    public async Task Valid_ValidatesWithoutErrors(string deletionDelay, CancellationToken cancellationToken)
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
        var result = await _sut.TestValidateAsync(command, cancellationToken: cancellationToken);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task Invalid_SourceMessage_HasValidationError(CancellationToken cancellationToken)
    {
        // Arrange
        var command = new SendTempReply
        {
            Text = "test",
            SourceMessage = null!
        };

        // Act
        var result = await _sut.TestValidateAsync(command, cancellationToken: cancellationToken);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SourceMessage);
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments(" ")]
    public async Task Invalid_Text_HasValidationError(string? text, CancellationToken cancellationToken)
    {
        // Arrange
        var command = new SendTempReply
        {
            Text = text!,
            ReactionInfo = null,
            SourceMessage = Substitute.For<IUserMessage>()
        };

        // Act
        var result = await _sut.TestValidateAsync(command, cancellationToken: cancellationToken);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Text);
    }

    [Test]
    [Arguments("00:00:00")] // 0 seconds
    [Arguments("00:00:00.5")] // 0.5 seconds
    [Arguments("00:01:30.1")] // 1 minute 30.1 seconds
    public async Task Invalid_DeletionDelay_HasValidationError(
        string deletionDelay,
        CancellationToken cancellationToken)
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
        var result = await _sut.TestValidateAsync(command, cancellationToken: cancellationToken);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DeletionDelay);
    }
}
