using System.ComponentModel.DataAnnotations;

namespace DiscordTranslationBot.Mediator;

public class RequestValidationException : Exception
{
    public RequestValidationException(
        string objectName,
        IReadOnlyList<ValidationResult> validationResults,
        RequestValidationExceptionType type = RequestValidationExceptionType.Request)
        : base(BuildMessage(type, objectName, validationResults))
    {
        ValidationResults = validationResults;
    }

    public IReadOnlyList<ValidationResult> ValidationResults { get; }

    private static string BuildMessage(
        RequestValidationExceptionType type,
        string objectName,
        IEnumerable<ValidationResult> validationResults)
    {
        return $"{type.ToStringFast()} validation failed for '{objectName}':{string.Concat(
            validationResults.Select(
                x =>
                    $"{Environment.NewLine} -- Members: '{string.Join(", ", x.MemberNames)}' with the error: '{x.ErrorMessage}'."))}";
    }
}
