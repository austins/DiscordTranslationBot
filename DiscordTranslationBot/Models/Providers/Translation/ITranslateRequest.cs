namespace DiscordTranslationBot.Models.Providers.Translation;

/// <summary>
/// Interface for a translate request for a provider's translate endpoint.
/// </summary>
public interface ITranslateRequest
{
    /// <summary>
    /// The text to translate.
    /// </summary>
    public string Text { get; init; }
}
