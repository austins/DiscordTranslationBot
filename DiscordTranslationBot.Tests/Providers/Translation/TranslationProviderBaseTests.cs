using DiscordTranslationBot.Exceptions;
using DiscordTranslationBot.Models;
using DiscordTranslationBot.Providers.Translation;
using FluentAssertions;
using Xunit;

namespace DiscordTranslationBot.Tests.Providers.Translation;

public abstract class TranslationProviderBaseTests : IAsyncLifetime
{
    protected TranslationProviderBase Sut { get; init; }

    public async Task InitializeAsync()
    {
        await Sut.InitializeSupportedLangCodesAsync(CancellationToken.None);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetLangCodeByCountry_Throws_UnsupportedCountryException()
    {
        // Arrange
        var country = new Country(NeoSmart.Unicode.Emoji.FlagFrance.ToString(), "unsupported_country")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
        };

        const string text = "test";

        // Act & Assert
        await Sut
            .Invoking(x => x.TranslateAsync(country, text, CancellationToken.None))
            .Should()
            .ThrowAsync<UnsupportedCountryException>();
    }
}
