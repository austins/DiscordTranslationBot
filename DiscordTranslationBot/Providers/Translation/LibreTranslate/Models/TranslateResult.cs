using System.Text.Json.Serialization;
using DiscordTranslationBot.Models.Providers.Translation;

namespace DiscordTranslationBot.Providers.Translation.LibreTranslate.Models;

/// <summary>
/// The result from the LibreTranslate translate endpoint.
/// </summary>
public sealed class TranslateResult : ITranslateResult
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
