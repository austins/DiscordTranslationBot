using DiscordTranslationBot.Countries.Exceptions;
using DiscordTranslationBot.Countries.Models;
using DiscordTranslationBot.Providers.Translation;
using NeoSmart.Unicode;

namespace DiscordTranslationBot.Tests.Providers.Translation;

public abstract class TranslationProviderBaseTests
{
    protected TranslationProviderBase Sut { get; init; } = null!;

    [SetUp]
    public async Task SetUp()
    {
        ArgumentNullException.ThrowIfNull(Sut);
        await Sut.InitializeSupportedLanguagesAsync(CancellationToken.None);
    }

    [Test]
    public void ProviderName_IsNotEmpty()
    {
        // Assert
        Sut.ProviderName.Should().NotBeEmpty();
    }

    [Test]
    public async Task TranslateByCountryAsync_Throws_UnsupportedCountryException_IfLangCodeNotFound()
    {
        // Arrange
        var country = new Country(Emoji.FlagFrance.ToString()!, "unsupported_country")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        };

        const string text = "test";

        // Act & Assert
        await Sut.Invoking(x => x.TranslateByCountryAsync(country, text, CancellationToken.None))
            .Should()
            .ThrowAsync<LanguageNotSupportedForCountryException>();
    }
}
