using DiscordTranslationBot.Commands.SlashCommandExecuted;
using DiscordTranslationBot.Notifications;
using Mediator;

namespace DiscordTranslationBot.Handlers;

/// <summary>
/// Handler for the Ready Discord event.
/// </summary>
public sealed class ReadyHandler : INotificationHandler<ReadyNotification>
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Instantiates a new instance of the <see cref="ReadyHandler"/> class.
    /// </summary>
    /// <param name="mediator">Mediator to use.</param>
    public ReadyHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Delegates ready events to the correct handler.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async ValueTask Handle(
        ReadyNotification notification,
        CancellationToken cancellationToken
    )
    {
        await _mediator.Send(new RegisterSlashCommands(), cancellationToken);
    }
}
