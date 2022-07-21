namespace DiscordTranslationBot.Configuration.TranslationProviders;

/// <summary>
/// Options for the Azure Translator provider.
/// </summary>
public sealed class AzureTranslatorOptions
{
    /// <summary>
    /// The API URL for Azure Translator.
    /// </summary>
    public Uri? ApiUrl { get; set; }

    /// <summary>
    /// The secret key for the Azure Translator API.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// The region for the Azure Translator API.
    /// </summary>
    public string Region { get; set; } = string.Empty;
}