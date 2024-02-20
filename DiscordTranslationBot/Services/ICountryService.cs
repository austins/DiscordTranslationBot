using System.Diagnostics.CodeAnalysis;
using DiscordTranslationBot.Models;

namespace DiscordTranslationBot.Services;

/// <summary>
/// Interface for <see cref="CountryService" />.
/// </summary>
public interface ICountryService
{
    /// <summary>
    /// Initializes the country service.
    /// </summary>
    /// <exception cref="InvalidOperationException">No flag emoji found or country couldn't be found.</exception>
    void Initialize();

    /// <summary>
    /// Get a country by a flag emoji unicode string.
    /// </summary>
    /// <param name="emojiUnicode">The unicode string of the flag emoji.</param>
    /// <param name="country">The country found.</param>
    /// <returns>true if country found; false if not.</returns>
    bool TryGetCountryByEmoji(string emojiUnicode, [NotNullWhen(true)] out Country? country);
}
