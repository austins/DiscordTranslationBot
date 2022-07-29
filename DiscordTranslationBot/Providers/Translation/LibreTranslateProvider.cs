using DiscordTranslationBot.Exceptions;
using DiscordTranslationBot.Models;
using LibreTranslate.Net;

namespace DiscordTranslationBot.Providers.Translation;

/// <summary>
/// Provider for LibreTranslate.
/// </summary>
public sealed class LibreTranslateProvider : TranslationProviderBase
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

    /// <inheritdoc cref="TranslationProviderBase.ProviderName"/>
    public override string ProviderName => "LibreTranslate";

    /// <summary>
    /// <inheritdoc cref="TranslationProviderBase.LangCodeMap"/>
    /// </summary>
    /// <remarks>
    /// Only some languages are supported by LibreTranslate.
    /// </remarks>
    protected override IReadOnlyDictionary<string, ISet<string>> LangCodeMap { get; } = new Dictionary<string, ISet<string>>
    {
        { LanguageCode.English.ToString(), new HashSet<string> { CountryName.Australia, CountryName.Canada, CountryName.UnitedKingdom, CountryName.UnitedStates, CountryName.UnitedStatesOutlyingIslands } },
        { LanguageCode.Arabic.ToString(), new HashSet<string> { CountryName.Algeria, CountryName.Bahrain, CountryName.Egypt, CountryName.SaudiArabia } },
        { LanguageCode.Chinese.ToString(), new HashSet<string> { CountryName.China, CountryName.HongKong, CountryName.Taiwan } },
        { LanguageCode.French.ToString(), new HashSet<string> { CountryName.France } },
        { LanguageCode.German.ToString(), new HashSet<string> { CountryName.Germany } },
        { LanguageCode.Hindi.ToString(), new HashSet<string> { CountryName.India } },
        { LanguageCode.Irish.ToString(), new HashSet<string> { CountryName.Ireland } },
        { LanguageCode.Italian.ToString(), new HashSet<string> { CountryName.Italy } },
        { LanguageCode.Japanese.ToString(), new HashSet<string> { CountryName.Japan } },
        { LanguageCode.Korean.ToString(), new HashSet<string> { CountryName.SouthKorea } },
        { LanguageCode.Portuguese.ToString(), new HashSet<string> { CountryName.Brazil, CountryName.Portugal } },
        { LanguageCode.Russian.ToString(), new HashSet<string> { CountryName.Russia } },
        { LanguageCode.Spanish.ToString(), new HashSet<string> { CountryName.Mexico, CountryName.Spain } },
    };

    /// <inheritdoc cref="TranslationProviderBase.TranslateAsync"/>
    /// <exception cref="UnsupportedCountryException">Country not supported.</exception>
    public override async Task<TranslationResult> TranslateAsync(string countryName, string text, CancellationToken cancellationToken)
    {
        try
        {
            var langCode = GetLangCodeByCountryName(countryName);

            var result = new TranslationResult
            {
                TargetLanguageCode = langCode,
                TranslatedText = await _libreTranslate.TranslateAsync(
                    new Translate
                    {
                        Source = LanguageCode.AutoDetect,
                        Target = LanguageCode.FromString(langCode),
                        Text = text,
                    }),
            };

            return result;
        }
        catch (HttpRequestException ex) when
            (ex.StackTrace?.Contains(nameof(LibreTranslate.Net.LibreTranslate), StringComparison.Ordinal) == true)
        {
            _logger.LogError(ex, $"Unable to connect to the {ProviderName} API URL.");
            throw;
        }
    }
}
