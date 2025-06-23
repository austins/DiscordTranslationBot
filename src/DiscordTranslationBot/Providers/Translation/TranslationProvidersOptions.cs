using DiscordTranslationBot.Extensions;
using DiscordTranslationBot.Providers.Translation.AzureTranslator;
using DiscordTranslationBot.Providers.Translation.LibreTranslate;
using System.ComponentModel.DataAnnotations;

namespace DiscordTranslationBot.Providers.Translation;

/// <summary>
/// The configuration options for translation providers.
/// </summary>
internal sealed class TranslationProvidersOptions : IValidatableObject
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
        AzureTranslator.TryValidate(out var azureTranslatorValidationResults);
        LibreTranslate.TryValidate(out var libreTranslateValidationResults);

        return [.. azureTranslatorValidationResults, .. libreTranslateValidationResults];
    }
}
