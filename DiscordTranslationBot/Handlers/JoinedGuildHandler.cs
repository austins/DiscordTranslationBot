using DiscordTranslationBot.Commands.SlashCommandExecuted;
using DiscordTranslationBot.Notifications;
using Mediator;

namespace DiscordTranslationBot.Handlers;

/// <summary>
/// Handler for the Joined Guild Discord event.
/// </summary>
public sealed class JoinedGuildHandler : INotificationHandler<JoinedGuildNotification>
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Instantiates a new instance of the <see cref="JoinedGuildHandler"/> class.
    /// </summary>
    /// <param name="mediator">Mediator to use.</param>
    public JoinedGuildHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Delegates joined guild events to the correct handler.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async ValueTask Handle(
        JoinedGuildNotification notification,
        CancellationToken cancellationToken
    )
    {
        await _mediator.Send(
            new RegisterSlashCommands { Guild = notification.Guild },
            cancellationToken
        );
    }
}
