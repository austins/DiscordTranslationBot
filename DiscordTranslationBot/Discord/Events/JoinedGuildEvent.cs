using Discord;

namespace DiscordTranslationBot.Discord.Events;

/// <summary>
/// Notification for the Discord joined guild event.
/// </summary>
public sealed class JoinedGuildEvent : INotification
{
    /// <summary>
    /// The guild that the bot joined.
    /// </summary>
    public required IGuild Guild { get; init; }
}
