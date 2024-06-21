using Discord;
using DiscordTranslationBot.Commands.Translation;
using DiscordTranslationBot.Countries.Models;
using DiscordTranslationBot.Discord.Models;
using DiscordTranslationBot.Extensions;
using Emoji = NeoSmart.Unicode.Emoji;

namespace DiscordTranslationBot.Tests.Unit.Commands.Translation;

public sealed class TranslateByCountryFlagEmojiReactionTests
{
    [Fact]
    public void Valid_Command_Validates_WithNoErrors()
    {
        // Arrange
        var command = new TranslateByCountryFlagEmojiReaction
        {
            Country = new Country(Emoji.FlagFrance.ToString()!, "France")
            {
                LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
            },
            Message = Substitute.For<IUserMessage>(),
            ReactionInfo = new ReactionInfo
            {
                UserId = 1UL,
                Emote = new global::Discord.Emoji(Emoji.FlagUnitedStates.ToString())
            }
        };

        // Act
        var isValid = command.TryValidate(out var validationResults);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void Invalid_Command_Validates_WithErrors()
    {
        // Arrange
        var command = new TranslateByCountryFlagEmojiReaction
        {
            Country = null!,
            Message = null!,
            ReactionInfo = null!
        };

        // Act
        var isValid = command.TryValidate(out var validationResults);

        // Assert
        isValid.Should().BeFalse();

        validationResults
            .Should()
            .ContainSingle(x => x.MemberNames.All(y => y == nameof(command.Country)))
            .And
            .ContainSingle(x => x.MemberNames.All(y => y == nameof(command.Message)))
            .And
            .ContainSingle(x => x.MemberNames.All(y => y == nameof(command.ReactionInfo)));
    }
}
