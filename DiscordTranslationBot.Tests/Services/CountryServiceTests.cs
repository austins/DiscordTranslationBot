using DiscordTranslationBot.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoSmart.Unicode;
using Xunit;

namespace DiscordTranslationBot.Tests.Services;

public sealed class CountryServiceTests
{
    private readonly CountryService _sut;

    public CountryServiceTests()
    {
        _sut = new CountryService(Mock.Of<ILogger<CountryService>>());
    }

    public static IEnumerable<object[]> FlagEmojiUnicode =>
        new[]
        {
            new object[] { Emoji.FlagUnitedStates.ToString(), "United States" },
            new object[] { Emoji.FlagFrance.ToString(), "France" },
            new object[] { "🇯🇵", "Japan" },
        };

    [Theory]
    [MemberData(nameof(FlagEmojiUnicode))]
    public void TryGetCountry_Returns_Expected(string emojiUnicode, string expectedCountryName)
    {
        // Act & Assert
        _sut.TryGetCountry(emojiUnicode, out var result).Should().BeTrue();
        result!.Name.Should().Be(expectedCountryName);
    }
}
