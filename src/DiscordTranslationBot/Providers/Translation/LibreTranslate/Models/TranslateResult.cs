using System.Text.Json.Serialization;

namespace DiscordTranslationBot.Providers.Translation.LibreTranslate.Models;

/// <summary>
/// The result from the LibreTranslate translate endpoint.
/// </summary>
internal sealed class TranslateResult
{
    /// <summary>
    /// The automatically detected language.
    /// </summary>
    [JsonPropertyName("detectedLanguage")]
    public DetectedLanguage? DetectedLanguage { get; init; }

    /// <summary>
    /// The translation results.
    /// </summary>
    [JsonPropertyName("translatedText")]
    public string? TranslatedText { get; init; }
}
