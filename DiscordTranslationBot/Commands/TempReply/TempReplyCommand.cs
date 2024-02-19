using System.ComponentModel.DataAnnotations;
using Discord;
using DiscordTranslationBot.Models.Discord;

namespace DiscordTranslationBot.Commands.TempReply;

/// <summary>
/// Interface for a temp reply command.
/// </summary>
public abstract class TempReplyCommand : IRequest
{
    /// <summary>
    /// The reaction associated with the source message, if any.
    /// </summary>
    public Reaction? Reaction { get; init; }

    /// <summary>
    /// The source message.
    /// </summary>
    [Required]
    public required IMessage SourceMessage { get; init; }
}
