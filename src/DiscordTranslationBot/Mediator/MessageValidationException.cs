using System.ComponentModel.DataAnnotations;

namespace DiscordTranslationBot.Mediator;

internal sealed class MessageValidationException : Exception
{
    public MessageValidationException(string messageName, IReadOnlyList<ValidationResult> validationResults)
        : base(BuildExceptionMessage(messageName, validationResults))
    {
        ValidationResults = validationResults;
    }

    public IReadOnlyList<ValidationResult> ValidationResults { get; }

    private static string BuildExceptionMessage(string messageName, IReadOnlyList<ValidationResult> validationResults)
    {
        return $"Message validation failed for '{messageName}':{string.Concat(
            validationResults.Select(
                x =>
                    $"{Environment.NewLine} -- Members: '{string.Join(", ", x.MemberNames)}' with the error: '{x.ErrorMessage}'."))}";
    }
}
