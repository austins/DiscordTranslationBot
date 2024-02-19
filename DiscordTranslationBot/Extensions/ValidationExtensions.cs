using System.ComponentModel.DataAnnotations;

namespace DiscordTranslationBot.Extensions;

public static class ValidationExtensions
{
    /// <summary>
    /// Validates an object instance with any Data Annotations validation attributes, and the
    /// <see cref="IValidatableObject.Validate" /> method if it implements this.
    /// </summary>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="validationResults">The resulting validation results; empty if instance is valid.</param>
    /// <returns>
    /// true if valid or if the object has no validation attributes and isn't an <see cref="IValidatableObject" />;
    /// false if invalid.
    /// </returns>
    public static bool TryValidateObject(this object instance, out IReadOnlyList<ValidationResult> validationResults)
    {
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(instance, new ValidationContext(instance), results, true);

        validationResults = results;
        return isValid;
    }
}
