using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace DiscordTranslationBot.Models.Providers.Translation.AzureTranslator;

/// <summary>
/// Supported language from the Azure Translator languages endpoint.
/// </summary>
[UsedImplicitly]
public sealed class Language
{
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
