using Discord;
using Discord.WebSocket;

namespace DiscordTranslationBot.Discord.Models;

/// <summary>
/// Holds values from <see cref="SocketReaction" />.
/// </summary>
/// <remarks>
/// Helps with mocking reactions in tests.
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

    public static ReactionInfo FromSocketReaction(SocketReaction socketReaction)
    {
        return new ReactionInfo
        {
            UserId = socketReaction.UserId,
            Emote = socketReaction.Emote
        };
    }
}
