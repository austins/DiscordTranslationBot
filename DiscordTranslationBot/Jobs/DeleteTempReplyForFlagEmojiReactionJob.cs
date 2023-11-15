using System.Globalization;
using Discord;
using DiscordTranslationBot.Extensions;
using DiscordTranslationBot.Models.Discord;
using FluentValidation;
using Quartz;

namespace DiscordTranslationBot.Jobs;

/// <summary>
/// Job that deletes a temporary reply message and clears the associated reaction when a user reacts to a message with a flag emoji.
/// </summary>
public sealed class DeleteTempReplyForFlagEmojiReactionJob : IJob
{
    private readonly IDiscordClient _client;
    private readonly IValidator<DeleteTempReplyForFlagEmojiReactionJob> _validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteTempReplyForFlagEmojiReactionJob"/> class.
    /// </summary>
    /// <param name="client">Discord client to use.</param>
    /// <param name="validator">Validator to use.</param>
    public DeleteTempReplyForFlagEmojiReactionJob(
        IDiscordClient client,
        IValidator<DeleteTempReplyForFlagEmojiReactionJob> validator
    )
    {
        _client = client;
        _validator = validator;
    }

    /// <summary>
    /// The Guild ID.
    /// </summary>
    public string GuildId { get; set; } = null!;

    /// <summary>
    /// The channel ID.
    /// </summary>
    public string ChannelId { get; set; } = null!;

    /// <summary>
    /// The reply message ID.
    /// </summary>
    public string ReplyMessageId { get; set; } = null!;

    /// <summary>
    /// The reaction emoji.
    /// </summary>
    public string ReactionEmoteName { get; set; } = null!;

    /// <summary>
    /// The user ID of the user who made the reaction.
    /// </summary>
    public string ReactionUserId { get; set; } = null!;

    /// <summary>
    /// The source message ID.
    /// </summary>
    public string SourceMessageId { get; set; } = null!;

    /// <summary>
    /// Handles the job.
    /// </summary>
    /// <param name="context">The job execution context.</param>
    public async Task Execute(IJobExecutionContext context)
    {
        await _validator.ValidateAndThrowAsync(this, context.CancellationToken);

        var guild = await _client.GetGuildAsync(
            Convert.ToUInt64(GuildId, CultureInfo.InvariantCulture),
            options: new RequestOptions { CancelToken = context.CancellationToken }
        );

        var channel = await guild.GetTextChannelAsync(
            Convert.ToUInt64(ChannelId, CultureInfo.InvariantCulture),
            options: new RequestOptions { CancelToken = context.CancellationToken }
        );

        // If the source message still exists, remove the reaction from it.
        var sourceMessage = await channel.GetMessageAsync(
            Convert.ToUInt64(SourceMessageId, CultureInfo.InvariantCulture),
            options: new RequestOptions { CancelToken = context.CancellationToken }
        );

        if (sourceMessage != null)
        {
            await sourceMessage.RemoveReactionAsync(
                new Emoji(ReactionEmoteName),
                Convert.ToUInt64(ReactionUserId, CultureInfo.InvariantCulture),
                new RequestOptions { CancelToken = context.CancellationToken }
            );
        }

        // Delete the reply message.
        await channel.DeleteMessageAsync(
            Convert.ToUInt64(ReplyMessageId, CultureInfo.InvariantCulture),
            new RequestOptions { CancelToken = context.CancellationToken }
        );
    }

    /// <summary>
    /// Creates a job detail to be used by the scheduler.
    /// </summary>
    /// <param name="reply">The reply message.</param>
    /// <param name="reaction">The reaction.</param>
    /// <param name="sourceMessage">The source message.</param>
    /// <returns>A job detail.</returns>
    public static IJobDetail Create(IMessage reply, Reaction reaction, IMessage sourceMessage)
    {
        return JobBuilder
            .Create<DeleteTempReplyForFlagEmojiReactionJob>()
            .UsingJobData(
                nameof(GuildId),
                (sourceMessage.Channel as IGuildChannel)!.Guild.Id.ToString(CultureInfo.InvariantCulture)
            )
            .UsingJobData(nameof(ChannelId), sourceMessage.Channel.Id.ToString(CultureInfo.InvariantCulture))
            .UsingJobData(nameof(ReplyMessageId), reply.Id.ToString(CultureInfo.InvariantCulture))
            .UsingJobData(nameof(ReactionEmoteName), reaction.Emote.Name)
            .UsingJobData(nameof(ReactionUserId), reaction.UserId.ToString(CultureInfo.InvariantCulture))
            .UsingJobData(nameof(SourceMessageId), sourceMessage.Id.ToString(CultureInfo.InvariantCulture))
            .Build();
    }
}

/// <summary>
/// Validator for <see cref="DeleteTempReplyForFlagEmojiReactionJob"/>.
/// </summary>
public sealed class DeleteTempReplyForFlagEmojiReactionJobValidator
    : AbstractValidator<DeleteTempReplyForFlagEmojiReactionJob>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteTempReplyForFlagEmojiReactionJobValidator"/> class.
    /// </summary>
    public DeleteTempReplyForFlagEmojiReactionJobValidator()
    {
        RuleFor(x => x.GuildId).PositiveUInt64();
        RuleFor(x => x.ChannelId).PositiveUInt64();
        RuleFor(x => x.ReplyMessageId).PositiveUInt64();
        RuleFor(x => x.ReactionEmoteName).NotEmpty().Must(x => NeoSmart.Unicode.Emoji.IsEmoji(x));
        RuleFor(x => x.ReactionUserId).PositiveUInt64();
        RuleFor(x => x.SourceMessageId).PositiveUInt64();
    }
}
