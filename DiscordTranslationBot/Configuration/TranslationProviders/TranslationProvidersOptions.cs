using System.ComponentModel.DataAnnotations;

namespace DiscordTranslationBot.Configuration.TranslationProviders;

/// <summary>
/// The configuration options for translation providers.
/// </summary>
public sealed class TranslationProvidersOptions : IValidatableObject
{
    /// <summary>
    /// Configuration section name for <see cref="TranslationProvidersOptions" />.
    /// </summary>
    public const string SectionName = "TranslationProviders";

    /// <summary>
    /// Options for Azure Translator.
    /// </summary>
    public AzureTranslatorOptions AzureTranslator { get; init; } = new();

    /// <summary>
    /// Options for LibreTranslate.
    /// </summary>
    public LibreTranslateOptions LibreTranslate { get; init; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(AzureTranslator, new ValidationContext(AzureTranslator), results, true);
        Validator.TryValidateObject(LibreTranslate, new ValidationContext(LibreTranslate), results, true);

        return results;
    }
}
