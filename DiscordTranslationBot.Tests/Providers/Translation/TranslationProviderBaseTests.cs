using DiscordTranslationBot.Exceptions;
using DiscordTranslationBot.Models;
using DiscordTranslationBot.Providers.Translation;
using NeoSmart.Unicode;

namespace DiscordTranslationBot.Tests.Providers.Translation;

public abstract class TranslationProviderBaseTests : IAsyncLifetime
{
    protected TranslationProviderBase Sut { get; init; } = null!;

    public async Task InitializeAsync()
    {
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
    private async Task TranslateByCountryAsync_Throws_UnsupportedCountryException_IfLangCodeNotFound()
    {
        // Arrange
        var country = new Country(Emoji.FlagFrance.ToString(), "unsupported_country")
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
