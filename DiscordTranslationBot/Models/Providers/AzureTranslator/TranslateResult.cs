using System.Text.Json.Serialization;

namespace DiscordTranslationBot.Models.Providers.AzureTranslator;

/// <summary>
/// The result from the Azure Translator translate endpoint.
/// </summary>
public sealed class TranslateResult
{
    /// <summary>
    /// The automatically detected language.
    /// </summary>
    [JsonPropertyName("detectedLanguage")]
    public DetectedLanguage DetectedLanguage { get; init; } = null!;

    /// <summary>
    /// The translation results.
    /// </summary>
    [JsonPropertyName("translations")]
    public IList<TranslationData> Translations { get; init; } = null!;
}
