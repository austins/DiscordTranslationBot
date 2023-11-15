using Discord;
using DiscordTranslationBot.Jobs;
using FluentValidation;
using NSubstitute.ReturnsExtensions;
using Quartz;

namespace DiscordTranslationBot.Tests.Jobs;

public sealed class DeleteTempReplyForFlagEmojiReactionJobTests
{
    private readonly IDiscordClient _client;
    private readonly IJobExecutionContext _context;
    private readonly DeleteTempReplyForFlagEmojiReactionJob _sut;

    public DeleteTempReplyForFlagEmojiReactionJobTests()
    {
        _client = Substitute.For<IDiscordClient>();

        _context = Substitute.For<IJobExecutionContext>();
        _context.CancellationToken.Returns(CancellationToken.None);

        _sut = new DeleteTempReplyForFlagEmojiReactionJob(
            _client,
            new DeleteTempReplyForFlagEmojiReactionJobValidator()
        );
    }

    [Fact]
    public async Task Execute_Succeeds()
    {
        // Arrange
        _sut.GuildId = "1";
        _sut.ChannelId = "2";
        _sut.ReplyMessageId = "3";
        _sut.ReactionEmoteName = "👀";
        _sut.ReactionUserId = "4";
        _sut.SourceMessageId = "5";

        var sourceMessage = Substitute.For<IMessage>();

        var channel = Substitute.For<ITextChannel>();
        channel.GetMessageAsync(default).ReturnsForAnyArgs(sourceMessage);

        var guild = Substitute.For<IGuild>();
        guild.GetTextChannelAsync(default).ReturnsForAnyArgs(channel);

        _client.GetGuildAsync(default).ReturnsForAnyArgs(guild);

        // Act
        await _sut.Execute(_context);

        // Assert
        await sourceMessage
            .Received(1)
            .RemoveReactionAsync(Arg.Any<Emoji>(), Arg.Any<ulong>(), Arg.Any<RequestOptions>());

        await channel.Received(1).DeleteMessageAsync(Arg.Any<ulong>(), Arg.Any<RequestOptions>());
    }

    [Fact]
    public async Task Execute_NoSourceMessage_Succeeds()
    {
        // Arrange
        _sut.GuildId = "1";
        _sut.ChannelId = "2";
        _sut.ReplyMessageId = "3";
        _sut.ReactionEmoteName = "👀";
        _sut.ReactionUserId = "4";
        _sut.SourceMessageId = "5";

        var channel = Substitute.For<ITextChannel>();
        channel.GetMessageAsync(default).ReturnsNullForAnyArgs();

        var guild = Substitute.For<IGuild>();
        guild.GetTextChannelAsync(default).ReturnsForAnyArgs(channel);

        _client.GetGuildAsync(default).ReturnsForAnyArgs(guild);

        // Act
        await _sut.Execute(_context);

        // Assert
        await channel.Received(1).DeleteMessageAsync(Arg.Any<ulong>(), Arg.Any<RequestOptions>());
    }

    [Fact]
    public async Task Execute_InvalidData_Throws()
    {
        // Arrange
        _sut.GuildId = null;
        _sut.ChannelId = null;
        _sut.ReplyMessageId = null;
        _sut.ReactionEmoteName = "Not an emoji.";
        _sut.ReactionUserId = null;
        _sut.SourceMessageId = "5abc";

        // Act & Assert
        await _sut.Invoking(x => x.Execute(_context)).Should().ThrowAsync<ValidationException>();
    }
}
