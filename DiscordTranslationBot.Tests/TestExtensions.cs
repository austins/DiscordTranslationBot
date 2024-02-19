using System.ComponentModel.DataAnnotations;

namespace DiscordTranslationBot.Tests;

internal static class TestExtensions
{
    public static (IReadOnlyList<ValidationResult> Results, bool IsValid) ValidateObject(this object instance)
    {
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(instance, new ValidationContext(instance), results, true);

        return (results, isValid);
    }
}
