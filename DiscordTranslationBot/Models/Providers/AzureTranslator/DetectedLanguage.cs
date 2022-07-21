using System.Text.Json.Serialization;

namespace DiscordTranslationBot.Models.Providers.AzureTranslator;

/// <summary>
/// The detected language from the Azure Translator translate endpoint.
/// </summary>
public sealed class DetectedLanguage
{
    /// <summary>
    /// The detected language code.
    /// </summary>
    [JsonPropertyName("language")]
    public string LanguageCode { get; set; } = null!;

    /// <summary>
    /// The confidence of the automatic language detection.
    /// </summary>
    [JsonPropertyName("score")]
    public float Score { get; set; }
}
