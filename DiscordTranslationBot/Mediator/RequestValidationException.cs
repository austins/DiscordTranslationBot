using System.ComponentModel.DataAnnotations;

namespace DiscordTranslationBot.Mediator;

public sealed class RequestValidationException : Exception
{
    public RequestValidationException(
        string requestName,
        IReadOnlyList<ValidationResult> validationResults,
        Exception? innerException = null)
        : base(BuildMessage(requestName, validationResults), innerException)
    {
        ValidationResults = validationResults;
    }

    public IReadOnlyList<ValidationResult> ValidationResults { get; }

    private static string BuildMessage(string requestName, IReadOnlyList<ValidationResult> validationResults)
    {
        return $"Request validation failed for '{requestName}':{string.Concat(
            validationResults.Select(
                x =>
                    $"{Environment.NewLine} -- Members: '{string.Join(", ", x.MemberNames)}' with the error: '{x.ErrorMessage}'."))}";
    }
}
