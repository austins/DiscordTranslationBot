using Discord;
using Mediator;

namespace DiscordTranslationBot.Notifications;

/// <summary>
/// Notification for the Discord log event.
/// </summary>
public sealed class LogNotification : INotification
{
    /// <summary>
    /// Discord client log message.
    /// </summary>
    public required LogMessage LogMessage { get; init; }
}
