using LibreTranslate.Net;
using NeoSmart.Unicode;

namespace DiscordTranslationBot.Services;

/// <summary>
/// Provides methods to interact with flag emojis. This should be injected as a singleton so the flag emoji list
/// doesn't have to be regenerated.
/// </summary>
public sealed class FlagEmojiService
{
    private readonly IEnumerable<SingleEmoji> _emoji;

    /// <summary>Initializes the FlagEmojiService.</summary>
    public FlagEmojiService()
    {
        _emoji = Emoji.All.Where(e => e.Group == "Flags");
    }

    /// <summary>Get a country name by the Unicode sequence of an emoji.</summary>
    /// <param name="sequence">The Unicode sequence.</param>
    /// <returns>Country name.</returns>
    public string? GetCountryNameBySequence(UnicodeSequence sequence)
    {
        return _emoji.SingleOrDefault(e => e.Sequence == sequence).Name
            ?.Replace("flag: ", string.Empty);
    }

    /// <summary>Get the language code by country name.</summary>
    /// <remarks>More countries can be mapped here. Only some languages are supported by LibreTranslate.</remarks>
    /// <param name="countryName">The country name.</param>
    /// <returns><see cref="LanguageCode" /> for LibreTranslate.</returns>
    public static LanguageCode? GetLanguageCodeByCountryName(string countryName)
    {
        return countryName switch
        {
            "Australia" => LanguageCode.English,
            "Canada" => LanguageCode.English,
            "United Kingdom" => LanguageCode.English,
            "United States" => LanguageCode.English,
            "U.S. Outlying Islands" => LanguageCode.English,
            "Algeria" => LanguageCode.Arabic,
            "Bahrain" => LanguageCode.Arabic,
            "Egypt" => LanguageCode.Arabic,
            "Saudi Arabia" => LanguageCode.Arabic,
            "China" => LanguageCode.Chinese,
            "Hong Kong SAR China" => LanguageCode.Chinese,
            "Taiwan" => LanguageCode.Chinese,
            "France" => LanguageCode.French,
            "Germany" => LanguageCode.German,
            "India" => LanguageCode.Hindi,
            "Ireland" => LanguageCode.Irish,
            "Italy" => LanguageCode.Italian,
            "Japan" => LanguageCode.Japanese,
            "South Korea" => LanguageCode.Korean,
            "Portugal" => LanguageCode.Portuguese,
            "Russia" => LanguageCode.Russian,
            "Mexico" => LanguageCode.Spanish,
            "Spain" => LanguageCode.Spanish,
            _ => null
        };
    }
}