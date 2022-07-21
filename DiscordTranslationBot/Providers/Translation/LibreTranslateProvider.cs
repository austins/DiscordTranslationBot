using DiscordTranslationBot.Exceptions;
using DiscordTranslationBot.Models;
using LibreTranslate.Net;

namespace DiscordTranslationBot.Providers.Translation;

/// <summary>
/// Provider for LibreTranslate.
/// </summary>
public sealed class LibreTranslateProvider : ITranslationProvider
{
    private readonly LibreTranslate.Net.LibreTranslate _libreTranslate;
    private readonly ILogger<LibreTranslateProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibreTranslateProvider"/> class.
    /// </summary>
    /// <param name="libreTranslate">LibreTranslate client to use.</param>
    /// <param name="logger">Logger to use.</param>
    public LibreTranslateProvider(LibreTranslate.Net.LibreTranslate libreTranslate, ILogger<LibreTranslateProvider> logger)
    {
        _libreTranslate = libreTranslate;
        _logger = logger;
    }

    /// <inheritdoc cref="ITranslationProvider.TranslateAsync"/>
    public async Task<TranslationResult> TranslateAsync(string countryName, string text, CancellationToken cancellationToken)
    {
        try
        {
            var langCode = GetTargetLanguageCodeByCountryName(countryName);
            var result = new TranslationResult { TargetLanguageCode = langCode.ToString() };

            result.TranslatedText = await _libreTranslate.TranslateAsync(
                new Translate
                {
                    Source = LanguageCode.AutoDetect,
                    Target = langCode,
                    Text = text,
                });

            return result;
        }
        catch (HttpRequestException ex) when
            (ex.StackTrace?.Contains(nameof(LibreTranslate.Net.LibreTranslate), StringComparison.Ordinal) == true)
        {
            _logger.LogError(ex, "Unable to connect to the LibreTranslate API URL.");
            throw;
        }
    }

    /// <summary>
    /// Get the target language code by country name.
    /// </summary>
    /// <remarks>
    /// More countries can be mapped here. Only some languages are supported by LibreTranslate.
    /// </remarks>
    /// <param name="countryName">The country name.</param>
    /// <returns><see cref="LanguageCode" /> for LibreTranslate.</returns>
    /// <exception cref="UnsupportedCountryException">Country not supported.</exception>
    private LanguageCode GetTargetLanguageCodeByCountryName(string countryName)
    {
        var langCode = countryName switch
        {
            "Australia" => LanguageCode.English,
            "Canada" => LanguageCode.English,
            "United Kingdom" => LanguageCode.English,
            "United States" => LanguageCode.English,
            "U.S. Outlying Islands" => LanguageCode.English,
            "Algeria" => LanguageCode.Arabic,
            "Bahrain" => LanguageCode.Arabic,
            "Egypt" => LanguageCode.Arabic,
            "Saudi Arabia" => LanguageCode.Arabic,
            "China" => LanguageCode.Chinese,
            "Hong Kong SAR China" => LanguageCode.Chinese,
            "Taiwan" => LanguageCode.Chinese,
            "France" => LanguageCode.French,
            "Germany" => LanguageCode.German,
            "India" => LanguageCode.Hindi,
            "Ireland" => LanguageCode.Irish,
            "Italy" => LanguageCode.Italian,
            "Japan" => LanguageCode.Japanese,
            "South Korea" => LanguageCode.Korean,
            "Portugal" => LanguageCode.Portuguese,
            "Russia" => LanguageCode.Russian,
            "Mexico" => LanguageCode.Spanish,
            "Spain" => LanguageCode.Spanish,
            _ => null
        };

        if (langCode == null)
        {
            _logger.LogWarning($"Translation for country [{countryName}] isn't supported.");
            throw new UnsupportedCountryException($"Translation for country [{countryName}] isn't supported.");
        }

        return langCode;
    }
}
