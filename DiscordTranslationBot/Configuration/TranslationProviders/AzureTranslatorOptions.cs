using FluentValidation;

namespace DiscordTranslationBot.Configuration.TranslationProviders;

/// <summary>
/// Options for the Azure Translator provider.
/// </summary>
public sealed class AzureTranslatorOptions : TranslationProviderOptionsBase
{
    /// <summary>
    /// The secret key for the Azure Translator API.
    /// </summary>
    public string? SecretKey { get; init; }

    /// <summary>
    /// The region for the Azure Translator API.
    /// </summary>
    public string? Region { get; init; }
}

/// <summary>
/// Validator for <see cref="AzureTranslatorOptions" />.
/// </summary>
public sealed class AzureTranslatorOptionsValidator : AbstractValidator<AzureTranslatorOptions>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public AzureTranslatorOptionsValidator()
    {
        Include(new TranslationProviderOptionsBaseValidator());

        When(
            x => x.Enabled,
            () =>
            {
                RuleFor(x => x.SecretKey).NotEmpty();
                RuleFor(x => x.Region).NotEmpty();
            });
    }
}
