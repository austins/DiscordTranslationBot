using System.ComponentModel.DataAnnotations;

namespace DiscordTranslationBot.Extensions;

public static class ValidationExtensions
{
    public static bool TryValidateObject(this object instance, out IReadOnlyList<ValidationResult> validationResults)
    {
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(instance, new ValidationContext(instance), results, true);

        validationResults = results;
        return isValid;
    }
}
