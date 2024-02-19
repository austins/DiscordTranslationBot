using System.ComponentModel.DataAnnotations;

namespace DiscordTranslationBot.Tests;

public abstract class ValidationTestBase
{
    protected static (IReadOnlyList<ValidationResult> Results, bool IsValid) ValidateObject(object instance)
    {
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(instance, new ValidationContext(instance), results, true);

        return (results, isValid);
    }
}
