using DiscordTranslationBot.Countries;
using DiscordTranslationBot.Countries.Utilities;
using NeoSmart.Unicode;

namespace DiscordTranslationBot.Tests.Unit.Countries.Utilities;

public sealed class CountryUtilityTests
{
    [Fact]
    public void TryGetCountryByEmoji_Success()
    {
        // Arrange
        var expectedCountry = CountryConstants.SupportedCountries[0];

        var emojiUnicode = expectedCountry.EmojiUnicode;

        // Act
        var result = CountryUtility.TryGetCountryByEmoji(emojiUnicode, out var country);

        // Assert
        result.ShouldBeTrue();
        country.ShouldBe(expectedCountry);
    }

    [Fact]
    public void TryGetCountryByEmoji_NotAnEmoji()
    {
        // Arrange
        var emojiUnicode = Emoji.Airplane.Name;

        // Act
        var result = CountryUtility.TryGetCountryByEmoji(emojiUnicode, out var country);

        // Assert
        result.ShouldBeFalse();
        country.ShouldBeNull();
    }
}
