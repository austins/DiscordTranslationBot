using System.Text;
using System.Text.Json;
using DiscordTranslationBot.Configuration.TranslationProviders;
using DiscordTranslationBot.Exceptions;
using DiscordTranslationBot.Models;
using DiscordTranslationBot.Models.Providers.Translation;
using DiscordTranslationBot.Models.Providers.Translation.LibreTranslate;
using Microsoft.Extensions.Options;

namespace DiscordTranslationBot.Providers.Translation;

/// <summary>
/// Provider for LibreTranslate.
/// </summary>
public sealed class LibreTranslateProvider : TranslationProviderBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly LibreTranslateOptions _libreTranslateOptions;
    private readonly ILogger<LibreTranslateProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibreTranslateProvider"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory to use.</param>
    /// <param name="translationProvidersOptions">Translation providers options.</param>
    /// <param name="logger">Logger to use.</param>
    public LibreTranslateProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<TranslationProvidersOptions> translationProvidersOptions,
        ILogger<LibreTranslateProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _libreTranslateOptions = translationProvidersOptions.Value.LibreTranslate;
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
        { "en", new HashSet<string> { CountryName.Australia, CountryName.Canada, CountryName.UnitedKingdom, CountryName.UnitedStates, CountryName.UnitedStatesOutlyingIslands } },
        { "ar", new HashSet<string> { CountryName.Algeria, CountryName.Bahrain, CountryName.Egypt, CountryName.SaudiArabia } },
        { "zh", new HashSet<string> { CountryName.China, CountryName.HongKong, CountryName.Taiwan } },
        { "fr", new HashSet<string> { CountryName.France } },
        { "de", new HashSet<string> { CountryName.Germany } },
        { "hi", new HashSet<string> { CountryName.India } },
        { "ga", new HashSet<string> { CountryName.Ireland } },
        { "it", new HashSet<string> { CountryName.Italy } },
        { "ja", new HashSet<string> { CountryName.Japan } },
        { "ko", new HashSet<string> { CountryName.SouthKorea } },
        { "pt", new HashSet<string> { CountryName.Brazil, CountryName.Portugal } },
        { "ru", new HashSet<string> { CountryName.Russia } },
        { "es", new HashSet<string> { CountryName.Mexico, CountryName.Spain } },
    };

    /// <inheritdoc cref="TranslationProviderBase.TranslateAsync"/>
    /// <exception cref="UnsupportedCountryException">Country not supported.</exception>
    public override async Task<TranslationResult> TranslateAsync(string countryName, string text, CancellationToken cancellationToken)
    {
        try
        {
            var langCode = GetLangCodeByCountryName(countryName);
            var result = new TranslationResult { TargetLanguageCode = langCode };

            using var httpClient = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(
                    $"{_libreTranslateOptions.ApiUrl}/translate"),
            };

            var requestBody = JsonSerializer.Serialize(new
            {
                q = text,
                source = "auto",
                target = langCode,
                format = "text",
            });

            request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Translate endpoint returned unsuccessful status code {response.StatusCode}.");
                throw new InvalidOperationException($"Translate endpoint returned unsuccessful status code {response.StatusCode}.");
            }

            var content = JsonSerializer.Deserialize<TranslateResult>(
                await response.Content.ReadAsStringAsync(cancellationToken));

            if (string.IsNullOrWhiteSpace(content?.TranslatedText))
            {
                _logger.LogError("No translation returned.");
                throw new InvalidOperationException("No translation returned.");
            }

            result.DetectedLanguageCode = content.DetectedLanguage?.LanguageCode;
            result.TranslatedText = content.TranslatedText;

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize the response.");
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, $"Unable to connect to the {ProviderName} API URL.");
            throw;
        }
    }
}
