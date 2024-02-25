using System.ComponentModel.DataAnnotations;

namespace DiscordTranslationBot.Mediator;

public class RequestValidationException : Exception
{
    public RequestValidationException(string requestName, IReadOnlyList<ValidationResult> validationResults)
        : base(BuildMessage(requestName, validationResults))
    {
        ValidationResults = validationResults;
    }

    public IReadOnlyList<ValidationResult> ValidationResults { get; }

    private static string BuildMessage(string requestName, IEnumerable<ValidationResult> validationResults)
    {
        return $"Request validation failed for '{requestName}':{string.Concat(
            validationResults.Select(
                x =>
                    $"{Environment.NewLine} -- Members: '{string.Join(", ", x.MemberNames)}' with the error: '{x.ErrorMessage}'."))}";
    }
}
