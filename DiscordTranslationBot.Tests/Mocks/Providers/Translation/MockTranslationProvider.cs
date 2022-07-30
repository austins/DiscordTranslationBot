using DiscordTranslationBot.Models;
using DiscordTranslationBot.Models.Providers.Translation;
using DiscordTranslationBot.Providers.Translation;

namespace DiscordTranslationBot.Tests.Mocks.Providers.Translation;
internal sealed class MockTranslationProvider : TranslationProviderBase
{
    public override string ProviderName => "Mock Translation Provider";

    protected override IReadOnlyDictionary<string, ISet<string>> LangCodeMap { get; } = new Dictionary<string, ISet<string>>
    {
        { "en", new HashSet<string> { CountryName.UnitedStates } },
    };

    public override async Task<TranslationResult> TranslateAsync(string countryName, string text, CancellationToken cancellationToken)
    {
        return await Task.FromResult(new TranslationResult
        {
            DetectedLanguageCode = "en",
            TargetLanguageCode = "en",
            TranslatedText = "test",
        });
    }
}
