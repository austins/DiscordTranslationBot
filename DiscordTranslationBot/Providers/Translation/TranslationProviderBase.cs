using DiscordTranslationBot.Exceptions;
using DiscordTranslationBot.Models;
using DiscordTranslationBot.Models.Providers.Translation;

namespace DiscordTranslationBot.Providers.Translation;

/// <summary>
/// Base class for translation providers.
/// </summary>
public abstract class TranslationProviderBase
{
    /// <summary>
    /// The name of the translation provider.
    /// </summary>
    public abstract string ProviderName { get; }

    /// <summary>
    /// Supported language codes for the provider.
    /// </summary>
#pragma warning disable CA2227
    protected ISet<string> SupportedLangCodes { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
#pragma warning restore CA2227

    /// <summary>
    /// Initialize the <see cref="SupportedLangCodes"/> for the provider.
    /// </summary>
    /// <remarks>
    /// This is called for each provider in <see cref="Worker.StartAsync"/> when the application starts up.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    public abstract Task InitializeSupportedLangCodesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Translate text.
    /// </summary>
    /// <param name="country">The country containing language codes to translate to.</param>
    /// <param name="text">The text to translate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Translated text.</returns>
    public abstract Task<TranslationResult> TranslateAsync(Country country, string text, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the lang code that a country supports.
    /// </summary>
    /// <param name="country">The country containing language codes it supports.</param>
    /// <returns>Lang code.</returns>
    /// <exception cref="UnsupportedCountryException">Country not supported.</exception>
    protected string GetLangCodeByCountry(Country country)
    {
        return country.LangCodes.FirstOrDefault(countryLangCode => SupportedLangCodes.Contains(countryLangCode))
               ?? throw new UnsupportedCountryException($"Translation for country {country.Name} isn't supported.");
    }
}
