using DiscordTranslationBot.Exceptions;
using DiscordTranslationBot.Models;

namespace DiscordTranslationBot.Providers.Translation;

/// <summary>
/// Base class for translation providers.
/// </summary>
public abstract class TranslationProviderBase
{
    /// <summary>
    /// The name of the translation provider.
    /// </summary>
    public abstract string ProviderName { get; }

    /// <summary>
    /// Language code to country name map for languages that are supported by a translation provider.
    /// Refer to <see cref="NeoSmart.Unicode.Emoji"/> for list of country names by flag.
    /// </summary>
    protected abstract IReadOnlyDictionary<string, ISet<string>> LangCodeMap { get; }

    /// <summary>
    /// Translate text.
    /// </summary>
    /// <param name="countryName">The country name that will be used to get the target language code.</param>
    /// <param name="text">The text to translate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Translated text.</returns>
    public abstract Task<TranslationResult> TranslateAsync(string countryName, string text, CancellationToken cancellationToken);

    /// <summary>
    /// Get lang code from <see cref="LangCodeMap"/> by country name.
    /// </summary>
    /// <param name="countryName">The country name that will be used to get the target language code.</param>
    /// <returns>Lang code.</returns>
    /// <exception cref="UnsupportedCountryException">Country not supported.</exception>
    protected string GetLangCodeByCountryName(string countryName)
    {
        var langCode = LangCodeMap.SingleOrDefault(x => x.Value.Contains(countryName)).Key;

        return string.IsNullOrWhiteSpace(langCode) ?
            throw new UnsupportedCountryException($"Translation for country {countryName} isn't supported.") :
            langCode;
    }
}
