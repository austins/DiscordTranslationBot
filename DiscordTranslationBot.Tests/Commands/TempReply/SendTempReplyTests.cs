using Discord;
using DiscordTranslationBot.Commands.TempReply;
using DiscordTranslationBot.Models.Discord;

namespace DiscordTranslationBot.Tests.Commands.TempReply;

public sealed class SendTempReplyTests : ValidationTestBase
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
            SourceMessage = Substitute.For<IMessage>(),
            DeletionDelayInSeconds = 20
        };

        // Act
        var (results, isValid) = ValidateObject(command);

        // Assert
        results.Should().BeEmpty();
        isValid.Should().BeTrue();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void Invalid_Reply_HasValidationErrors(string? text)
    {
        // Arrange
        var command = new SendTempReply
        {
            Text = text!,
            Reaction = null,
            SourceMessage = null!
        };

        // Act
        var (results, isValid) = ValidateObject(command);

        // Assert
        results.Should().HaveCount(2);
        results.Should().ContainSingle(x => x.MemberNames.All(y => y == nameof(command.Text)));
        results.Should().ContainSingle(x => x.MemberNames.All(y => y == nameof(command.SourceMessage)));
        isValid.Should().BeFalse();
    }
}
