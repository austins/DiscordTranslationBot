using System.Text.Json.Serialization;

namespace DiscordTranslationBot.Providers.Translation.LibreTranslate.Models;

/// <summary>
/// Supported language from the LibreTranslate languages endpoint.
/// </summary>
internal sealed class Language
{
    /// <summary>
    /// The language code.
    /// </summary>
    [JsonPropertyName("code")]
    public required string LangCode { get; init; }

    /// <summary>
    /// The language name.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }
}
