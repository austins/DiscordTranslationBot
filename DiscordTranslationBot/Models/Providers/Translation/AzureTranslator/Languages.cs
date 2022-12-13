using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace DiscordTranslationBot.Models.Providers.Translation.AzureTranslator;

/// <summary>
/// The list of supported languages from the Azure Translator languages endpoint.
/// </summary>
[UsedImplicitly]
public sealed class Languages
{
    /// <summary>
    /// Language codes stored as the keys.
    /// </summary>
    [UsedImplicitly]
    [JsonPropertyName("translation")]
    public IDictionary<string, Language>? LangCodes { get; init; }
}
