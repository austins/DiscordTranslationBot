using DiscordTranslationBot.Models;
using NeoSmart.Unicode;

namespace DiscordTranslationBot.Services;

/// <summary>
/// Interface for <see cref="CountryService" />.
/// </summary>
public interface ICountryService
{
    /// <summary>
    /// Get a country by a flag emoji unicode string.
    /// </summary>
    /// <param name="emojiUnicode">The unicode string of the flag emoji.</param>
    /// <param name="country">The country found.</param>
    /// <returns>true if country found; false if not.</returns>
    bool TryGetCountry(string emojiUnicode, out Country? country);
}

/// <summary>
/// Maps all flag emojis to a set of <see cref="Country" /> and assigns language codes to them.
/// </summary>
/// <remarks>
/// This should be injected as a singleton (prior to the translation providers) as the list of countries
/// should only be generated once and made available to each translation provider.
/// </remarks>
public sealed partial class CountryService : ICountryService
{
    private readonly ISet<Country> _countries;
    private readonly Log _log;

    /// <summary>
    /// Initializes a new instance of the <see cref="CountryService" /> class.
    /// </summary>
    /// <param name="logger">Logger to use.</param>
    /// <exception cref="InvalidOperationException">No flag emoji found.</exception>
    public CountryService(ILogger<CountryService> logger)
    {
        _log = new Log(logger);

        // Get all flag emojis.
        var flagEmoji = Emoji.All.Where(e => e is { Group: "Flags", Subgroup: "country-flag" });

        _countries = flagEmoji.Select(
                e => new Country(e.ToString(), e.Name?.Replace("flag: ", string.Empty, StringComparison.Ordinal)))
            .ToHashSet();

        if (!_countries.Any())
        {
            _log.NoFlagEmojiFound();
            throw new InvalidOperationException("No flag emoji found.");
        }

        InitializeSupportedLangCodes();
    }

    /// <inheritdoc cref="ICountryService.TryGetCountry" />
    public bool TryGetCountry(string emojiUnicode, out Country? country)
    {
        country = _countries.SingleOrDefault(c => c.EmojiUnicode == emojiUnicode);
        return country != null;
    }

    /// <summary>
    /// Maps supported language codes to countries.
    /// </summary>
    /// <remarks>
    /// The language codes should be the primary language of the country that translation providers
    /// will translate to when the associated flag emoji is received. The language codes are based on
    /// the supported languages of each translation provider.
    /// </remarks>
    private void InitializeSupportedLangCodes()
    {
        SetLangCodes(Emoji.FlagAustralia, "en");
        SetLangCodes(Emoji.FlagCanada, "en");
        SetLangCodes(Emoji.FlagUnitedKingdom, "en");
        SetLangCodes(Emoji.FlagUnitedStates, "en");
        SetLangCodes(Emoji.FlagUsOutlyingIslands, "en");
        SetLangCodes(Emoji.FlagAlgeria, "ar");
        SetLangCodes(Emoji.FlagBahrain, "ar");
        SetLangCodes(Emoji.FlagEgypt, "ar");
        SetLangCodes(Emoji.FlagSaudiArabia, "ar");
        SetLangCodes(Emoji.FlagChina, "zh-Hans", "zh");
        SetLangCodes(Emoji.FlagHongKongSarChina, "zh-Hant", "zh");
        SetLangCodes(Emoji.FlagTaiwan, "zh-Hant", "zh");
        SetLangCodes(Emoji.FlagFrance, "fr");
        SetLangCodes(Emoji.FlagGermany, "de");
        SetLangCodes(Emoji.FlagIndia, "hi");
        SetLangCodes(Emoji.FlagIreland, "ga");
        SetLangCodes(Emoji.FlagItaly, "it");
        SetLangCodes(Emoji.FlagJapan, "ja");
        SetLangCodes(Emoji.FlagSouthKorea, "ko");
        SetLangCodes(Emoji.FlagBrazil, "pt-br", "pt");
        SetLangCodes(Emoji.FlagPortugal, "pt-pt", "pt");
        SetLangCodes(Emoji.FlagRussia, "ru");
        SetLangCodes(Emoji.FlagMexico, "es");
        SetLangCodes(Emoji.FlagSpain, "es");
        SetLangCodes(Emoji.FlagVietnam, "vi");
        SetLangCodes(Emoji.FlagThailand, "th");
        SetLangCodes(Emoji.FlagUkraine, "uk");
        SetLangCodes(Emoji.FlagIndonesia, "id");
    }

    /// <summary>
    /// Maps language codes to a country.
    /// </summary>
    /// <param name="flagEmoji">Flag emoji.</param>
    /// <param name="langCodes">Language codes to add.</param>
    /// <exception cref="InvalidOperationException">Country couldn't be found.</exception>
    private void SetLangCodes(SingleEmoji flagEmoji, params string[] langCodes)
    {
        var country = _countries.SingleOrDefault(c => c.EmojiUnicode == flagEmoji.ToString());
        if (country == null)
        {
            _log.CountryNotFound();

            throw new InvalidOperationException(
                "Country language codes couldn't be initialized as country couldn't be found.");
        }

        country.LangCodes.UnionWith(langCodes.ToHashSet(StringComparer.OrdinalIgnoreCase));
    }

    private sealed partial class Log
    {
        private readonly ILogger<CountryService> _logger;

        public Log(ILogger<CountryService> logger)
        {
            _logger = logger;
        }

        [LoggerMessage(Level = LogLevel.Critical, Message = "No flag emoji found.")]
        public partial void NoFlagEmojiFound();

        [LoggerMessage(
            Level = LogLevel.Critical,
            Message = "Country language codes couldn't be initialized as country couldn't be found.")]
        public partial void CountryNotFound();
    }
}
