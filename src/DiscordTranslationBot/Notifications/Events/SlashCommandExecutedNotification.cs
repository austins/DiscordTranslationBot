using Discord;

namespace DiscordTranslationBot.Notifications.Events;

/// <summary>
/// Notification for the Discord SlashCommandExecuted event.
/// </summary>
public sealed class SlashCommandExecutedNotification : INotification
{
    /// <summary>
    /// The slash command interaction.
    /// </summary>
    public required ISlashCommandInteraction Interaction { get; init; }
}

public sealed class SlashCommandExecutedNotificationValidator : AbstractValidator<SlashCommandExecutedNotification>
{
    public SlashCommandExecutedNotificationValidator()
    {
        RuleFor(x => x.Interaction).NotNull();
    }
}
