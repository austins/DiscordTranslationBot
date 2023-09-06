using System.Text.Json.Serialization;

namespace DiscordTranslationBot.Models.Providers.Translation.AzureTranslator;

/// <summary>
/// Translate request for the Azure Translator translate endpoint.
/// </summary>
public sealed class TranslateRequest : ITranslateRequest
{
    /// <inheritdoc cref="ITranslateRequest.Text" />
    [JsonPropertyName("Text")]
    public required string Text { get; set; }
}
