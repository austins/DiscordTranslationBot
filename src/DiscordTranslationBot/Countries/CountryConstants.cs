using DiscordTranslationBot.Countries.Models;
using NeoSmart.Unicode;

namespace DiscordTranslationBot.Countries;

/// <summary>
/// Country constants used for flag emoji reactions.
/// </summary>
internal static class CountryConstants
{
    /// <summary>
    /// Countries supported for translation by flag emoji unicode and their supported language codes.
    /// </summary>
    /// <remarks>
    /// The language codes should be the primary language of the country that translation providers
    /// will translate to when the associated flag emoji is received. The language codes are based on
    /// the supported languages of each translation provider.
    /// </remarks>
    public static IReadOnlyDictionary<string, Country> SupportedCountries { get; } = InitializeSupportedCountries();

    private static IReadOnlyDictionary<string, Country> InitializeSupportedCountries()
    {
        var countries = new Dictionary<string, Country>();

        AddCountry(Emoji.FlagAustralia, "en");
        AddCountry(Emoji.FlagCanada, "en");
        AddCountry(Emoji.FlagUnitedKingdom, "en");
        AddCountry(Emoji.FlagUnitedStates, "en");
        AddCountry(Emoji.FlagUsOutlyingIslands, "en");
        AddCountry(Emoji.FlagAlgeria, "ar");
        AddCountry(Emoji.FlagBahrain, "ar");
        AddCountry(Emoji.FlagEgypt, "ar");
        AddCountry(Emoji.FlagSaudiArabia, "ar");
        AddCountry(Emoji.FlagChina, "zh-Hans", "zh");
        AddCountry(Emoji.FlagHongKongSarChina, "zh-Hant", "zh");
        AddCountry(Emoji.FlagTaiwan, "zh-Hant", "zh");
        AddCountry(Emoji.FlagFrance, "fr");
        AddCountry(Emoji.FlagGermany, "de");
        AddCountry(Emoji.FlagIndia, "hi");
        AddCountry(Emoji.FlagIreland, "ga");
        AddCountry(Emoji.FlagItaly, "it");
        AddCountry(Emoji.FlagJapan, "ja");
        AddCountry(Emoji.FlagSouthKorea, "ko");
        AddCountry(Emoji.FlagBrazil, "pt-br", "pt");
        AddCountry(Emoji.FlagPortugal, "pt-pt", "pt");
        AddCountry(Emoji.FlagRussia, "ru");
        AddCountry(Emoji.FlagMexico, "es");
        AddCountry(Emoji.FlagSpain, "es");
        AddCountry(Emoji.FlagVietnam, "vi");
        AddCountry(Emoji.FlagThailand, "th");
        AddCountry(Emoji.FlagUkraine, "uk");
        AddCountry(Emoji.FlagIndonesia, "id");
        AddCountry(Emoji.FlagKazakhstan, "kk");

        return countries;

        void AddCountry(SingleEmoji emoji, params string[] langCodes)
        {
            countries.Add(emoji.ToString(), new Country(emoji, langCodes));
        }
    }
}
