using Discord;
using DiscordTranslationBot.Commands.TempReplies;
using DiscordTranslationBot.Extensions;
using DiscordTranslationBot.Models.Discord;

namespace DiscordTranslationBot.Tests.Commands.TempReplies;

public sealed class SendTempReplyTests
{
    [Test]
    public void Valid_ValidatesWithoutErrors()
    {
        // Arrange
        var command = new SendTempReply
        {
            Text = "test",
            Reaction = new Reaction
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

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void Invalid_Properties_HasValidationErrors(string? text)
    {
        // Arrange
        var command = new SendTempReply
        {
            Text = text!,
            Reaction = null,
            SourceMessage = null!
        };

        // Act
        var isValid = command.TryValidateObject(out var validationResults);

        // Assert
        isValid.Should().BeFalse();

        validationResults.Should()
            .HaveCount(2)
            .And.ContainSingle(x => x.MemberNames.All(y => y == nameof(command.Text)))
            .And.ContainSingle(x => x.MemberNames.All(y => y == nameof(command.SourceMessage)));
    }

    [Test]
    public void Invalid_DeletionDelay_HasValidationError()
    {
        // Arrange
        var command = new SendTempReply
        {
            Text = "test",
            Reaction = new Reaction
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

        validationResults.Should()
            .HaveCount(1)
            .And.ContainSingle(x => x.MemberNames.All(y => y == nameof(command.DeletionDelay)));
    }
}
