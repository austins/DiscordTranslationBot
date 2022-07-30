using DiscordTranslationBot.Models;
using DiscordTranslationBot.Services;
using FluentAssertions;
using NeoSmart.Unicode;
using Xunit;

namespace DiscordTranslationBot.Tests.Services;

public sealed class FlagEmojiServiceTests
{
    private readonly FlagEmojiService _sut;

    public FlagEmojiServiceTests()
    {
        _sut = new FlagEmojiService();
    }

    public static IEnumerable<object[]> FlagSequences =>
        new[]
        {
            new object[] { Emoji.FlagUnitedStates.Sequence, CountryName.UnitedStates },
            new object[] { Emoji.FlagFrance.Sequence, CountryName.France },
            new object[] { "🇯🇵".AsUnicodeSequence(), CountryName.Japan },
        };

    [Theory]
    [MemberData(nameof(FlagSequences))]
    public void GetCountryNameBySequence_Returns_Expected(UnicodeSequence sequence, string expected)
    {
        // Act
        var countryName = _sut.GetCountryNameBySequence(sequence);

        // Assert
        countryName.Should().Be(expected);
    }
}
