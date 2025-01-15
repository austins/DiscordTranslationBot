using Discord;
using DiscordTranslationBot.Commands.TempReplies;
using DiscordTranslationBot.Discord.Models;
using DiscordTranslationBot.Extensions;

namespace DiscordTranslationBot.Tests.Unit.Commands.TempReplies;

public sealed class DeleteTempReplyTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Valid_ValidatesWithoutErrors(bool hasReactionInfo)
    {
        // Arrange
        var command = new DeleteTempReply
        {
            Reply = Substitute.For<IUserMessage>(),
            SourceMessageId = 1UL,
            ReactionInfo = hasReactionInfo
                ? new ReactionInfo
                {
                    UserId = 1,
                    Emote = Substitute.For<IEmote>()
                }
                : null
        };

        // Act
        var isValid = command.TryValidate(out var validationResults);

        // Assert
        isValid.ShouldBeTrue();
        validationResults.ShouldBeEmpty();
    }

    [Fact]
    public void Invalid_Reply_HasValidationError()
    {
        // Arrange
        var command = new DeleteTempReply
        {
            Reply = null!,
            SourceMessageId = 1UL,
            ReactionInfo = new ReactionInfo
            {
                UserId = 1,
                Emote = Substitute.For<IEmote>()
            }
        };

        // Act
        var isValid = command.TryValidate(out var validationResults);

        // Assert
        isValid.ShouldBeFalse();

        var result = validationResults.ShouldHaveSingleItem();
        var memberName = result.MemberNames.ShouldHaveSingleItem();
        memberName.ShouldBe(nameof(command.Reply));
    }
}
