using Discord;
using DiscordTranslationBot.Commands.TempReplies;
using DiscordTranslationBot.Discord.Models;
using DiscordTranslationBot.Extensions;
using System.Globalization;

namespace DiscordTranslationBot.Tests.Unit.Commands.TempReplies;

public sealed class SendTempReplyTests
{
    [Theory]
    [InlineData("00:00:01")]
    [InlineData("00:00:10")]
    [InlineData("00:00:30.678")]
    [InlineData("00:01:00")]
    [InlineData("00:01:30")]
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
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [Fact]
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
        isValid.Should().BeFalse();
        validationResults.Should().OnlyContain(x => x.MemberNames.All(y => y == nameof(command.SourceMessage)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
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
        isValid.Should().BeFalse();
        validationResults.Should().OnlyContain(x => x.MemberNames.All(y => y == nameof(command.Text)));
    }

    [Theory]
    [InlineData("00:00:00")] // 0 seconds
    [InlineData("00:00:00.5")] // 0.5 seconds
    [InlineData("00:01:30.1")] // 1 minute 30.1 seconds
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
        isValid.Should().BeFalse();
        validationResults.Should().OnlyContain(x => x.MemberNames.All(y => y == nameof(command.DeletionDelay)));
    }
}
