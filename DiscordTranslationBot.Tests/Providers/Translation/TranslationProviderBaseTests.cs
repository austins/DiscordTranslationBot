using DiscordTranslationBot.Exceptions;
using DiscordTranslationBot.Models;
using DiscordTranslationBot.Providers.Translation;
using NeoSmart.Unicode;

namespace DiscordTranslationBot.Tests.Providers.Translation;

[TestClass]
public abstract class TranslationProviderBaseTests
{
    protected TranslationProviderBase Sut { get; init; } = null!;

    [TestInitialize]
    public async Task TestInitialize()
    {
        ArgumentNullException.ThrowIfNull(Sut);
        await Sut.InitializeSupportedLanguagesAsync(CancellationToken.None);
    }

    [TestMethod]
    public void ProviderName_IsNotEmpty()
    {
        // Assert
        Sut.ProviderName.Should().NotBeEmpty();
    }

    [TestMethod]
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
            .ThrowAsync<UnsupportedCountryException>();
    }
}
