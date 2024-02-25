namespace DiscordTranslationBot.Countries.Models;

/// <summary>
/// Details about a country.
/// </summary>
public sealed class Country
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Country" /> class.
    /// </summary>
    /// <param name="emojiUnicode">The unicode string of the flag emoji.</param>
    /// <param name="name">The name of the country.</param>
    public Country(string emojiUnicode, string? name)
    {
        EmojiUnicode = emojiUnicode;
        Name = name ?? "Unknown";
    }

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
    public ISet<string> LangCodes { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}
