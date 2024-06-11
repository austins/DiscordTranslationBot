namespace DiscordTranslationBot.Providers.Translation.Models;

/// <summary>
/// Info about a translation provider's supported language.
/// </summary>
public sealed class SupportedLanguage : IEquatable<SupportedLanguage>
{
    /// <summary>
    /// The language code.
    /// </summary>
    public required string LangCode { get; init; }

    /// <summary>
    /// The language name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Check if two supported language instances are the same.
    /// </summary>
    /// <param name="other">Other supported language.</param>
    /// <returns>true if they are the same; false if not.</returns>
    public bool Equals(SupportedLanguage? other)
    {
        return other is not null
               && (ReferenceEquals(this, other) || LangCode.Equals(other.LangCode, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Check if two supported language instances are the same.
    /// </summary>
    /// <param name="obj">Other object that could be a supported language instance.</param>
    /// <returns>true if they are the same; false if not.</returns>
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || (obj is SupportedLanguage other && Equals(other));
    }

    /// <summary>
    /// Get the hash code for the equality check.
    /// </summary>
    /// <returns>Hash code.</returns>
    public override int GetHashCode()
    {
        return LangCode.GetHashCode(StringComparison.OrdinalIgnoreCase);
    }
}
