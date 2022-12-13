using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace DiscordTranslationBot.Models.Providers.Translation.LibreTranslate;

/// <summary>
/// Supported language from the LibreTranslate languages endpoint.
/// </summary>
[UsedImplicitly]
public sealed class Language
{
    /// <summary>
    /// The language code.
    /// </summary>
    [JsonPropertyName("code")]
    public required string LangCode
    {
        get;
        [UsedImplicitly]
        init;
    }

    /// <summary>
    /// The language name.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name
    {
        get;
        [UsedImplicitly]
        init;
    }
}
