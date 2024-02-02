using DiscordTranslationBot.Services;
using NeoSmart.Unicode;

namespace DiscordTranslationBot.Tests.Services;

public sealed class CountryServiceTests
{
    private readonly CountryService _sut;

    public CountryServiceTests()
    {
        _sut = new CountryService(Substitute.For<ILogger<CountryService>>());
    }

    public static TheoryData<string, string> TryGetCountryTestData =>
        new()
        {
            { Emoji.FlagUnitedStates.ToString(), "United States" },
            { Emoji.FlagFrance.ToString(), "France" },
            { "🇯🇵", "Japan" }
        };

    [Theory]
    [MemberData(nameof(TryGetCountryTestData))]
    public void TryGetCountry_Returns_Expected(string emojiUnicode, string expectedCountryName)
    {
        // Act & Assert
        _sut.TryGetCountry(emojiUnicode, out var result).Should().BeTrue();
        result!.Name.Should().Be(expectedCountryName);
    }
}
