using Discord;
using DiscordTranslationBot.Commands.Translation;
using DiscordTranslationBot.Countries.Models;
using DiscordTranslationBot.Discord.Models;
using DiscordTranslationBot.Extensions;

namespace DiscordTranslationBot.Tests.Unit.Commands.Translation;

public sealed class TranslateByCountryFlagEmojiReactionTests
{
    [Fact]
    public void Valid_Command_Validates_WithNoErrors()
    {
        // Arrange
        var request = new TranslateByCountryFlagEmojiReaction
        {
            Country = new Country("test", "test"),
            Message = Substitute.For<IUserMessage>(),
            ReactionInfo = new ReactionInfo
            {
                UserId = 1UL,
                Emote = Substitute.For<IEmote>()
            }
        };

        // Act
        var isValid = request.TryValidateObject(out var validationResults);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }
}
