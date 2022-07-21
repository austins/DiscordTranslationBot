namespace DiscordTranslationBot.Models;

/// <summary>
/// Translation data to use in a Discord message.
/// </summary>
public sealed class TranslationResult
{
    /// <summary>
    /// The translation provider display name.
    /// </summary>
    public string ProviderName { get; set; } = null!;

    /// <summary>
    /// The detected language code from the provider's automatic detection.
    /// </summary>
    public string? DetectedLanguageCode { get; set; }

    /// <summary>
    /// The target language code.
    /// </summary>
    public string TargetLanguageCode { get; set; } = null!;

    /// <summary>
    /// The translated text.
    /// </summary>
    public string TranslatedText { get; set; } = null!;
}
