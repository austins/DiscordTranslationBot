using Discord;
using Discord.WebSocket;

namespace DiscordTranslationBot.Models.Discord;

/// <summary>
/// Holds values from <see cref="SocketReaction" />.
/// </summary>
/// <remarks>
/// Helps with mocking reactions in tests.
/// </remarks>
public sealed class Reaction
{
    /// <summary>
    /// The ID of the user who initiated the reaction.
    /// </summary>
    public required ulong UserId { get; init; }

    /// <summary>
    /// The emote of the reaction.
    /// </summary>
    public required IEmote Emote { get; init; }
}
