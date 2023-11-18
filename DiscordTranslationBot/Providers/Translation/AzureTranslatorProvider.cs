using System.Text.Json;
using DiscordTranslationBot.Configuration.TranslationProviders;
using DiscordTranslationBot.Extensions;
using DiscordTranslationBot.Models.Providers.Translation;
using DiscordTranslationBot.Models.Providers.Translation.AzureTranslator;
using Microsoft.Extensions.Options;

namespace DiscordTranslationBot.Providers.Translation;

/// <summary>
/// Provider for Azure Translator.
/// </summary>
public sealed partial class AzureTranslatorProvider : TranslationProviderBase
{
    /// <summary>
    /// Azure has a limit of 10,000 characters for text in a request.
    /// See: https://docs.microsoft.com/en-us/azure/cognitive-services/translator/reference/v3-0-translate#request-body.
    /// </summary>
    public const int TextCharacterLimit = 10000;

    private readonly AzureTranslatorOptions _azureTranslatorOptions;
    private readonly Log _log;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureTranslatorProvider" /> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory to use.</param>
    /// <param name="translationProvidersOptions">Translation providers options.</param>
    /// <param name="logger">Logger to use.</param>
    public AzureTranslatorProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<TranslationProvidersOptions> translationProvidersOptions,
        ILogger<AzureTranslatorProvider> logger)
        : base(httpClientFactory)
    {
        _azureTranslatorOptions = translationProvidersOptions.Value.AzureTranslator;
        _log = new Log(logger);
    }

    /// <inheritdoc cref="TranslationProviderBase.TranslateCommandLangCodes" />
    public override IReadOnlySet<string> TranslateCommandLangCodes =>
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "en",
            "zh-Hans",
            "zh-Hant",
            "es",
            "hi",
            "ja",
            "pt-pt",
            "ru",
            "vi",
            "fr",
            "ar",
            "fil",
            "de",
            "id",
            "ko",
            "th",
            "uk",
            "nl",
            "el",
            "it",
            "tr"
        };

    /// <inheritdoc cref="TranslationProviderBase.ProviderName" />
    public override string ProviderName => "Azure Translator";

    /// <inheritdoc cref="TranslationProviderBase.InitializeSupportedLanguagesAsync" />
    /// <remarks>
    /// List of supported language codes reference:
    /// https://docs.microsoft.com/en-us/azure/cognitive-services/translator/language-support#translation.
    /// </remarks>
    public override async Task InitializeSupportedLanguagesAsync(CancellationToken cancellationToken)
    {
        if (SupportedLanguages.Any())
        {
            return;
        }

        var httpClient = CreateHttpClient();

        var response = await httpClient.GetAsync(
            new Uri("https://api.cognitive.microsofttranslator.com/languages?api-version=3.0&scope=translation"),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _log.ResponseFailure("Languages", response.StatusCode);

            throw new InvalidOperationException(
                $"Languages endpoint returned unsuccessful status code {response.StatusCode}.");
        }

        var content = JsonSerializer.Deserialize<Languages>(
            await response.Content.ReadAsStringAsync(cancellationToken));

        if (content?.LangCodes?.Any() != true)
        {
            _log.NoLanguageCodesReturned();
            throw new InvalidOperationException("Languages endpoint returned no language codes.");
        }

        SupportedLanguages = content.LangCodes.Select(
                lc => new SupportedLanguage
                {
                    LangCode = lc.Key,
                    Name = lc.Value.Name
                })
            .ToHashSet();
    }

    /// <inheritdoc cref="TranslationProviderBase.TranslateAsync" />
    /// <exception cref="ArgumentException">Text exceeds character limit.</exception>
    /// <exception cref="InvalidOperationException">An error occured.</exception>
    public override async Task<TranslationResult> TranslateAsync(
        SupportedLanguage targetLanguage,
        string text,
        CancellationToken cancellationToken,
        SupportedLanguage? sourceLanguage = null)
    {
        try
        {
            if (text.Length >= TextCharacterLimit)
            {
                _log.CharacterLimitExceeded(TextCharacterLimit, text.Length);

                throw new ArgumentException(
                    $"The text can't exceed {TextCharacterLimit} characters including spaces. Length: {text.Length}.");
            }

            var result = new TranslationResult
            {
                TargetLanguageCode = targetLanguage.LangCode,
                TargetLanguageName = targetLanguage.Name
            };

            var httpClient = CreateHttpClient();

            using var request = new HttpRequestMessage();
            request.Method = HttpMethod.Post;

            var translateUrl =
                $"{_azureTranslatorOptions.ApiUrl}translate?api-version=3.0&to={result.TargetLanguageCode}";

            if (sourceLanguage?.LangCode != null)
            {
                translateUrl += $"&from={sourceLanguage.LangCode}";
            }

            request.RequestUri = new Uri(translateUrl);

            request.Headers.Add("Ocp-Apim-Subscription-Key", _azureTranslatorOptions.SecretKey);
            request.Headers.Add("Ocp-Apim-Subscription-Region", _azureTranslatorOptions.Region);

            request.Content = new List<ITranslateRequest> { new TranslateRequest { Text = text } }
                .SerializeToRequestContent();

            var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _log.ResponseFailure("Translate", response.StatusCode);

                throw new InvalidOperationException(
                    $"Translate endpoint returned unsuccessful status code {response.StatusCode}.");
            }

            var content = await response.Content.ReadAsTranslateResultsAsync<TranslateResult>(cancellationToken);
            var translation = content?.SingleOrDefault();
            if (translation?.Translations?.Any() != true)
            {
                _log.NoTranslationReturned();
                throw new InvalidOperationException("No translation returned.");
            }

            result.DetectedLanguageCode = translation.DetectedLanguage?.LanguageCode;

            result.DetectedLanguageName = SupportedLanguages.SingleOrDefault(
                    sl => sl.LangCode.Equals(result.DetectedLanguageCode, StringComparison.OrdinalIgnoreCase))
                ?.Name;

            result.TranslatedText = translation.Translations[0].Text;

            return result;
        }
        catch (JsonException ex)
        {
            _log.DeserializationFailure(ex);
            throw;
        }
        catch (HttpRequestException ex)
        {
            _log.ConnectionFailure(ex, ProviderName);
            throw;
        }
    }

    private sealed partial class Log : Log<AzureTranslatorProvider>
    {
        private readonly ILogger<AzureTranslatorProvider> _logger;

        public Log(ILogger<AzureTranslatorProvider> logger)
            : base(logger)
        {
            _logger = logger;
        }

        [LoggerMessage(
            Level = LogLevel.Error,
            Message = "The text can't exceed {textCharacterLimit} characters including spaces. Length: {textLength}.")]
        public partial void CharacterLimitExceeded(int textCharacterLimit, int textLength);
    }
}
