using Discord;
using DiscordTranslationBot.Commands.TempReply;
using DiscordTranslationBot.Handlers;
using DiscordTranslationBot.Mediator;
using DiscordTranslationBot.Models.Discord;

namespace DiscordTranslationBot.Tests.Handlers;

public sealed class TempReplyHandlerTests
{
    private readonly IBackgroundCommandService _backgroundCommandService;

    private readonly TempReplyHandler _sut;

    public TempReplyHandlerTests()
    {
        _backgroundCommandService = Substitute.For<IBackgroundCommandService>();

        _sut = new TempReplyHandler(_backgroundCommandService, Substitute.For<ILogger<TempReplyHandler>>());
    }

    [Fact]
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

    [Fact]
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

    [Fact]
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

        var reply = Substitute.For<IUserMessage>();
        request.SourceMessage.Channel.SendMessageAsync().ReturnsForAnyArgs(reply);

        // Act
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        request.SourceMessage.Channel.ReceivedWithAnyArgs(1).EnterTypingState();

        await request.SourceMessage.Channel.ReceivedWithAnyArgs(1).SendMessageAsync();

        _backgroundCommandService.Received(1)
            .Schedule(
                Arg.Is<DeleteTempReply>(x => x.Delay == TimeSpan.FromSeconds(request.DeletionDelayInSeconds)),
                Arg.Any<CancellationToken>());
    }
}
