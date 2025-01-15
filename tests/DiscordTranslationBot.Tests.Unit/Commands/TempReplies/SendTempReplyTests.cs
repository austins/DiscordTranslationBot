using Discord;
using DiscordTranslationBot.Commands.TempReplies;
using DiscordTranslationBot.Discord.Models;
using DiscordTranslationBot.Extensions;
using System.Globalization;

namespace DiscordTranslationBot.Tests.Unit.Commands.TempReplies;

public sealed class SendTempReplyTests
{
    [Test]
    [Arguments("00:00:01")]
    [Arguments("00:00:10")]
    [Arguments("00:00:30.678")]
    [Arguments("00:01:00")]
    [Arguments("00:01:30")]
    public void Valid_ValidatesWithoutErrors(string deletionDelay)
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
        var isValid = command.TryValidate(out var validationResults);

        // Assert
        isValid.ShouldBeTrue();
        validationResults.ShouldBeEmpty();
    }

    [Test]
    public void Invalid_SourceMessage_HasValidationError()
    {
        // Arrange
        var command = new SendTempReply
        {
            Text = "test",
            SourceMessage = null!
        };

        // Act
        var isValid = command.TryValidate(out var validationResults);

        // Assert
        isValid.ShouldBeFalse();

        var result = validationResults.ShouldHaveSingleItem();
        var memberName = result.MemberNames.ShouldHaveSingleItem();
        memberName.ShouldBe(nameof(command.SourceMessage));
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments(" ")]
    public void Invalid_Text_HasValidationError(string? text)
    {
        // Arrange
        var command = new SendTempReply
        {
            Text = text!,
            ReactionInfo = null,
            SourceMessage = Substitute.For<IUserMessage>()
        };

        // Act
        var isValid = command.TryValidate(out var validationResults);

        // Assert
        isValid.ShouldBeFalse();

        var result = validationResults.ShouldHaveSingleItem();
        var memberName = result.MemberNames.ShouldHaveSingleItem();
        memberName.ShouldBe(nameof(command.Text));
    }

    [Test]
    [Arguments("00:00:00")] // 0 seconds
    [Arguments("00:00:00.5")] // 0.5 seconds
    [Arguments("00:01:30.1")] // 1 minute 30.1 seconds
    public void Invalid_DeletionDelay_HasValidationError(string deletionDelay)
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
        var isValid = command.TryValidate(out var validationResults);

        // Assert
        isValid.ShouldBeFalse();

        var result = validationResults.ShouldHaveSingleItem();
        var memberName = result.MemberNames.ShouldHaveSingleItem();
        memberName.ShouldBe(nameof(command.DeletionDelay));
    }
}
