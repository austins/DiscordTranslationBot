namespace DiscordTranslationBot.Exceptions;

/// <summary>
/// Exception indicating that a country couldn't be mapped to a target language code and is unsupported.
/// </summary>
public sealed class UnsupportedCountryException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedCountryException"/> class.
    /// </summary>
    public UnsupportedCountryException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedCountryException"/> class.
    /// </summary>
    /// <param name="message">Message for exception.</param>
    public UnsupportedCountryException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedCountryException"/> class.
    /// </summary>
    /// <param name="message">Message for exception.</param>
    /// <param name="innerException">Inner exception.</param>
    public UnsupportedCountryException(string message, Exception innerException)
        : base(message, innerException) { }
}
