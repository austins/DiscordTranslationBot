using System.Net;
using Discord;
using Discord.Net;
using DiscordTranslationBot.Commands.TempReplies;
using ReactionMetadata = DiscordTranslationBot.Discord.Models.ReactionMetadata;

namespace DiscordTranslationBot.Tests.Handlers;

public sealed class SendTempReplyHandlerTests
{
    private readonly SendTempReplyHandler _sut;

    public SendTempReplyHandlerTests()
    {
        _sut = new SendTempReplyHandler(new LoggerFake<SendTempReplyHandler>());
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Handle_SendTempReply_Success(bool hasReaction)
    {
        // Arrange
        var request = new SendTempReply
        {
            Text = "test",
            ReactionMetadata = hasReaction
                ? new ReactionMetadata
                {
                    UserId = 1,
                    Emote = Substitute.For<IEmote>()
                }
                : null,
            SourceMessage = Substitute.For<IUserMessage>(),
            DeletionDelay = TimeSpan.FromTicks(1)
        };

        var reply = Substitute.For<IUserMessage>();
        request.SourceMessage.Channel.SendMessageAsync().ReturnsForAnyArgs(reply);

        if (hasReaction)
        {
            reply.Channel.GetMessageAsync(
                    Arg.Is<ulong>(x => x == request.SourceMessage.Id),
                    Arg.Any<CacheMode>(),
                    Arg.Any<RequestOptions>())
                .Returns(request.SourceMessage);
        }

        // Act
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        request.SourceMessage.Channel.ReceivedWithAnyArgs(1).EnterTypingState();

        await request.SourceMessage.Channel.ReceivedWithAnyArgs(1).SendMessageAsync();

        await reply.Channel.Received(hasReaction ? 1 : 0)
            .GetMessageAsync(
                Arg.Is<ulong>(x => x == request.SourceMessage.Id),
                Arg.Any<CacheMode>(),
                Arg.Any<RequestOptions>());

        await request.SourceMessage.Received(hasReaction ? 1 : 0)
            .RemoveReactionAsync(Arg.Any<IEmote>(), Arg.Any<ulong>(), Arg.Any<RequestOptions>());

        await reply.ReceivedWithAnyArgs(1).DeleteAsync();
    }

    [Test]
    public async Task Handle_SendTempReply_Success_TempReplyAlreadyDeleted()
    {
        // Arrange
        var request = new SendTempReply
        {
            Text = "test",
            ReactionMetadata = null,
            SourceMessage = Substitute.For<IUserMessage>(),
            DeletionDelay = TimeSpan.FromTicks(1)
        };

        var reply = Substitute.For<IUserMessage>();
        request.SourceMessage.Channel.SendMessageAsync().ReturnsForAnyArgs(reply);

        reply.DeleteAsync(Arg.Any<RequestOptions>())
            .ThrowsAsync(
                new HttpException(
                    HttpStatusCode.NotFound,
                    Substitute.For<IRequest>(),
                    DiscordErrorCode.UnknownMessage));

        // Act & Assert
        await _sut.Invoking(x => x.Handle(request, CancellationToken.None)).Should().NotThrowAsync();

        request.SourceMessage.Channel.ReceivedWithAnyArgs(1).EnterTypingState();

        await request.SourceMessage.Channel.ReceivedWithAnyArgs(1).SendMessageAsync();

        await reply.ReceivedWithAnyArgs(1).DeleteAsync();
    }
}
