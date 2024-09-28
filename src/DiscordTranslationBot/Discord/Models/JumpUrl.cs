﻿namespace DiscordTranslationBot.Discord.Models;

public sealed class JumpUrl
{
    /// <summary>
    /// Indicates if this jump URL is for a direct-message channel.
    /// </summary>
    public required bool IsDmChannel { get; init; }

    /// <summary>
    /// Guild ID. Not null if <see cref="IsDmChannel" /> is false.
    /// </summary>
    public required ulong? GuildId { get; init; }

    /// <summary>
    /// Channel ID.
    /// </summary>
    public required ulong ChannelId { get; init; }

    /// <summary>
    /// Message ID.
    /// </summary>
    public required ulong MessageId { get; init; }
}
