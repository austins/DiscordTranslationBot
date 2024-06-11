using DiscordTranslationBot.Countries.Services;
using NeoSmart.Unicode;

namespace DiscordTranslationBot.Tests.Unit.Countries.Services;

public sealed class CountryServiceTests
{
    private readonly CountryService _sut;

    public CountryServiceTests()
    {
        _sut = new CountryService(new LoggerFake<CountryService>());
        _sut.Initialize();
    }

    public static TheoryData<string, string> TryGetCountryByEmojiData =>
        new()
        {
            { Emoji.FlagUnitedStates.ToString(), "United States" },
            { Emoji.FlagFrance.ToString(), "France" },
            { "🇯🇵", "Japan" }
        };

    [Theory]
    [MemberData(nameof(TryGetCountryByEmojiData))]
    public void TryGetCountryByEmoji_WithValidEmoji_Returns_Expected(string emojiUnicode, string expectedCountryName)
    {
        // Act & Assert
        _sut.TryGetCountryByEmoji(emojiUnicode, out var result).Should().BeTrue();
        result!.Name.Should().Be(expectedCountryName);
    }

    [Fact]
    public void TryGetCountryByEmoji_WithInvalidEmoji_Returns_Expected()
    {
        // Act & Assert
        _sut.TryGetCountryByEmoji(string.Empty, out var result).Should().BeFalse();
        result.Should().BeNull();
    }
}
