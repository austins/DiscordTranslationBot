using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace DiscordTranslationBot.Models.Providers.Translation.LibreTranslate;

/// <summary>
/// The detected language from the LibreTranslate translate endpoint.
/// </summary>
[UsedImplicitly]
public sealed class DetectedLanguage
{
    /// <summary>
    /// The detected language code.
    /// </summary>
    [JsonPropertyName("language")]
    public required string LanguageCode
    {
        get;
        [UsedImplicitly]
        init;
    }
}
