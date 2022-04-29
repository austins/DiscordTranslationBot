using LibreTranslate.Net;
using NeoSmart.Unicode;

namespace DiscordTranslationBot.Services;

public sealed class FlagEmojiService
{
    private readonly IEnumerable<SingleEmoji> _emoji;

    public FlagEmojiService()
    {
        _emoji = Emoji.All.Where(e => e.Group == "Flags");
    }

    public string? GetCountryNameBySequence(UnicodeSequence sequence)
    {
        return _emoji.SingleOrDefault(e => e.Sequence == sequence).Name
            ?.Replace("flag: ", string.Empty);
    }

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