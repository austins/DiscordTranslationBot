using System.ComponentModel.DataAnnotations;
using Discord;
using DiscordTranslationBot.Models.Discord;

namespace DiscordTranslationBot.Commands.TempReplies;

/// <summary>
/// Sends a temp reply.
/// </summary>
public sealed class SendTempReply : IRequest, IValidatableObject
{
    /// <summary>
    /// The source message.
    /// </summary>
    [Required]
    public required IUserMessage SourceMessage { get; init; }

    /// <summary>
    /// The reaction associated with the source message, if any.
    /// </summary>
    public Reaction? Reaction { get; init; }

    /// <summary>
    /// The reply text.
    /// </summary>
    [Required]
    public required string Text { get; init; }

    /// <summary>
    /// The delay after which the reply will be deleted.
    /// </summary>
    public TimeSpan DeletionDelay { get; init; } = TimeSpan.FromSeconds(10);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DeletionDelay <= TimeSpan.Zero)
        {
            yield return new ValidationResult("Deletion delay must be greater than 0.", [nameof(DeletionDelay)]);
        }
    }
}
