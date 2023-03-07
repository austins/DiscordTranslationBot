namespace DiscordTranslationBot.Configuration.TranslationProviders;

/// <summary>
/// Base class for a translation provider's options.
/// </summary>
public abstract class TranslationProviderOptionsBase
{
    /// <summary>
    /// Flag indicating whether this provider is enabled.
    /// </summary>
    public bool Enabled { get; init; }
}
