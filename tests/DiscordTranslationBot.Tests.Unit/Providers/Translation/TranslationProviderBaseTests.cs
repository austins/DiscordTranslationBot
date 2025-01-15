using DiscordTranslationBot.Countries.Exceptions;
using DiscordTranslationBot.Countries.Models;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.Models;

namespace DiscordTranslationBot.Tests.Unit.Providers.Translation;

public sealed class TranslationProviderBaseTests
{
    [Test]
    public async Task TranslateByCountryAsync_Throws_UnsupportedCountryException_IfLangCodeNotFound(
        CancellationToken cancellationToken)
    {
        // Arrange
        var country = Substitute.For<ICountry>();
        country.Name.Returns("Test");
        country.LangCodes.Returns(new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        const string text = "test";

        var sut = new TranslationProviderFake();

        // Act & Assert
        await Should.ThrowAsync<LanguageNotSupportedForCountryException>(
            async () => await sut.TranslateByCountryAsync(country, text, cancellationToken));
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
