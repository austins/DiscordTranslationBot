using Discord;

namespace DiscordTranslationBot.Discord.Events;

/// <summary>
/// Notification for the Discord SelectMenuExecuted event.
/// </summary>
public sealed class SelectMenuExecutedEvent : INotification
{
    /// <summary>
    /// The message component.
    /// </summary>
    public required IComponentInteraction MessageComponent { get; init; }
}
