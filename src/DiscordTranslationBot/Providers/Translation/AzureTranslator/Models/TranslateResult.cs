using System.Text.Json.Serialization;

namespace DiscordTranslationBot.Providers.Translation.AzureTranslator.Models;

/// <summary>
/// The result from the Azure Translator translate endpoint.
/// </summary>
internal sealed class TranslateResult
{
    /// <summary>
    /// The automatically detected language.
    /// </summary>
    [JsonPropertyName("detectedLanguage")]
    public DetectedLanguage? DetectedLanguage { get; init; }

    /// <summary>
    /// The translation results.
    /// </summary>
    [JsonPropertyName("translations")]
    public IList<TranslationData>? Translations { get; init; }
}
