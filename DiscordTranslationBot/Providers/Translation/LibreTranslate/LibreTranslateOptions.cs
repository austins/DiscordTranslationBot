using FluentValidation;

namespace DiscordTranslationBot.Providers.Translation.LibreTranslate;

/// <summary>
/// Options for the LibreTranslate provider.
/// </summary>
public sealed class LibreTranslateOptions : TranslationProviderOptionsBase
{
}

public sealed class LibreTranslateOptionsValidator : AbstractValidator<LibreTranslateOptions>
{
    public LibreTranslateOptionsValidator()
    {
        Include(new TranslationProviderOptionsBaseValidator());
    }
}
