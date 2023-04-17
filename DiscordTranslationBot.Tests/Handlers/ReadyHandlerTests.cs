using DiscordTranslationBot.Commands.SlashCommandExecuted;
using DiscordTranslationBot.Handlers;
using DiscordTranslationBot.Notifications;

namespace DiscordTranslationBot.Tests.Handlers;

public sealed class ReadyHandlerTests
{
    private readonly IMediator _mediator;
    private readonly ReadyHandler _sut;

    public ReadyHandlerTests()
    {
        _mediator = Substitute.For<IMediator>();
        _sut = new ReadyHandler(_mediator);
    }

    [Fact]
    public async Task Handle_ReadyNotification_Delegates_Success()
    {
        // Arrange
        var notification = new ReadyNotification();

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<RegisterSlashCommands>(), Arg.Any<CancellationToken>());
    }
}
