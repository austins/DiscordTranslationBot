using FluentValidation;

namespace DiscordTranslationBot.Configuration.TranslationProviders;

/// <summary>
/// Base class for a translation provider's options.
/// </summary>
public abstract class TranslationProviderOptionsBase
{
    /// <summary>
    /// Flag indicating whether this provider is enabled.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// The API URL for Azure Translator.
    /// </summary>
    public Uri? ApiUrl { get; init; }
}

/// <summary>
/// Validator for <see cref="TranslationProviderOptionsBase" />.
/// </summary>
public sealed class TranslationProviderOptionsBaseValidator : AbstractValidator<TranslationProviderOptionsBase>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public TranslationProviderOptionsBaseValidator()
    {
        When(x => x.Enabled, () => RuleFor(x => x.ApiUrl).NotNull());
    }
}
