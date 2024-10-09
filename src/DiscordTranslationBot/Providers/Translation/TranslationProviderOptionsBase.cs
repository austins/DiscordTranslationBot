using System.ComponentModel.DataAnnotations;

namespace DiscordTranslationBot.Providers.Translation;

/// <summary>
/// Base class for a translation provider's options.
/// </summary>
internal abstract class TranslationProviderOptionsBase : IValidatableObject
{
    /// <summary>
    /// Flag indicating whether this provider is enabled.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// The API URL for translation provider.
    /// </summary>
    public Uri? ApiUrl { get; init; }

    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Enabled && ApiUrl?.IsAbsoluteUri != true)
        {
            yield return new ValidationResult(
                $"{GetType().Name}.{nameof(ApiUrl)} is required and must be an absolute URI.",
                [nameof(ApiUrl)]);
        }
    }
}
