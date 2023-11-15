using System.Globalization;
using Discord;
using Discord.WebSocket;
using DiscordTranslationBot.Extensions;
using DiscordTranslationBot.Models.Discord;
using FluentValidation;
using Quartz;

namespace DiscordTranslationBot.Jobs;

public sealed class DeleteTempReplyForFlagEmojiReactionJob : IJob
{
    private readonly IDiscordClient _client;
    private readonly IValidator<DeleteTempReplyForFlagEmojiReactionJob> _validator;

    public DeleteTempReplyForFlagEmojiReactionJob(
        IDiscordClient client,
        IValidator<DeleteTempReplyForFlagEmojiReactionJob> validator
    )
    {
        _client = client;
        _validator = validator;
    }

    public string GuildId { get; set; }

    public string ChannelId { get; set; }

    public string ReplyMessageId { get; set; }

    public string ReactionEmoteName { get; set; }

    public string ReactionUserId { get; set; }

    public string SourceMessageId { get; set; }

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

public sealed class DeleteTempReplyForFlagEmojiReactionJobValidator
    : AbstractValidator<DeleteTempReplyForFlagEmojiReactionJob>
{
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
