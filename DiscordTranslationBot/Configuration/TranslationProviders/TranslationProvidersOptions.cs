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
    public AzureTranslatorOptions AzureTranslator { get; init; } = new();

    /// <summary>
    /// Options for LibreTranslate.
    /// </summary>
    public LibreTranslateOptions LibreTranslate { get; init; } = new();
}

/// <summary>
/// Validator for <see cref="TranslationProvidersOptions" />.
/// </summary>
public sealed class TranslationProvidersOptionsValidator : AbstractValidator<TranslationProvidersOptions>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public TranslationProvidersOptionsValidator()
    {
        RuleFor(x => x.AzureTranslator).SetValidator(new AzureTranslatorOptionsValidator());
        RuleFor(x => x.LibreTranslate).SetValidator(new LibreTranslateOptionsValidator());
    }
}
