using System.Net;

namespace DiscordTranslationBot.Providers.Translation.Models;

internal readonly union TranslationOutcome(TranslationResult, UnsupportedTargetLanguage, ProviderFailure);

internal sealed record UnsupportedTargetLanguage(string ProviderName);

internal sealed record ProviderFailure(string Reason, HttpStatusCode? StatusCode = null, Exception? Cause = null);

/// <summary>
/// Translation data to use in a Discord message.
/// </summary>
internal sealed class TranslationResult
{
    /// <summary>
    /// The detected language code from the provider's automatic detection.
    /// </summary>
    public string? DetectedLanguageCode { get; set; }

    /// <summary>
    /// The detected language name.
    /// </summary>
    public string? DetectedLanguageName { get; set; }

    /// <summary>
    /// The target language code.
    /// </summary>
    public required string TargetLanguageCode { get; init; }

    /// <summary>
    /// The target language name.
    /// </summary>
    public string? TargetLanguageName { get; init; }

    /// <summary>
    /// The translated text.
    /// </summary>
    public string TranslatedText { get; set; } = null!;
}
