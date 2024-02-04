using DiscordTranslationBot.Services;
using NeoSmart.Unicode;

namespace DiscordTranslationBot.Tests.Services;

[TestClass]
public sealed class CountryServiceTests
{
    private readonly CountryService _sut;

    public CountryServiceTests()
    {
        _sut = new CountryService(Substitute.For<ILogger<CountryService>>());
    }

    public static IEnumerable<object[]> TryGetCountryTestData =>
        new List<object[]>
        {
            new object[] { Emoji.FlagUnitedStates.ToString(), "United States" },
            new object[] { Emoji.FlagFrance.ToString(), "France" },
            new object[] { "🇯🇵", "Japan" }
        };

    [DataTestMethod]
    [DynamicData(nameof(TryGetCountryTestData))]
    public void TryGetCountry_Returns_Expected(string emojiUnicode, string expectedCountryName)
    {
        // Act & Assert
        _sut.TryGetCountry(emojiUnicode, out var result).Should().BeTrue();
        result!.Name.Should().Be(expectedCountryName);
    }
}
