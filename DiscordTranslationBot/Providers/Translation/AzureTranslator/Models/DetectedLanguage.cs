using System.Text.Json.Serialization;

namespace DiscordTranslationBot.Providers.Translation.AzureTranslator.Models;

/// <summary>
/// The detected language from the Azure Translator translate endpoint.
/// </summary>
public sealed class DetectedLanguage
{
    /// <summary>
    /// The detected language code.
    /// </summary>
    [JsonPropertyName("language")]
    public required string LanguageCode { get; init; }
}
