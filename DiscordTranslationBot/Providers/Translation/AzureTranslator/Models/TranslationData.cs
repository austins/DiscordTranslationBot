using System.Text.Json.Serialization;

namespace DiscordTranslationBot.Providers.Translation.AzureTranslator.Models;

/// <summary>
/// Translation data from the Azure Translator translate endpoint.
/// </summary>
public sealed class TranslationData
{
    /// <summary>
    /// The translated text.
    /// </summary>
    [JsonPropertyName("text")]
    public required string Text { get; init; }
}
