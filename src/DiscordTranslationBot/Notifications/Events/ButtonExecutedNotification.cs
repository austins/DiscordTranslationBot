using Discord;

namespace DiscordTranslationBot.Notifications.Events;

/// <summary>
/// Notification for the Discord ButtonExecuted event.
/// </summary>
public sealed class ButtonExecutedNotification : INotification
{
    /// <summary>
    /// The button component interaction.
    /// </summary>
    public required IComponentInteraction Interaction { get; init; }
}

public sealed class ButtonExecutedNotificationValidator : AbstractValidator<ButtonExecutedNotification>
{
    public ButtonExecutedNotificationValidator()
    {
        RuleFor(x => x.Interaction).NotNull();
    }
}
