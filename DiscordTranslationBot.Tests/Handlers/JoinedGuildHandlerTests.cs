using Discord;
using DiscordTranslationBot.Commands.SlashCommandExecuted;
using DiscordTranslationBot.Handlers;
using DiscordTranslationBot.Notifications;
using Mediator;
using Moq;
using Xunit;

namespace DiscordTranslationBot.Tests.Handlers;

public sealed class JoinedGuildHandlerTests
{
    private readonly Mock<IMediator> _mediator;
    private readonly JoinedGuildHandler _sut;

    public JoinedGuildHandlerTests()
    {
        _mediator = new Mock<IMediator>(MockBehavior.Strict);
        _sut = new JoinedGuildHandler(_mediator.Object);
    }

    [Fact]
    public async Task Handle_ReadyNotification_Success()
    {
        // Arrange
        var notification = new JoinedGuildNotification { Guild = Mock.Of<IGuild>() };

        _mediator
            .Setup(
                x =>
                    x.Send(
                        It.Is<RegisterSlashCommands>(x => x.Guild == notification.Guild),
                        It.IsAny<CancellationToken>()
                    )
            )
            .ReturnsAsync(Unit.Value);

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        _mediator.Verify(
            x =>
                x.Send(
                    It.Is<RegisterSlashCommands>(x => x.Guild == notification.Guild),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }
}
