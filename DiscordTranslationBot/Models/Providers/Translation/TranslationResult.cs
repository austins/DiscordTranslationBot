namespace DiscordTranslationBot.Models.Providers.Translation;

/// <summary>
/// Translation data to use in a Discord message.
/// </summary>
public sealed class TranslationResult
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
