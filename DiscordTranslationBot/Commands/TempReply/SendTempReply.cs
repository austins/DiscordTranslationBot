using System.ComponentModel.DataAnnotations;

namespace DiscordTranslationBot.Commands.TempReply;

/// <summary>
/// Sends a temp reply.
/// </summary>
public sealed class SendTempReply : TempReplyCommand
{
    /// <summary>
    /// The reply text.
    /// </summary>
    [Required]
    public required string Text { get; init; }

    /// <summary>
    /// The delay in seconds after which the reply will be deleted.
    /// </summary>
    [Range(0, int.MaxValue, MinimumIsExclusive = true)]
    public int DeletionDelayInSeconds { get; init; } = 10;
}
