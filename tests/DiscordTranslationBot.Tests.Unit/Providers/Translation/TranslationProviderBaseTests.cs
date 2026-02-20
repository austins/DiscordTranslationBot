using DiscordTranslationBot.Countries.Exceptions;
using DiscordTranslationBot.Countries.Models;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.Models;
using NeoSmart.Unicode;

namespace DiscordTranslationBot.Tests.Unit.Providers.Translation;

public sealed class TranslationProviderBaseTests
{
    [Fact]
    public async Task TranslateByCountryAsync_Throws_UnsupportedCountryException_IfLangCodeNotFound()
    {
        // Arrange
        var country = new Country(Emoji.FlagUnitedStates, ["unsupported-lang-code"]);

        const string text = "test";

        var sut = new TranslationProviderFake();

        // Act & Assert
        await sut
            .Awaiting(x => x.TranslateByCountryAsync(country, text, TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<LanguageNotSupportedForCountryException>();
    }

    private sealed class TranslationProviderFake : TranslationProviderBase
    {
        public override Task InitializeSupportedLanguagesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override Task<TranslationResult> TranslateAsync(
            SupportedLanguage targetLanguage,
            string text,
            CancellationToken cancellationToken,
            SupportedLanguage? sourceLanguage = null)
        {
            return Task.FromResult(new TranslationResult { TargetLanguageCode = "test" });
        }
    }
}
