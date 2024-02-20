using DiscordTranslationBot.Models.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.LibreTranslate.Models;

namespace DiscordTranslationBot.Providers.Translation.LibreTranslate;

/// <summary>
/// Provider for LibreTranslate.
/// </summary>
public sealed class LibreTranslateProvider : TranslationProviderBase
{
    private readonly ILibreTranslateClient _client;
    private readonly Log<LibreTranslateProvider> _log;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibreTranslateProvider" /> class.
    /// </summary>
    /// <param name="client">LibreTranslate client to use.</param>
    /// <param name="logger">Logger to use.</param>
    public LibreTranslateProvider(ILibreTranslateClient client, ILogger<LibreTranslateProvider> logger)
    {
        _client = client;
        _log = new Log(logger);
    }

    /// <inheritdoc cref="TranslationProviderBase.ProviderName" />
    public override string ProviderName => "LibreTranslate";

    /// <inheritdoc cref="TranslationProviderBase.InitializeSupportedLanguagesAsync" />
    /// <remarks>
    /// List of supported language codes reference: https://libretranslate.com/languages.
    /// </remarks>
    public override async Task InitializeSupportedLanguagesAsync(CancellationToken cancellationToken)
    {
        if (SupportedLanguages.Any())
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

        if (response.Content?.Any() != true)
        {
            _log.NoLanguageCodesReturned();
            throw new InvalidOperationException("Languages endpoint returned no language codes.");
        }

        SupportedLanguages = response.Content.Select(
                lc => new SupportedLanguage
                {
                    LangCode = lc.LangCode,
                    Name = lc.Name
                })
            .ToHashSet();
    }

    /// <inheritdoc cref="TranslationProviderBase.TranslateAsync" />
    /// <exception cref="InvalidOperationException">An error occured.</exception>
    public override async Task<TranslationResult> TranslateAsync(
        SupportedLanguage targetLanguage,
        string text,
        CancellationToken cancellationToken,
        SupportedLanguage? sourceLanguage = null)
    {
        var result = new TranslationResult
        {
            TargetLanguageCode = targetLanguage.LangCode,
            TargetLanguageName = targetLanguage.Name
        };

        var response = await _client.TranslateAsync(
            new TranslateRequest
            {
                Text = text,
                SourceLangCode = sourceLanguage?.LangCode ?? "auto",
                TargetLangCode = targetLanguage.LangCode
            },
            cancellationToken);

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

        if (string.IsNullOrWhiteSpace(response.Content?.TranslatedText))
        {
            _log.NoTranslationReturned();
            throw new InvalidOperationException("No translation returned.");
        }

        result.DetectedLanguageCode = response.Content.DetectedLanguage?.LanguageCode;

        result.DetectedLanguageName = SupportedLanguages.FirstOrDefault(
                sl => sl.LangCode.Equals(result.DetectedLanguageCode, StringComparison.OrdinalIgnoreCase))
            ?.Name;

        result.TranslatedText = response.Content.TranslatedText;

        return result;
    }

    private sealed class Log : Log<LibreTranslateProvider>
    {
        public Log(ILogger<LibreTranslateProvider> logger)
            : base(logger)
        {
        }
    }
}
