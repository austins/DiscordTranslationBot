using System.Text.Json.Serialization;
using DiscordTranslationBot.Models.Providers.Translation;

namespace DiscordTranslationBot.Providers.Translation.AzureTranslator.Models;

/// <summary>
/// Translate request for the Azure Translator translate endpoint.
/// </summary>
public sealed class TranslateRequest : ITranslateRequest
{
    /// <inheritdoc cref="ITranslateRequest.Text" />
    [JsonPropertyName("Text")]
    public required string Text { get; set; }
}
