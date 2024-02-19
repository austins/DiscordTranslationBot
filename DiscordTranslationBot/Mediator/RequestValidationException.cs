using System.ComponentModel.DataAnnotations;

namespace DiscordTranslationBot.Mediator;

public sealed class RequestValidationException : Exception
{
    public RequestValidationException(
        string requestName,
        IReadOnlyList<ValidationResult> results,
        Exception? innerException = null)
        : base(BuildMessage(requestName, results), innerException)
    {
        Results = results;
    }

    public IReadOnlyList<ValidationResult> Results { get; }

    private static string BuildMessage(string requestName, IReadOnlyList<ValidationResult> results)
    {
        return $"Request validation failed for '{requestName}':{string.Concat(
            results.Select(
                x =>
                    $"{Environment.NewLine} -- Members: '{string.Join(", ", x.MemberNames)}' with the error: '{x.ErrorMessage}'."))}";
    }
}
