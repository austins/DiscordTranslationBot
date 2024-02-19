using Discord;
using DiscordTranslationBot.Commands.TempReply;
using DiscordTranslationBot.Models.Discord;

namespace DiscordTranslationBot.Tests.Commands.TempReply;

public sealed class DeleteTempReplyTests
{
    [Test]
    public void Valid_ValidatesWithoutErrors()
    {
        // Arrange
        var command = new DeleteTempReply
        {
            Reply = Substitute.For<IMessage>(),
            Reaction = new Reaction
            {
                UserId = 1,
                Emote = Substitute.For<IEmote>()
            },
            SourceMessage = Substitute.For<IMessage>()
        };

        // Act
        var (results, isValid) = command.ValidateObject();

        // Assert
        results.Should().BeEmpty();
        isValid.Should().BeTrue();
    }

    [Test]
    public void Invalid_Reply_HasValidationErrors()
    {
        // Arrange
        var command = new DeleteTempReply
        {
            Reply = null!,
            Reaction = null,
            SourceMessage = null!
        };

        // Act
        var (results, isValid) = command.ValidateObject();

        // Assert
        results.Should().HaveCount(2);
        results.Should().ContainSingle(x => x.MemberNames.All(y => y == nameof(command.Reply)));
        results.Should().ContainSingle(x => x.MemberNames.All(y => y == nameof(command.SourceMessage)));
        isValid.Should().BeFalse();
    }
}
