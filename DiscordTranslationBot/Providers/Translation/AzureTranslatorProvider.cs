using System.Text;
using System.Text.Json;
using DiscordTranslationBot.Configuration.TranslationProviders;
using DiscordTranslationBot.Exceptions;
using DiscordTranslationBot.Models;
using DiscordTranslationBot.Models.Providers.AzureTranslator;
using Microsoft.Extensions.Options;

namespace DiscordTranslationBot.Providers.Translation;

/// <summary>
/// Provider for Azure Translator.
/// </summary>
public sealed class AzureTranslatorProvider : ITranslationProvider
{
    /// <summary>
    /// Language code map.
    /// </summary>
    /// <remarks>
    /// Refer to Azure documentation for list of supported language codes: https://docs.microsoft.com/en-us/azure/cognitive-services/translator/language-support.
    /// Refer to <see cref="NeoSmart.Unicode.Emoji"/> for list of country names by flag.
    /// </remarks>
    private static readonly IReadOnlyDictionary<string, ISet<string>> LangCodeMap = new Dictionary<string, ISet<string>>
    {
        { "en", new HashSet<string> { CountryName.Australia, CountryName.Canada, CountryName.UnitedKingdom, CountryName.UnitedStates, CountryName.UnitedStatesOutlyingIslands } },
        { "ar", new HashSet<string> { CountryName.Algeria, CountryName.Bahrain, CountryName.Egypt, CountryName.SaudiArabia } },
        { "zh-Hans", new HashSet<string> { CountryName.China } },
        { "zh-Hant", new HashSet<string> { CountryName.HongKong, CountryName.Taiwan } },
        { "fr", new HashSet<string> { CountryName.France } },
        { "de", new HashSet<string> { CountryName.Germany } },
        { "hi", new HashSet<string> { CountryName.India } },
        { "ga", new HashSet<string> { CountryName.Ireland } },
        { "it", new HashSet<string> { CountryName.Italy } },
        { "ja", new HashSet<string> { CountryName.Japan } },
        { "ko", new HashSet<string> { CountryName.SouthKorea } },
        { "pt-br", new HashSet<string> { CountryName.Brazil } },
        { "pt-pt", new HashSet<string> { CountryName.Portugal } },
        { "ru", new HashSet<string> { CountryName.Russia } },
        { "es", new HashSet<string> { CountryName.Mexico, CountryName.Spain } },
        { "vi", new HashSet<string> { CountryName.Vietnam } },
        { "th", new HashSet<string> { CountryName.Thailand } },
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AzureTranslatorOptions _azureTranslatorOptions;
    private readonly ILogger<AzureTranslatorProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureTranslatorProvider"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory to use.</param>
    /// <param name="translationProvidersOptions">Translation providers options.</param>
    /// <param name="logger">Logger to use.</param>
    public AzureTranslatorProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<TranslationProvidersOptions> translationProvidersOptions,
        ILogger<AzureTranslatorProvider> logger)
    {
        _httpClientFactory = httpClientFactory;

        if (translationProvidersOptions == null)
            throw new ArgumentNullException(nameof(translationProvidersOptions));

        _azureTranslatorOptions = translationProvidersOptions.Value.AzureTranslator;

        _logger = logger;
    }

    /// <inheritdoc cref="ITranslationProvider.TranslateAsync"/>
    /// <exception cref="UnsupportedCountryException">Country not supported.</exception>
    public async Task<TranslationResult> TranslateAsync(string countryName, string text, CancellationToken cancellationToken)
    {
        try
        {
            var langCode = LangCodeMap.SingleOrDefault(x => x.Value.Contains(countryName)).Key;
            if (string.IsNullOrWhiteSpace(langCode))
            {
                _logger.LogWarning($"Translation for country [{countryName}] isn't supported.");
                throw new UnsupportedCountryException($"Translation for country {countryName} isn't supported (Azure Translator).");
            }

            if (text.Length >= 10000)
            {
                _logger.LogError($"The text can't exceed 10,000 characters including spaces. Length: {text.Length}.");
                throw new ArgumentException($"The text can't exceed 10,000 characters including spaces. Length: {text.Length}.");
            }

            var result = new TranslationResult { ProviderName = "Azure Translator", TargetLanguageCode = langCode };

            using var httpClient = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri =
                    new Uri(
                        $"{_azureTranslatorOptions.ApiUrl}/translate?api-version=3.0&to={result.TargetLanguageCode}"),
            };

            var requestBody = JsonSerializer.Serialize(new object[] { new { Text = text } });
            request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            request.Headers.Add("Ocp-Apim-Subscription-Key", _azureTranslatorOptions.SecretKey);
            request.Headers.Add("Ocp-Apim-Subscription-Region", _azureTranslatorOptions.Region);

            var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Translate endpoint returned unsuccessful status code {response.StatusCode}.");
                throw new InvalidOperationException($"Translate endpoint returned unsuccessful status code {response.StatusCode}.");
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var translations = JsonSerializer.Deserialize<IList<TranslateResult>>(content);

            var translation = translations?.SingleOrDefault();
            if (translation == null)
            {
                _logger.LogError("No translation returned.");
                throw new InvalidOperationException("No translation returned.");
            }

            result.DetectedLanguageCode = translation.DetectedLanguage?.LanguageCode;
            result.TranslatedText = translation.Translations[0].Text;

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize the response.");
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Unable to connect to the Azure Translator API URL.");
            throw;
        }
    }
}
