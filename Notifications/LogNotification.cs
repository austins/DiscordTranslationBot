using Discord;
using MediatR;

namespace DiscordTranslationBot.Notifications;

internal sealed class LogNotification : INotification
{
    public LogMessage LogMessage { get; set; }
}