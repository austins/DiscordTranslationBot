using FluentValidation;

namespace DiscordTranslationBot.Providers.Translation;

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
    /// The API URL for translation provider.
    /// </summary>
    public Uri? ApiUrl { get; init; }
}

public sealed class TranslationProviderOptionsBaseValidator : AbstractValidator<TranslationProviderOptionsBase>
{
    public TranslationProviderOptionsBaseValidator()
    {
        When(x => x.Enabled, () => RuleFor(x => x.ApiUrl).Must(x => x?.IsAbsoluteUri == true));
    }
}
