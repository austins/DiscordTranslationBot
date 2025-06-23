using System.ComponentModel.DataAnnotations;

namespace DiscordTranslationBot.Providers.Translation.AzureTranslator;

/// <summary>
/// Options for the Azure Translator provider.
/// </summary>
internal sealed class AzureTranslatorOptions : TranslationProviderOptionsBase
{
    /// <summary>
    /// The secret key for the Azure Translator API.
    /// </summary>
    public string? SecretKey { get; init; }

    /// <summary>
    /// The region for the Azure Translator API.
    /// </summary>
    public string? Region { get; init; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validationResults = base.Validate(validationContext).ToList();

        if (Enabled)
        {
            if (string.IsNullOrWhiteSpace(SecretKey))
            {
                validationResults.Add(
                    new ValidationResult(
                        $"{nameof(AzureTranslatorOptions)}.{nameof(SecretKey)} is required.",
                        [nameof(SecretKey)]));
            }

            if (string.IsNullOrWhiteSpace(Region))
            {
                validationResults.Add(
                    new ValidationResult(
                        $"{nameof(AzureTranslatorOptions)}.{nameof(Region)} is required.",
                        [nameof(Region)]));
            }
        }

        return validationResults;
    }
}
