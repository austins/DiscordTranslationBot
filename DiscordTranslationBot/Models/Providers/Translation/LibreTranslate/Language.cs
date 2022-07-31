using System.Text.Json.Serialization;

namespace DiscordTranslationBot.Models.Providers.Translation.LibreTranslate;

/// <summary>
/// Supported language from the LibreTranslate languages endpoint.
/// </summary>
public sealed class Language
{
    /// <summary>
    /// The language code.
    /// </summary>
    [JsonPropertyName("code")]
    public string LangCode { get; init; } = string.Empty;
}
