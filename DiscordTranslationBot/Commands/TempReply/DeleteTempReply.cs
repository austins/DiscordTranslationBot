using System.ComponentModel.DataAnnotations;
using Discord;
using DiscordTranslationBot.Mediator;

namespace DiscordTranslationBot.Commands.TempReply;

/// <summary>
/// Command to delete a temp reply.
/// </summary>
public sealed class DeleteTempReply : TempReplyCommand, IBackgroundCommand
{
    /// <summary>
    /// The reply message to delete.
    /// </summary>
    [Required]
    public required IMessage Reply { get; init; }

    /// <inheritdoc cref="IBackgroundCommand.Delay" />
    public TimeSpan? Delay { get; init; }
}
