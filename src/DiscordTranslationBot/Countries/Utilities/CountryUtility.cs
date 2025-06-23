using DiscordTranslationBot.Countries.Models;
using NeoSmart.Unicode;
using System.Diagnostics.CodeAnalysis;

namespace DiscordTranslationBot.Countries.Utilities;

internal static class CountryUtility
{
    /// <summary>
    /// Get a country by a flag emoji unicode string.
    /// </summary>
    /// <param name="emojiUnicode">The unicode string of the flag emoji.</param>
    /// <param name="country">The country found.</param>
    /// <returns>true if country found; false if not.</returns>
    public static bool TryGetCountryByEmoji(string emojiUnicode, [NotNullWhen(true)] out ICountry? country)
    {
        if (!Emoji.IsEmoji(emojiUnicode))
        {
            country = null;
            return false;
        }

        country = CountryConstants.SupportedCountries.FirstOrDefault(c => c.EmojiUnicode == emojiUnicode);
        return country is not null;
    }
}
