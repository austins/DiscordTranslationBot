using Discord;
using DiscordTranslationBot.Commands.TempReplies;
using DiscordTranslationBot.Discord.Models;
using DiscordTranslationBot.Extensions;

namespace DiscordTranslationBot.Tests.Unit.Commands.TempReplies;

public sealed class SendTempReplyTests
{
    [Fact]
    public void Valid_ValidatesWithoutErrors()
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
        var isValid = command.TryValidateObject(out var validationResults);

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
        var isValid = command.TryValidateObject(out var validationResults);

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
            SourceMessage = Substitute.For<IUserMessage>()
        };

        // Act
        var isValid = command.TryValidateObject(out var validationResults);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().OnlyContain(x => x.MemberNames.All(y => y == nameof(command.Text)));
    }

    [Fact]
    public void Invalid_DeletionDelay_HasValidationError()
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
        var isValid = command.TryValidateObject(out var validationResults);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().OnlyContain(x => x.MemberNames.All(y => y == nameof(command.DeletionDelay)));
    }
}
