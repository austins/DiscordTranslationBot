using DiscordTranslationBot.Models;

namespace DiscordTranslationBot.Providers.Translation;

/// <summary>
/// Interface for translation providers.
/// </summary>
public interface ITranslationProvider
{
    /// <summary>
    /// Translate text.
    /// </summary>
    /// <param name="countryName">The country name that will be used to get the target language code.</param>
    /// <param name="text">The text to translate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Translated text.</returns>
    Task<TranslationResult> TranslateAsync(string countryName, string text, CancellationToken cancellationToken);
}
