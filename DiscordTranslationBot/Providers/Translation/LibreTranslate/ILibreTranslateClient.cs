using DiscordTranslationBot.Providers.Translation.LibreTranslate.Models;
using Refit;

namespace DiscordTranslationBot.Providers.Translation.LibreTranslate;

/// <summary>
/// API client for LibreTranslate.
/// </summary>
public interface ILibreTranslateClient
{
    /// <summary>
    /// Get supported languages.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>API response of languages.</returns>
    [Get("/languages")]
    public Task<IApiResponse<IList<Language>>> GetLanguagesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Translate text (multiple supported).
    /// </summary>
    /// <param name="request">Info about text to translate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>API response of translation result.</returns>
    [Post("/translate")]
    public Task<IApiResponse<TranslateResult>> TranslateAsync(
        [Body] TranslateRequest request,
        CancellationToken cancellationToken);
}
