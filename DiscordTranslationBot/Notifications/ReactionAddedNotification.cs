using Discord;
using DiscordTranslationBot.Models.Discord;
using MediatR;

namespace DiscordTranslationBot.Notifications;

/// <summary>
/// Notification for the Discord ReactionAdded event.
/// </summary>
public sealed class ReactionAddedNotification : INotification
{
    /// <summary>
    /// The user message.
    /// </summary>
    public Task<IUserMessage> Message { get; set; } = null!;

    /// <summary>
    /// The message channel.
    /// </summary>
    public Task<IMessageChannel> Channel { get; set; } = null!;

    /// <summary>
    /// The reaction.
    /// </summary>
    public Reaction Reaction { get; set; } = null!;
}