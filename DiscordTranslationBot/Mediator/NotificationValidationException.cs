using System.ComponentModel.DataAnnotations;

namespace DiscordTranslationBot.Mediator;

public sealed class NotificationValidationException : RequestValidationException
{
    public NotificationValidationException(string objectName, IReadOnlyList<ValidationResult> validationResults)
        : base(objectName, validationResults, RequestValidationExceptionType.Notification)
    {
    }
}
