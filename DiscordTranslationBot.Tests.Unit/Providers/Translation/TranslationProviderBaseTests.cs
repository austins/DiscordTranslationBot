using DiscordTranslationBot.Countries.Exceptions;
using DiscordTranslationBot.Countries.Models;
using DiscordTranslationBot.Providers.Translation;
using NeoSmart.Unicode;

namespace DiscordTranslationBot.Tests.Unit.Providers.Translation;

public abstract class TranslationProviderBaseTests : IAsyncLifetime
{
    protected TranslationProviderBase Sut { get; init; } = null!;

    public async Task InitializeAsync()
    {
        ArgumentNullException.ThrowIfNull(Sut);
        await Sut.InitializeSupportedLanguagesAsync(CancellationToken.None);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public void ProviderName_IsNotEmpty()
    {
        // Assert
        Sut.ProviderName.Should().NotBeEmpty();
    }

    [Fact]
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
