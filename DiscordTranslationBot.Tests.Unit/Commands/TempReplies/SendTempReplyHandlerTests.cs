using System.Net;
using Discord;
using Discord.Net;
using DiscordTranslationBot.Commands.TempReplies;
using DiscordTranslationBot.Discord.Models;

namespace DiscordTranslationBot.Tests.Unit.Commands.TempReplies;

public sealed class SendTempReplyHandlerTests
{
    private readonly SendTempReplyHandler _sut = new(new LoggerFake<SendTempReplyHandler>());

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_SendTempReply_Success(bool hasReaction)
    {
        // Arrange
        var command = new SendTempReply
        {
            Text = "test",
            ReactionInfo = hasReaction
                ? new ReactionInfo
                {
                    UserId = 1,
                    Emote = Substitute.For<IEmote>()
                }
                : null,
            SourceMessage = Substitute.For<IUserMessage>(),
            DeletionDelay = TimeSpan.FromTicks(1)
        };

        var reply = Substitute.For<IUserMessage>();
        command.SourceMessage.Channel.SendMessageAsync().ReturnsForAnyArgs(reply);

        if (hasReaction)
        {
            reply
                .Channel
                .GetMessageAsync(
                    Arg.Is<ulong>(x => x == command.SourceMessage.Id),
                    Arg.Any<CacheMode>(),
                    Arg.Any<RequestOptions>())
                .Returns(command.SourceMessage);
        }

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        command.SourceMessage.Channel.ReceivedWithAnyArgs(1).EnterTypingState();

        await command.SourceMessage.Channel.ReceivedWithAnyArgs(1).SendMessageAsync();

        await reply
            .Channel
            .Received(hasReaction ? 1 : 0)
            .GetMessageAsync(
                Arg.Is<ulong>(x => x == command.SourceMessage.Id),
                Arg.Any<CacheMode>(),
                Arg.Any<RequestOptions>());

        await command
            .SourceMessage
            .Received(hasReaction ? 1 : 0)
            .RemoveReactionAsync(Arg.Any<IEmote>(), Arg.Any<ulong>(), Arg.Any<RequestOptions>());

        await reply.ReceivedWithAnyArgs(1).DeleteAsync();
    }

    [Fact]
    public async Task Handle_SendTempReply_Success_TempReplyAlreadyDeleted()
    {
        // Arrange
        var command = new SendTempReply
        {
            Text = "test",
            ReactionInfo = null,
            SourceMessage = Substitute.For<IUserMessage>(),
            DeletionDelay = TimeSpan.FromTicks(1)
        };

        var reply = Substitute.For<IUserMessage>();
        command.SourceMessage.Channel.SendMessageAsync().ReturnsForAnyArgs(reply);

        reply
            .DeleteAsync(Arg.Any<RequestOptions>())
            .ThrowsAsync(
                new HttpException(
                    HttpStatusCode.NotFound,
                    Substitute.For<IRequest>(),
                    DiscordErrorCode.UnknownMessage));

        // Act & Assert
        await _sut.Awaiting(x => x.Handle(command, CancellationToken.None)).Should().NotThrowAsync();

        command.SourceMessage.Channel.ReceivedWithAnyArgs(1).EnterTypingState();

        await command.SourceMessage.Channel.ReceivedWithAnyArgs(1).SendMessageAsync();

        await reply.ReceivedWithAnyArgs(1).DeleteAsync();
    }
}
