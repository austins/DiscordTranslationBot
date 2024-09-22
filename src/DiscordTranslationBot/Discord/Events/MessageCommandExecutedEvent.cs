using Discord;

namespace DiscordTranslationBot.Discord.Events;

/// <summary>
/// Notification for the Discord MessageCommandExecuted event.
/// </summary>
public sealed class MessageCommandExecutedEvent : INotification
{
    /// <summary>
    /// The message command.
    /// </summary>
    public required IMessageCommandInteraction MessageCommand { get; init; }
}
