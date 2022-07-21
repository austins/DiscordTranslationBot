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
    public async Task<TranslationResult> TranslateAsync(string countryName, string text, CancellationToken cancellationToken)
    {
        try
        {
            var result = new TranslationResult
            {
                ProviderName = "Azure Translator",
                TargetLanguageCode = GetTargetLanguageCodeByCountryName(countryName),
            };

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

            result.DetectedLanguageCode = translation.DetectedLanguage.LanguageCode;
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

    /// <summary>
    /// Get the target language code by country name.
    /// </summary>
    /// <remarks>
    /// More countries can be mapped here.
    /// <see cref="NeoSmart.Unicode.Emoji"/> for list of country names by flag.
    /// </remarks>
    /// <param name="countryName">The country name.</param>
    /// <returns>Target language code.</returns>
    /// <exception cref="UnsupportedCountryException">Country not supported.</exception>
    private string GetTargetLanguageCodeByCountryName(string countryName)
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
            CountryName.China => LanguageCode.ChineseSimplified,
            CountryName.HongKong => LanguageCode.ChineseTraditional,
            CountryName.Taiwan => LanguageCode.ChineseTraditional,
            CountryName.France => LanguageCode.French,
            CountryName.Germany => LanguageCode.German,
            CountryName.India => LanguageCode.Hindi,
            CountryName.Ireland => LanguageCode.Irish,
            CountryName.Italy => LanguageCode.Italian,
            CountryName.Japan => LanguageCode.Japanese,
            CountryName.SouthKorea => LanguageCode.Korean,
            CountryName.Brazil => LanguageCode.PortugueseBrazil,
            CountryName.Portugal => LanguageCode.PortuguesePortugal,
            CountryName.Russia => LanguageCode.Russian,
            CountryName.Mexico => LanguageCode.Spanish,
            CountryName.Spain => LanguageCode.Spanish,
            CountryName.Vietnam => LanguageCode.Vietnamese,
            CountryName.Thailand => LanguageCode.Thai,
            _ => null
        };

        if (langCode == null)
        {
            _logger.LogWarning($"Translation for country [{countryName}] isn't supported.");
            throw new UnsupportedCountryException($"Translation for country {countryName} isn't supported (Azure Translator).");
        }

        return langCode;
    }

    /// <summary>
    /// Language code map.
    /// Refer to Azure documentation for list of supported language codes: https://docs.microsoft.com/en-us/azure/cognitive-services/translator/language-support.
    /// </summary>
    private static class LanguageCode
    {
        public const string English = "en";
        public const string Arabic = "ar";
        public const string ChineseSimplified = "zh-Hans";
        public const string ChineseTraditional = "zh-Hant";
        public const string French = "fr";
        public const string German = "de";
        public const string Hindi = "hi";
        public const string Irish = "ga";
        public const string Italian = "it";
        public const string Japanese = "ja";
        public const string Korean = "ko";
        public const string PortugueseBrazil = "pt-br";
        public const string PortuguesePortugal = "pt-pt";
        public const string Russian = "ru";
        public const string Spanish = "es";
        public const string Vietnamese = "vi";
        public const string Thai = "th";
    }
}
