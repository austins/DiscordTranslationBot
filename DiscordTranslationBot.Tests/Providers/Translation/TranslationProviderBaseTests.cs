using DiscordTranslationBot.Exceptions;
using DiscordTranslationBot.Models;
using DiscordTranslationBot.Providers.Translation;
using FluentAssertions;
using NeoSmart.Unicode;
using Xunit;

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
    public async Task GetLangCodeByCountry_Throws_UnsupportedCountryException()
    {
        // Arrange
        var country = new Country(Emoji.FlagFrance.ToString(), "unsupported_country")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        };

        const string text = "test";

        // Act & Assert
        await Sut.Invoking(x => x.TranslateAsync(country, text, CancellationToken.None))
            .Should()
            .ThrowAsync<UnsupportedCountryException>();
    }
}
