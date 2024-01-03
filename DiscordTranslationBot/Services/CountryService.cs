using System.Diagnostics.CodeAnalysis;
using DiscordTranslationBot.Constants;
using DiscordTranslationBot.Models;
using NeoSmart.Unicode;

namespace DiscordTranslationBot.Services;

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
        _countries = Emoji.All.Where(e => e is { Group: "Flags", Subgroup: "country-flag" })
            .Select(e => new Country(e.ToString()!, e.Name?.Replace("flag: ", string.Empty, StringComparison.Ordinal)))
            .ToHashSet();

        if (!_countries.Any())
        {
            _log.NoFlagEmojiFound();
            throw new InvalidOperationException("No flag emoji found.");
        }

        InitializeSupportedLangCodes();
    }

    /// <inheritdoc cref="ICountryService.TryGetCountry" />
    public bool TryGetCountry(string emojiUnicode, [NotNullWhen(true)] out Country? country)
    {
        country = _countries.SingleOrDefault(c => c.EmojiUnicode == emojiUnicode);
        return country != null;
    }

    /// <summary>
    /// Maps supported language codes to a country.
    /// </summary>
    /// <exception cref="InvalidOperationException">Country couldn't be found.</exception>
    private void InitializeSupportedLangCodes()
    {
        foreach (var (flagEmoji, langCodes) in CountryConstants.LangCodeMap)
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
