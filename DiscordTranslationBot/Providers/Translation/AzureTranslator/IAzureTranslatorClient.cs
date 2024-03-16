using DiscordTranslationBot.Providers.Translation.AzureTranslator.Models;
using Refit;

namespace DiscordTranslationBot.Providers.Translation.AzureTranslator;

/// <summary>
/// API client for Azure Translator.
/// </summary>
public interface IAzureTranslatorClient
{
    /// <summary>
    /// Get supported languages.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>API response of languages.</returns>
    [Get("/languages?api-version=3.0&scope=translation")]
    public Task<IApiResponse<Languages>> GetLanguagesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Translate text (multiple supported).
    /// </summary>
    /// <param name="targetLangCode">The target language.</param>
    /// <param name="requests">Info about text to translate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="sourceLangCode">
    /// Optionally, the source language code. If null, the service will try to automatically
    /// detect the source language.
    /// </param>
    /// <returns>API response of translation results.</returns>
    [Post("/translate?api-version=3.0")]
    public Task<IApiResponse<IList<TranslateResult>>> TranslateAsync(
        [AliasAs("to")] [Query] string targetLangCode,
        [Body] IList<TranslateRequest> requests,
        CancellationToken cancellationToken,
        [AliasAs("from")] [Query] string? sourceLangCode = null);
}
