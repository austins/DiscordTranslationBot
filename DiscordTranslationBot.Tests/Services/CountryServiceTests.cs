using DiscordTranslationBot.Services;
using NeoSmart.Unicode;

namespace DiscordTranslationBot.Tests.Services;

public sealed class CountryServiceTests
{
    private readonly CountryService _sut;

    public CountryServiceTests()
    {
        _sut = new CountryService(new LoggerFake<CountryService>());
    }

    public static IReadOnlyCollection<(string EmojiUnicode, string ExpectedCountryName)> TryGetCountryTestData =>
        new List<(string, string)>
        {
            (Emoji.FlagUnitedStates.ToString(), "United States"),
            (Emoji.FlagFrance.ToString(), "France"),
            ("🇯🇵", "Japan")
        };

    [TestCaseSource(nameof(TryGetCountryTestData))]
    public void TryGetCountry_Returns_Expected((string EmojiUnicode, string ExpectedCountryName) data)
    {
        // Act & Assert
        _sut.TryGetCountry(data.EmojiUnicode, out var result).Should().BeTrue();
        result!.Name.Should().Be(data.ExpectedCountryName);
    }
}
