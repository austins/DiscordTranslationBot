using System.ComponentModel.DataAnnotations;

namespace DiscordTranslationBot.Extensions;

internal static class ValidationExtensions
{
    /// <summary>
    /// Validates an object instance with any Data Annotations validation attributes, and the
    /// <see cref="IValidatableObject.Validate" /> method if it implements this.
    /// </summary>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="validationResults">All validation results if instance isn't valid; empty if instance is valid.</param>
    /// <returns>
    /// <see langword="true" /> if valid or if the object has no validation attributes and
    /// isn't an <see cref="IValidatableObject" />; <see langword="false" /> if invalid.
    /// </returns>
    public static bool TryValidate(this object instance, out IReadOnlyList<ValidationResult> validationResults)
    {
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(instance, new ValidationContext(instance), results, true);

        validationResults = results;
        return isValid;
    }
}
