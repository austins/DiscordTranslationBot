namespace DiscordTranslationBot.Providers.Translation.Exceptions;

/// <summary>
/// Exception indicating that an attempt to translate has failed.
/// </summary>
public sealed class TranslationFailureException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TranslationFailureException" /> class.
    /// </summary>
    /// <param name="providerName">The name of the translation provider that failed.</param>
    /// <param name="innerException">The inner exception.</param>
    public TranslationFailureException(string providerName, Exception innerException)
        : base($"Failed to translate text with {providerName}.", innerException)
    {
        ProviderName = providerName;
    }

    public string ProviderName { get; }
}
