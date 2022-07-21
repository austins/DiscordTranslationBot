using DiscordTranslationBot.Exceptions;
using DiscordTranslationBot.Models;
using LibreTranslate.Net;

namespace DiscordTranslationBot.Providers.Translation;

/// <summary>
/// Provider for LibreTranslate.
/// </summary>
public sealed class LibreTranslateProvider : ITranslationProvider
{
    /// <summary>
    /// Language code map.
    /// </summary>
    /// <remarks>
    /// Only some languages are supported by LibreTranslate.
    /// Refer to <see cref="NeoSmart.Unicode.Emoji"/> for list of country names by flag.
    /// </remarks>
    private static readonly IDictionary<LanguageCode, IList<string>> LangCodeMap = new Dictionary<LanguageCode, IList<string>>
    {
        { LanguageCode.English, new List<string> { CountryName.Australia, CountryName.Canada, CountryName.UnitedKingdom, CountryName.UnitedStates, CountryName.UnitedStatesOutlyingIslands } },
        { LanguageCode.Arabic, new List<string> { CountryName.Algeria, CountryName.Bahrain, CountryName.Egypt, CountryName.SaudiArabia } },
        { LanguageCode.Chinese, new List<string> { CountryName.China, CountryName.HongKong, CountryName.Taiwan } },
        { LanguageCode.French, new List<string> { CountryName.France } },
        { LanguageCode.German, new List<string> { CountryName.Germany } },
        { LanguageCode.Hindi, new List<string> { CountryName.India } },
        { LanguageCode.Irish, new List<string> { CountryName.Ireland } },
        { LanguageCode.Italian, new List<string> { CountryName.Italy } },
        { LanguageCode.Japanese, new List<string> { CountryName.Japan } },
        { LanguageCode.Korean, new List<string> { CountryName.SouthKorea } },
        { LanguageCode.Portuguese, new List<string> { CountryName.Brazil, CountryName.Portugal } },
        { LanguageCode.Russian, new List<string> { CountryName.Russia } },
        { LanguageCode.Spanish, new List<string> { CountryName.Mexico, CountryName.Spain } },
    };

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
    /// <exception cref="UnsupportedCountryException">Country not supported.</exception>
    public async Task<TranslationResult> TranslateAsync(string countryName, string text, CancellationToken cancellationToken)
    {
        try
        {
            var langCode = LangCodeMap.SingleOrDefault(c => c.Value.Contains(countryName)).Key;
            if (langCode == null)
            {
                _logger.LogWarning($"Translation for country [{countryName}] isn't supported.");
                throw new UnsupportedCountryException($"Translation for country {countryName} isn't supported (LibreTranslate).");
            }

            var result = new TranslationResult
            {
                ProviderName = "LibreTranslate",
                TargetLanguageCode = langCode.ToString(),
                TranslatedText = await _libreTranslate.TranslateAsync(
                    new Translate
                    {
                        Source = LanguageCode.AutoDetect,
                        Target = langCode,
                        Text = text,
                    }),
            };

            return result;
        }
        catch (HttpRequestException ex) when
            (ex.StackTrace?.Contains(nameof(LibreTranslate.Net.LibreTranslate), StringComparison.Ordinal) == true)
        {
            _logger.LogError(ex, "Unable to connect to the LibreTranslate API URL.");
            throw;
        }
    }
}
