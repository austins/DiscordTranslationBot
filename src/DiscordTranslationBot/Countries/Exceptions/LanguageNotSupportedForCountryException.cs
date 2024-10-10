namespace DiscordTranslationBot.Countries.Exceptions;

/// <summary>
/// Exception indicating that a country doesn't have the target language code mapped and is unsupported.
/// </summary>
public sealed class LanguageNotSupportedForCountryException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LanguageNotSupportedForCountryException" /> class.
    /// </summary>
    /// <param name="message">Message for exception.</param>
    public LanguageNotSupportedForCountryException(string message)
        : base(message)
    {
    }
}
