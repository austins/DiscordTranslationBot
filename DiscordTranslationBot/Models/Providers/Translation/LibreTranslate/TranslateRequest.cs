using System.Text.Json.Serialization;

namespace DiscordTranslationBot.Models.Providers.Translation.LibreTranslate;

/// <summary>
/// Translate request for the LibreTranslate translate endpoint.
/// </summary>
public sealed class TranslateRequest : ITranslateRequest
{
    /// <summary>
    /// The language code to translate from.
    /// </summary>
    [JsonPropertyName("source")]
    public required string SourceLangCode { get; init; }

    /// <summary>
    /// The language code to translate to.
    /// </summary>
    [JsonPropertyName("target")]
    public required string TargetLangCode { get; init; }

    /// <summary>
    /// The format.
    /// </summary>
    [JsonPropertyName("format")]
    public string Format { get; init; } = "text";

    /// <inheritdoc cref="ITranslateRequest.Text" />
    [JsonPropertyName("q")]
    public required string Text { get; init; }
}
