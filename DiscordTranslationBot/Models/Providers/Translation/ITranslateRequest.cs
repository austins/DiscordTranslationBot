namespace DiscordTranslationBot.Models.Providers.Translation;

public interface ITranslateRequest
{
    /// <summary>
    /// The text to translate.
    /// </summary>
    public string Text { get; init; }
}
