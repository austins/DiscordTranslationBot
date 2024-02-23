using NetEscapades.EnumGenerators;

namespace DiscordTranslationBot.Mediator;

[EnumExtensions]
public enum RequestValidationExceptionType
{
    Request,
    Notification
}
