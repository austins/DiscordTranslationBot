using Discord;
using DiscordTranslationBot.Commands.SlashCommandExecuted;
using DiscordTranslationBot.Handlers;
using DiscordTranslationBot.Notifications;

namespace DiscordTranslationBot.Tests.Handlers;

public sealed class JoinedGuildHandlerTests
{
    private readonly IMediator _mediator;
    private readonly JoinedGuildHandler _sut;

    public JoinedGuildHandlerTests()
    {
        _mediator = Substitute.For<IMediator>();
        _sut = new JoinedGuildHandler(_mediator);
    }

    [Fact]
    public async Task Handle_ReadyNotification_Delegates_Success()
    {
        // Arrange
        var notification = new JoinedGuildNotification { Guild = Substitute.For<IGuild>() };

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await _mediator
            .Received(1)
            .Send(Arg.Is<RegisterSlashCommands>(x => x.Guild == notification.Guild), Arg.Any<CancellationToken>());
    }
}
