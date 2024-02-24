using DiscordTranslationBot.Countries.Services;
using NeoSmart.Unicode;

namespace DiscordTranslationBot.Tests.Services;

public sealed class CountryServiceTests
{
    private readonly CountryService _sut;

    public CountryServiceTests()
    {
        _sut = new CountryService(new LoggerFake<CountryService>());
        _sut.Initialize();
    }

    public static IReadOnlyCollection<(string EmojiUnicode, string ExpectedCountryName)> TryGetCountryByEmojiTestData =>
        new List<(string, string)>
        {
            (Emoji.FlagUnitedStates.ToString(), "United States"),
            (Emoji.FlagFrance.ToString(), "France"),
            ("🇯🇵", "Japan")
        };

    [TestCaseSource(nameof(TryGetCountryByEmojiTestData))]
    public void TryGetCountryByEmoji_WithValidEmoji_Returns_Expected(
        (string EmojiUnicode, string ExpectedCountryName) data)
    {
        // Act & Assert
        _sut.TryGetCountryByEmoji(data.EmojiUnicode, out var result).Should().BeTrue();
        result!.Name.Should().Be(data.ExpectedCountryName);
    }

    [Test]
    public void TryGetCountryByEmoji_WithInvalidEmoji_Returns_Expected()
    {
        // Act & Assert
        _sut.TryGetCountryByEmoji(string.Empty, out var result).Should().BeFalse();
        result.Should().BeNull();
    }
}
