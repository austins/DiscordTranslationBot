using Discord;

namespace DiscordTranslationBot.Notifications.Events;

/// <summary>
/// Notification for the Discord joined guild event.
/// </summary>
public sealed class JoinedGuildNotification : INotification
{
    /// <summary>
    /// The guild that the bot joined.
    /// </summary>
    public required IGuild Guild { get; init; }
}

public sealed class JoinedGuildNotificationValidator : AbstractValidator<JoinedGuildNotification>
{
    public JoinedGuildNotificationValidator()
    {
        RuleFor(x => x.Guild).NotNull();
    }
}
