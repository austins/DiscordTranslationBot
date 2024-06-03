using System.ComponentModel.DataAnnotations;

namespace DiscordTranslationBot.Mediator;

public sealed class MessageValidationException : Exception
{
    public MessageValidationException(string requestName, IReadOnlyList<ValidationResult> validationResults)
        : base(BuildExceptionMessage(requestName, validationResults))
    {
        ValidationResults = validationResults;
    }

    public IReadOnlyList<ValidationResult> ValidationResults { get; }

    private static string BuildExceptionMessage(string requestName, IEnumerable<ValidationResult> validationResults)
    {
        return $"Message validation failed for '{requestName}':{string.Concat(
            validationResults.Select(
                x =>
                    $"{Environment.NewLine} -- Members: '{string.Join(", ", x.MemberNames)}' with the error: '{x.ErrorMessage}'."))}";
    }
}
