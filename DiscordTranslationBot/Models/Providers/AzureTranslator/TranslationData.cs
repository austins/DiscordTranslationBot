using System.Text.Json.Serialization;

namespace DiscordTranslationBot.Models.Providers.AzureTranslator;

/// <summary>
/// Translation data from the Azure Translator translate endpoint.
/// </summary>
public sealed class TranslationData
{
    /// <summary>
    /// The translated text.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = null!;
}
