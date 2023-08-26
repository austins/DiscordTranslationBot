using Discord;

namespace DiscordTranslationBot.Notifications;

/// <summary>
/// Notification for the Discord log event.
/// </summary>
public sealed class LogNotification : INotification
{
    /// <summary>
    /// The Discord log message.
    /// </summary>
    public required LogMessage LogMessage { get; init; }
}
