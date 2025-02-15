using Discord;

namespace DiscordTranslationBot.Notifications.Events;

/// <summary>
/// Notification for the Discord MessageCommandExecuted event.
/// </summary>
public sealed class MessageCommandExecutedNotification : INotification
{
    /// <summary>
    /// The message command interaction.
    /// </summary>
    public required IMessageCommandInteraction Interaction { get; init; }
}

public sealed class MessageCommandExecutedNotificationValidator : AbstractValidator<MessageCommandExecutedNotification>
{
    public MessageCommandExecutedNotificationValidator()
    {
        RuleFor(x => x.Interaction).NotNull();
    }
}
