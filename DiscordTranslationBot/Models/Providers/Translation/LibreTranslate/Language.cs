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

    /// <summary>
    /// The language name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
}
