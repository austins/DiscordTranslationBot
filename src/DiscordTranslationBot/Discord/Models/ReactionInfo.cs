using Discord;
using Discord.WebSocket;

namespace DiscordTranslationBot.Discord.Models;

/// <summary>
/// Holds values from <see cref="SocketReaction" />.
/// </summary>
/// <remarks>
/// Helps with mocking reactions in tests because <see cref="SocketReaction" /> has an internal constructor
/// and <see cref="IReaction" /> does not have the UserId.
/// </remarks>
internal sealed class ReactionInfo
{
    /// <summary>
    /// The ID of the user who initiated the reaction.
    /// </summary>
    public required ulong UserId { get; init; }

    /// <summary>
    /// The emote of the reaction.
    /// </summary>
    public required IEmote Emote { get; init; }

    /// <summary>
    /// Whether the user who initiated the reaction is a bot.
    /// </summary>
    public bool IsBot { get; init; }

    public static ReactionInfo FromSocketReaction(SocketReaction socketReaction)
    {
        return new ReactionInfo
        {
            UserId = socketReaction.UserId,
            Emote = socketReaction.Emote,
            IsBot = socketReaction.User is { IsSpecified: true, Value.IsBot: true }
        };
    }
}
