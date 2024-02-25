using DiscordTranslationBot.Countries.Services;
using NeoSmart.Unicode;

namespace DiscordTranslationBot.Countries;

/// <summary>
/// Country constants used for flag emoji reactions.
/// </summary>
internal static class CountryConstants
{
    /// <summary>
    /// Maps supported language codes to countries.
    /// The language codes should be the primary language of the country that translation providers
    /// will translate to when the associated flag emoji is received. The language codes are based on
    /// the supported languages of each translation provider.
    /// In <see cref="CountryService.Initialize" />, the language codes will be mapped case-insensitive to a set of countries.
    /// </summary>
    internal static IReadOnlyDictionary<SingleEmoji, string[]> LangCodeMap { get; } =
        new Dictionary<SingleEmoji, string[]>
        {
            { Emoji.FlagAustralia, ["en"] },
            { Emoji.FlagCanada, ["en"] },
            { Emoji.FlagUnitedKingdom, ["en"] },
            { Emoji.FlagUnitedStates, ["en"] },
            { Emoji.FlagUsOutlyingIslands, ["en"] },
            { Emoji.FlagAlgeria, ["ar"] },
            { Emoji.FlagBahrain, ["ar"] },
            { Emoji.FlagEgypt, ["ar"] },
            { Emoji.FlagSaudiArabia, ["ar"] },
            { Emoji.FlagChina, ["zh-Hans", "zh"] },
            { Emoji.FlagHongKongSarChina, ["zh-Hant", "zh"] },
            { Emoji.FlagTaiwan, ["zh-Hant", "zh"] },
            { Emoji.FlagFrance, ["fr"] },
            { Emoji.FlagGermany, ["de"] },
            { Emoji.FlagIndia, ["hi"] },
            { Emoji.FlagIreland, ["ga"] },
            { Emoji.FlagItaly, ["it"] },
            { Emoji.FlagJapan, ["ja"] },
            { Emoji.FlagSouthKorea, ["ko"] },
            { Emoji.FlagBrazil, ["pt-br", "pt"] },
            { Emoji.FlagPortugal, ["pt-pt", "pt"] },
            { Emoji.FlagRussia, ["ru"] },
            { Emoji.FlagMexico, ["es"] },
            { Emoji.FlagSpain, ["es"] },
            { Emoji.FlagVietnam, ["vi"] },
            { Emoji.FlagThailand, ["th"] },
            { Emoji.FlagUkraine, ["uk"] },
            { Emoji.FlagIndonesia, ["id"] }
        };
}
