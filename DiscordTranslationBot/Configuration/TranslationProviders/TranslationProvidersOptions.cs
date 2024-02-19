using System.ComponentModel.DataAnnotations;
using DiscordTranslationBot.Extensions;

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
        AzureTranslator.TryValidateObject(out var azureTranslatorValidationResults);
        LibreTranslate.TryValidateObject(out var libreTranslateValidationResults);

        return azureTranslatorValidationResults.Concat(libreTranslateValidationResults);
    }
}
