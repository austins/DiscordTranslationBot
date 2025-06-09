using DiscordTranslationBot.Providers.Translation.AzureTranslator.Models;
using DiscordTranslationBot.Providers.Translation.Models;

namespace DiscordTranslationBot.Providers.Translation.AzureTranslator;

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

    private readonly IAzureTranslatorClient _client;
    private readonly Log _log;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureTranslatorProvider" /> class.
    /// </summary>
    /// <param name="client">Azure Translator client to use.</param>
    /// <param name="logger">Logger to use.</param>
    public AzureTranslatorProvider(IAzureTranslatorClient client, ILogger<AzureTranslatorProvider> logger)
    {
        _client = client;
        _log = new Log(logger);
    }

    /// <inheritdoc cref="ITranslationProvider.TranslateCommandLangCodes" />
    public override IReadOnlySet<string> TranslateCommandLangCodes { get; } =
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
            "tr",
            "kk"
        };

    /// <inheritdoc cref="TranslationProviderBase.InitializeSupportedLanguagesAsync" />
    /// <remarks>
    /// List of supported language codes reference:
    /// https://docs.microsoft.com/en-us/azure/cognitive-services/translator/language-support#translation.
    /// </remarks>
    public override async Task InitializeSupportedLanguagesAsync(CancellationToken cancellationToken)
    {
        if (SupportedLanguages.Count > 0)
        {
            return;
        }

        var response = await _client.GetLanguagesAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _log.ResponseFailure(
                "Languages endpoint returned unsuccessful status code.",
                response.StatusCode,
                response.Error);

            throw new InvalidOperationException(
                $"Languages endpoint returned unsuccessful status code {response.StatusCode}.",
                response.Error);
        }

        if (response.Content?.LangCodes?.Any() != true)
        {
            _log.NoLanguageCodesReturned();
            throw new InvalidOperationException("Languages endpoint returned no language codes.");
        }

        SupportedLanguages = response
            .Content
            .LangCodes
            .Select(lc => new SupportedLanguage
            {
                LangCode = lc.Key,
                Name = lc.Value.Name
            })
            .ToHashSet();
    }

    /// <inheritdoc cref="ITranslationProvider.TranslateAsync" />
    /// <exception cref="ArgumentException">Text exceeds character limit.</exception>
    /// <exception cref="InvalidOperationException">An error occured.</exception>
    public override async Task<TranslationResult> TranslateAsync(
        SupportedLanguage targetLanguage,
        string text,
        CancellationToken cancellationToken,
        SupportedLanguage? sourceLanguage = null)
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

        var response = await _client.TranslateAsync(
            result.TargetLanguageCode,
            [new TranslateRequest { Text = text }],
            cancellationToken,
            sourceLanguage?.LangCode);

        if (!response.IsSuccessStatusCode)
        {
            _log.ResponseFailure(
                "Translate endpoint returned unsuccessful status code.",
                response.StatusCode,
                response.Error);

            throw new InvalidOperationException(
                $"Translate endpoint returned unsuccessful status code {response.StatusCode}.",
                response.Error);
        }

        var translation = response.Content?.FirstOrDefault();
        if (translation?.Translations?.Any() != true)
        {
            _log.NoTranslationReturned();
            throw new InvalidOperationException("No translation returned.");
        }

        result.DetectedLanguageCode = translation.DetectedLanguage?.LanguageCode;

        result.DetectedLanguageName = SupportedLanguages.FirstOrDefault(sl =>
                sl.LangCode.Equals(result.DetectedLanguageCode, StringComparison.OrdinalIgnoreCase))
            ?.Name;

        result.TranslatedText = translation.Translations[0].Text;

        return result;
    }

    private new sealed partial class Log(ILogger logger) : TranslationProviderBase.Log(logger)
    {
        [LoggerMessage(
            Level = LogLevel.Error,
            Message = "The text can't exceed {textCharacterLimit} characters including spaces. Length: {textLength}.")]
        public partial void CharacterLimitExceeded(int textCharacterLimit, int textLength);
    }
}
