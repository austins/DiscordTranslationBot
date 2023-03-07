namespace DiscordTranslationBot.Configuration.TranslationProviders;

/// <summary>
/// Options for the LibreTranslate provider.
/// </summary>
public sealed class LibreTranslateOptions : TranslationProviderOptionsBase
{
    /// <summary>
    /// The API URL for LibreTranslate.
    /// </summary>
    public Uri? ApiUrl { get; init; }
}
