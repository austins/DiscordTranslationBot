using Discord;

namespace DiscordTranslationBot.Notifications;

/// <summary>
/// Notification for the Discord MessageCommandExecuted event.
/// </summary>
public sealed class MessageCommandExecutedNotification : INotification
{
    /// <summary>
    /// The slash command.
    /// </summary>
    public required IMessageCommandInteraction Command { get; init; }
}
