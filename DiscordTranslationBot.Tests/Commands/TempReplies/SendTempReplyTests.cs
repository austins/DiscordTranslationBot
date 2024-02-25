using Discord;
using DiscordTranslationBot.Commands.TempReplies;
using DiscordTranslationBot.Discord.Models;
using DiscordTranslationBot.Extensions;

namespace DiscordTranslationBot.Tests.Commands.TempReplies;

public sealed class SendTempReplyTests
{
    [Test]
    public void Valid_ValidatesWithoutErrors()
    {
        // Arrange
        var request = new SendTempReply
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
        var isValid = request.TryValidateObject(out var validationResults);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void Invalid_Properties_HasValidationErrors(string? text)
    {
        // Arrange
        var request = new SendTempReply
        {
            Text = text!,
            ReactionInfo = null,
            SourceMessage = null!
        };

        // Act
        var isValid = request.TryValidateObject(out var validationResults);

        // Assert
        isValid.Should().BeFalse();

        validationResults.Should()
            .HaveCount(2)
            .And.ContainSingle(x => x.MemberNames.All(y => y == nameof(request.Text)))
            .And.ContainSingle(x => x.MemberNames.All(y => y == nameof(request.SourceMessage)));
    }

    [Test]
    public void Invalid_DeletionDelay_HasValidationError()
    {
        // Arrange
        var request = new SendTempReply
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
        var isValid = request.TryValidateObject(out var validationResults);

        // Assert
        isValid.Should().BeFalse();

        validationResults.Should()
            .HaveCount(1)
            .And.ContainSingle(x => x.MemberNames.All(y => y == nameof(request.DeletionDelay)));
    }
}
