using System.Text.Json.Serialization;

namespace DiscordTranslationBot.Models.Providers.Translation.AzureTranslator;

/// <summary>
/// The list of supported languages from the Azure Translator languages endpoint.
/// </summary>
public sealed class Languages
{
    /// <summary>
    /// Language codes stored as the keys.
    /// </summary>
    [JsonPropertyName("translation")]
    public IDictionary<string, Language>? LangCodes { get; init; }
}
