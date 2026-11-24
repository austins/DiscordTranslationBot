using DiscordTranslationBot.Providers.Translation.LibreTranslate.Models;
using DiscordTranslationBot.Providers.Translation.Models;
using DiscordTranslationBot.Telemetry;
using System.Collections.Frozen;

namespace DiscordTranslationBot.Providers.Translation.LibreTranslate;

/// <summary>
/// Provider for LibreTranslate.
/// </summary>
internal sealed class LibreTranslateProvider : TranslationProviderBase
{
    private readonly ILibreTranslateClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibreTranslateProvider" /> class.
    /// </summary>
    /// <param name="client">LibreTranslate client to use.</param>
    /// <param name="instrumentation">Instrumentation to use.</param>
    /// <param name="logger">Logger to use.</param>
    public LibreTranslateProvider(
        ILibreTranslateClient client,
        Instrumentation instrumentation,
        ILogger<LibreTranslateProvider> logger)
        : base(instrumentation, logger)
    {
        _client = client;
    }

    /// <inheritdoc cref="ITranslationProvider.InitializeSupportedLanguagesAsync" />
    /// <remarks>
    /// List of supported language codes reference: https://libretranslate.com/languages.
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
            ProviderLog.ResponseFailure(
                "Languages endpoint returned unsuccessful status code.",
                response.StatusCode,
                response.Error);

            throw new InvalidOperationException(
                $"Languages endpoint returned unsuccessful status code {response.StatusCode}.",
                response.Error);
        }

        if (response.Content is not { Count: > 0 })
        {
            ProviderLog.NoLanguageCodesReturned();
            throw new InvalidOperationException("Languages endpoint returned no language codes.");
        }

        SupportedLanguages = response.Content.ToFrozenDictionary(
            lc => lc.LangCode,
            lc => lc.Name,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc cref="TranslationProviderBase.TranslateCoreAsync" />
    protected override async Task<TranslationOutcome> TranslateCoreAsync(
        SupportedLanguage targetLanguage,
        string text,
        CancellationToken cancellationToken,
        string? sourceLangCode = null)
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
                SourceLangCode = sourceLangCode ?? "auto",
                TargetLangCode = targetLanguage.LangCode
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new ProviderFailure(
                "Translate endpoint returned unsuccessful status code.",
                response.StatusCode,
                response.Error);
        }

        if (string.IsNullOrWhiteSpace(response.Content?.TranslatedText))
        {
            return new ProviderFailure("No translation returned.", response.StatusCode, response.Error);
        }

        result.DetectedLanguageCode = response.Content.DetectedLanguage?.LanguageCode;

        result.DetectedLanguageName = result.DetectedLanguageCode is not null
            ? SupportedLanguages.GetValueOrDefault(result.DetectedLanguageCode)
            : null;

        result.TranslatedText = response.Content.TranslatedText;

        return result;
    }
}
