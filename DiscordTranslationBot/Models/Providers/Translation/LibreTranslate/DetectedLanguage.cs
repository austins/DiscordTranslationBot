using System.Text.Json.Serialization;

namespace DiscordTranslationBot.Models.Providers.Translation.LibreTranslate;

/// <summary>
/// The detected language from the LibreTranslate translate endpoint.
/// </summary>
public sealed class DetectedLanguage
{
    /// <summary>
    /// The detected language code.
    /// </summary>
    [JsonPropertyName("language")]
    public string LanguageCode { get; init; } = null!;
}
