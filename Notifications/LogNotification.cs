using Discord;
using MediatR;

namespace DiscordTranslationBot.Notifications;

/// <summary>
/// Notification for the Discord log event.
/// </summary>
internal sealed class LogNotification : INotification
{
    /// <summary>
    /// Discord client log message.
    /// </summary>
    public LogMessage LogMessage { get; set; }
}