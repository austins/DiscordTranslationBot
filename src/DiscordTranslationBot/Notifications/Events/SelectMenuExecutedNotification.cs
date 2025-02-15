using Discord;

namespace DiscordTranslationBot.Notifications.Events;

/// <summary>
/// Notification for the Discord SelectMenuExecuted event.
/// </summary>
public sealed class SelectMenuExecutedNotification : INotification
{
    /// <summary>
    /// The select menu component interaction.
    /// </summary>
    public required IComponentInteraction Interaction { get; init; }
}

public sealed class SelectMenuExecutedNotificationValidator : AbstractValidator<SelectMenuExecutedNotification>
{
    public SelectMenuExecutedNotificationValidator()
    {
        RuleFor(x => x.Interaction).NotNull();
    }
}
