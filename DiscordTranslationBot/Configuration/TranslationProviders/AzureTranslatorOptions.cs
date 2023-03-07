namespace DiscordTranslationBot.Configuration.TranslationProviders;

/// <summary>
/// Options for the Azure Translator provider.
/// </summary>
public sealed class AzureTranslatorOptions
{
    /// <summary>
    /// The API URL for Azure Translator.
    /// </summary>
    public Uri? ApiUrl { get; init; }

    /// <summary>
    /// The secret key for the Azure Translator API.
    /// </summary>
    public required string SecretKey { get; init; }

    /// <summary>
    /// The region for the Azure Translator API.
    /// </summary>
    public required string Region { get; init; }
}
