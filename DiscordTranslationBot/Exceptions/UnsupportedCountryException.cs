namespace DiscordTranslationBot.Exceptions;

/// <summary>
/// Exception indicating that a country couldn't be mapped to a target language code and is unsupported.
/// </summary>
public sealed class UnsupportedCountryException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedCountryException" /> class.
    /// </summary>
    /// <param name="message">Message for exception.</param>
    public UnsupportedCountryException(string message)
        : base(message)
    {
    }
}
