using FluentValidation;

namespace DiscordTranslationBot.Configuration.TranslationProviders;

/// <summary>
/// Options for the LibreTranslate provider.
/// </summary>
public sealed class LibreTranslateOptions : TranslationProviderOptionsBase
{
}

/// <summary>
/// Validator for <see cref="LibreTranslateOptions" />.
/// </summary>
public sealed class LibreTranslateOptionsValidator : AbstractValidator<LibreTranslateOptions>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public LibreTranslateOptionsValidator()
    {
        Include(new TranslationProviderOptionsBaseValidator());
    }
}
