using DiscordTranslationBot.Exceptions;
using DiscordTranslationBot.Providers.Translation;
using FluentAssertions;
using Xunit;

namespace DiscordTranslationBot.Tests.Providers.Translation;

public abstract class TranslationProviderBaseTests
{
    protected abstract TranslationProviderBase Sut { get; }

    [Fact]
    public async Task GetLangCodeByCountryName_Throws_UnsupportedCountryException()
    {
        // Arrange
        const string countryName = "unsupported_country";
        const string text = "test";

        // Act & Assert
        await Sut
            .Invoking(x => x.TranslateAsync(countryName, text, CancellationToken.None))
            .Should()
            .ThrowAsync<UnsupportedCountryException>();
    }
}
