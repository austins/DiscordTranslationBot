using Discord;
using DiscordTranslationBot.Commands.TempReply;
using DiscordTranslationBot.Extensions;
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
        var isValid = command.TryValidateObject(out var validationResults);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
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
        var isValid = command.TryValidateObject(out var validationResults);

        // Assert
        isValid.Should().BeFalse();

        validationResults.Should()
            .HaveCount(2)
            .And.ContainSingle(x => x.MemberNames.All(y => y == nameof(command.Reply)))
            .And.ContainSingle(x => x.MemberNames.All(y => y == nameof(command.SourceMessage)));
    }
}
