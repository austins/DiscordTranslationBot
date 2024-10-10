using NeoSmart.Unicode;

namespace DiscordTranslationBot.Countries.Models;

/// <summary>
/// Details about a country.
/// </summary>
public sealed class Country : ICountry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Country" /> class.
    /// </summary>
    /// <param name="emoji">The flag emoji.</param>
    /// <param name="langCodes">The language codes for the country.</param>
    public Country(SingleEmoji emoji, string[] langCodes)
    {
        if (emoji is not { Group: "Flags", Subgroup: "country-flag" })
        {
            throw new ArgumentException("Emoji is not a flag emoji.", nameof(emoji));
        }

        if (langCodes.Length == 0)
        {
            throw new ArgumentException(
                "Supported language codes set for a country cannot be empty.",
                nameof(langCodes));
        }

        EmojiUnicode = emoji.ToString();
        Name = emoji.Name?.Replace("flag: ", string.Empty, StringComparison.Ordinal) ?? "Unknown";
        LangCodes = langCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc cref="ICountry.EmojiUnicode" />
    public string EmojiUnicode { get; }

    /// <inheritdoc cref="ICountry.Name" />
    public string Name { get; }

    /// <inheritdoc cref="ICountry.LangCodes" />
    public IReadOnlySet<string> LangCodes { get; }
}

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
