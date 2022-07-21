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
            var result = new TranslationResult
            {
                ProviderName = "LibreTranslate",
                TargetLanguageCode = langCode.ToString(),
            };

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
            CountryName.Australia => LanguageCode.English,
            CountryName.Canada => LanguageCode.English,
            CountryName.UnitedKingdom => LanguageCode.English,
            CountryName.UnitedStates => LanguageCode.English,
            CountryName.UnitedStatesOutlyingIslands => LanguageCode.English,
            CountryName.Algeria => LanguageCode.Arabic,
            CountryName.Bahrain => LanguageCode.Arabic,
            CountryName.Egypt => LanguageCode.Arabic,
            CountryName.SaudiArabia => LanguageCode.Arabic,
            CountryName.China => LanguageCode.Chinese,
            CountryName.HongKong => LanguageCode.Chinese,
            CountryName.Taiwan => LanguageCode.Chinese,
            CountryName.France => LanguageCode.French,
            CountryName.Germany => LanguageCode.German,
            CountryName.India => LanguageCode.Hindi,
            CountryName.Ireland => LanguageCode.Irish,
            CountryName.Italy => LanguageCode.Italian,
            CountryName.Japan => LanguageCode.Japanese,
            CountryName.SouthKorea => LanguageCode.Korean,
            CountryName.Portugal => LanguageCode.Portuguese,
            CountryName.Russia => LanguageCode.Russian,
            CountryName.Mexico => LanguageCode.Spanish,
            CountryName.Spain => LanguageCode.Spanish,
            _ => null
        };

        if (langCode == null)
        {
            _logger.LogWarning($"Translation for country [{countryName}] isn't supported.");
            throw new UnsupportedCountryException($"Translation for country {countryName} isn't supported (LibreTranslate).");
        }

        return langCode;
    }
}
