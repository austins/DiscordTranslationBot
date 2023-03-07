using System.Text.Json.Serialization;

namespace DiscordTranslationBot.Models.Providers.Translation.LibreTranslate;

/// <summary>
/// The result from the LibreTranslate translate endpoint.
/// </summary>
public sealed class TranslateResult
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
