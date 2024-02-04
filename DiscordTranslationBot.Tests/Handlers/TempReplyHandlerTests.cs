using Discord;
using DiscordTranslationBot.Commands.TempReply;
using DiscordTranslationBot.Handlers;
using DiscordTranslationBot.Models.Discord;
using MediatR;

namespace DiscordTranslationBot.Tests.Handlers;

[TestClass]
public sealed class TempReplyHandlerTests
{
    private readonly IMediator _mediator;

    private readonly TempReplyHandler _sut;

    public TempReplyHandlerTests()
    {
        _mediator = Substitute.For<IMediator>();

        _sut = new TempReplyHandler(_mediator, new LoggerFake<TempReplyHandler>());
    }

    [TestMethod]
    public async Task Handle_DeleteTempReply_Success_WithReactionAndSourceMessage()
    {
        // Arrange
        var request = new DeleteTempReply
        {
            Reply = Substitute.For<IMessage>(),
            Reaction = new Reaction
            {
                UserId = 1,
                Emote = Substitute.For<IEmote>()
            },
            SourceMessage = Substitute.For<IMessage>()
        };

        var sourceMessage = Substitute.For<IMessage>();
        request.Reply.Channel.GetMessageAsync(default).ReturnsForAnyArgs(sourceMessage);

        // Act
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        await request.Reply.Channel.Received(1)
            .GetMessageAsync(
                Arg.Is<ulong>(x => x == request.SourceMessage.Id),
                Arg.Any<CacheMode>(),
                Arg.Any<RequestOptions>());

        await sourceMessage.Received(1)
            .RemoveReactionAsync(
                Arg.Is<IEmote>(x => x == request.Reaction.Emote),
                Arg.Is<ulong>(x => x == request.Reaction.UserId),
                Arg.Any<RequestOptions>());

        await request.Reply.ReceivedWithAnyArgs(1).DeleteAsync();
    }

    [TestMethod]
    public async Task Handle_DeleteTempReply_Success_NoReactionAndSourceMessage()
    {
        // Arrange
        var request = new DeleteTempReply
        {
            Reply = Substitute.For<IMessage>(),
            Reaction = null,
            SourceMessage = Substitute.For<IMessage>()
        };

        // Act
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        await request.Reply.Channel.DidNotReceiveWithAnyArgs().GetMessageAsync(default);

        await request.Reply.ReceivedWithAnyArgs(1).DeleteAsync();
    }

    [TestMethod]
    public async Task Handle_SendTempReply_Success()
    {
        // Arrange
        var request = new SendTempReply
        {
            Text = "test",
            Reaction = null,
            SourceMessage = Substitute.For<IMessage>(),
            DeletionDelayInSeconds = 10
        };

        request.SourceMessage.Channel.SendMessageAsync().ReturnsForAnyArgs(Substitute.For<IUserMessage>());

        // Act
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        request.SourceMessage.Channel.ReceivedWithAnyArgs(1).EnterTypingState();

        await request.SourceMessage.Channel.ReceivedWithAnyArgs(1).SendMessageAsync();

        await _mediator.Received(1)
            .Send(
                Arg.Is<DeleteTempReply>(x => x.Delay == TimeSpan.FromSeconds(request.DeletionDelayInSeconds)),
                Arg.Any<CancellationToken>());
    }
}
