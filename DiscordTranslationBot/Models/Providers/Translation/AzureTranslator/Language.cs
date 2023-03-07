using System.Text.Json.Serialization;

namespace DiscordTranslationBot.Models.Providers.Translation.AzureTranslator;

/// <summary>
/// Supported language from the Azure Translator languages endpoint.
/// </summary>
public sealed class Language
{
    /// <summary>
    /// The language name.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }
}
