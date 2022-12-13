using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace DiscordTranslationBot.Models.Providers.Translation.LibreTranslate;

/// <summary>
/// The result from the LibreTranslate translate endpoint.
/// </summary>
[UsedImplicitly]
public sealed class TranslateResult
{
    /// <summary>
    /// The automatically detected language.
    /// </summary>
    [JsonPropertyName("detectedLanguage")]
    public DetectedLanguage? DetectedLanguage
    {
        get;
        [UsedImplicitly]
        init;
    }

    /// <summary>
    /// The translation results.
    /// </summary>
    [JsonPropertyName("translatedText")]
    public string? TranslatedText
    {
        get;
        [UsedImplicitly]
        init;
    }
}
