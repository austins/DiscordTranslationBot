using Discord;
using DiscordTranslationBot.Discord.Models;

namespace DiscordTranslationBot.Notifications.Events;

/// <summary>
/// Notification for the Discord ReactionAdded event.
/// </summary>
public sealed class ReactionAddedNotification : INotification
{
    /// <summary>
    /// The user message.
    /// </summary>
    public required IUserMessage Message { get; init; }

    /// <summary>
    /// The channel.
    /// </summary>
    public required IMessageChannel Channel { get; init; }

    /// <summary>
    /// The reaction.
    /// </summary>
    public required ReactionInfo ReactionInfo { get; init; }
}

public sealed class ReactionAddedNotificationValidator : AbstractValidator<ReactionAddedNotification>
{
    public ReactionAddedNotificationValidator()
    {
        RuleFor(x => x.Message).NotNull();
        RuleFor(x => x.Channel).NotNull();
        RuleFor(x => x.ReactionInfo).NotNull();
    }
}
