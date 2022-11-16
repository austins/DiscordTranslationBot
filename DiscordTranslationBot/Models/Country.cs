namespace DiscordTranslationBot.Models;

/// <summary>
/// Details about a country.
/// </summary>
public class Country : IEquatable<Country>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Country"/> class.
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
    public ISet<string> LangCodes { get; init; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Check if two country instances are the same.
    /// </summary>
    /// <param name="other">Other country.</param>
    /// <returns>true if they are the same; false if not.</returns>
    public bool Equals(Country? other)
    {
        return EmojiUnicode == other?.EmojiUnicode;
    }

    /// <summary>
    /// Check if two country instances are the same.
    /// </summary>
    /// <param name="obj">Other object that could be a country instance.</param>
    /// <returns>true if they are the same; false if not.</returns>
    public override bool Equals(object? obj)
    {
        return obj is { }
            && (ReferenceEquals(this, obj) || (obj.GetType() == GetType() && Equals((Country)obj)));
    }

    /// <summary>
    /// Get the hash code for the equality check.
    /// </summary>
    /// <returns>Hash code.</returns>
    public override int GetHashCode()
    {
        return string.GetHashCode(EmojiUnicode, StringComparison.Ordinal);
    }
}
