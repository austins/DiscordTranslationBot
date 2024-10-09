namespace DiscordTranslationBot.Providers.Translation.Models;

/// <summary>
/// Interface for a translate request for a provider's translate endpoint.
/// </summary>
internal interface ITranslateRequest
{
    /// <summary>
    /// The text to translate.
    /// </summary>
    public string Text { get; set; }
}
