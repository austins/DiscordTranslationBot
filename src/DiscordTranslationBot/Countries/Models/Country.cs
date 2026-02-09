using NeoSmart.Unicode;

namespace DiscordTranslationBot.Countries.Models;

/// <summary>
/// Details about a country.
/// </summary>
internal sealed class Country
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

        Name = emoji.Name?.Replace("flag: ", string.Empty, StringComparison.Ordinal) ?? "Unknown";
        LangCodes = langCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The name of the country.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The language codes for the country.
    /// </summary>
    public IReadOnlySet<string> LangCodes { get; }
}
