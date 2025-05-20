using DiscordTranslationBot.Countries.Models;
using NeoSmart.Unicode;

namespace DiscordTranslationBot.Countries;

/// <summary>
/// Country constants used for flag emoji reactions.
/// </summary>
public static class CountryConstants
{
    /// <summary>
    /// Countries supported for translation by flag emojis and their supported language codes.
    /// </summary>
    /// <remarks>
    /// The language codes should be the primary language of the country that translation providers
    /// will translate to when the associated flag emoji is received. The language codes are based on
    /// the supported languages of each translation provider.
    /// </remarks>
    public static IReadOnlyList<ICountry> SupportedCountries { get; } =
    [
        new Country(Emoji.FlagAustralia, ["en"]),
        new Country(Emoji.FlagCanada, ["en"]),
        new Country(Emoji.FlagUnitedKingdom, ["en"]),
        new Country(Emoji.FlagUnitedStates, ["en"]),
        new Country(Emoji.FlagUsOutlyingIslands, ["en"]),
        new Country(Emoji.FlagAlgeria, ["ar"]),
        new Country(Emoji.FlagBahrain, ["ar"]),
        new Country(Emoji.FlagEgypt, ["ar"]),
        new Country(Emoji.FlagSaudiArabia, ["ar"]),
        new Country(Emoji.FlagChina, ["zh-Hans", "zh"]),
        new Country(Emoji.FlagHongKongSarChina, ["zh-Hant", "zh"]),
        new Country(Emoji.FlagTaiwan, ["zh-Hant", "zh"]),
        new Country(Emoji.FlagFrance, ["fr"]),
        new Country(Emoji.FlagGermany, ["de"]),
        new Country(Emoji.FlagIndia, ["hi"]),
        new Country(Emoji.FlagIreland, ["ga"]),
        new Country(Emoji.FlagItaly, ["it"]),
        new Country(Emoji.FlagJapan, ["ja"]),
        new Country(Emoji.FlagSouthKorea, ["ko"]),
        new Country(Emoji.FlagBrazil, ["pt-br", "pt"]),
        new Country(Emoji.FlagPortugal, ["pt-pt", "pt"]),
        new Country(Emoji.FlagRussia, ["ru"]),
        new Country(Emoji.FlagMexico, ["es"]),
        new Country(Emoji.FlagSpain, ["es"]),
        new Country(Emoji.FlagVietnam, ["vi"]),
        new Country(Emoji.FlagThailand, ["th"]),
        new Country(Emoji.FlagUkraine, ["uk"]),
        new Country(Emoji.FlagIndonesia, ["id"]),
        new Country(Emoji.FlagKazakhstan, ["kk"])
    ];
}
