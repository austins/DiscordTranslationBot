using FluentValidation;

namespace DiscordTranslationBot.Configuration.TranslationProviders;

/// <summary>
/// The configuration options for translation providers.
/// </summary>
public sealed class TranslationProvidersOptions
{
    /// <summary>
    /// Configuration section name for <see cref="TranslationProvidersOptions" />.
    /// </summary>
    public const string SectionName = "TranslationProviders";

    /// <summary>
    /// Options for Azure Translator.
    /// </summary>
    public AzureTranslatorOptions AzureTranslator { get; set; } = new();

    /// <summary>
    /// Options for LibreTranslate.
    /// </summary>
    public LibreTranslateOptions LibreTranslate { get; set; } = new();
}

/// <summary>
/// Validator for <see cref="TranslationProvidersOptions" />.
/// </summary>
public sealed class TranslationProvidersOptionsValidator : AbstractValidator<TranslationProvidersOptions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TranslationProvidersOptionsValidator" /> class.
    /// </summary>
    public TranslationProvidersOptionsValidator()
    {
        // Validate Azure Translator options.
        When(
            x => x.AzureTranslator.Enabled,
            () =>
            {
                RuleFor(x => x.AzureTranslator.ApiUrl).NotNull();
                RuleFor(x => x.AzureTranslator.SecretKey).NotEmpty();
                RuleFor(x => x.AzureTranslator.Region).NotEmpty();
            }
        );

        // Validate Libre Translate options.
        When(x => x.LibreTranslate.Enabled, () => RuleFor(x => x.LibreTranslate.ApiUrl).NotNull());
    }
}
