namespace DiscordTranslationBot.Countries.Models;

public interface ICountry
{
    /// <summary>
    /// The unicode string of the flag emoji.
    /// </summary>
    public string EmojiUnicode { get; }

    /// <summary>
    /// The name of the country.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The language codes for the country.
    /// </summary>
    public IReadOnlySet<string> LangCodes { get; }
}
