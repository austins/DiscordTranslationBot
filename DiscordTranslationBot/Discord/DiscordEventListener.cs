using Discord;
using Discord.WebSocket;
using DiscordTranslationBot.Commands.Logging;
using DiscordTranslationBot.Discord.Events;

namespace DiscordTranslationBot.Discord;

/// <summary>
/// Configures the events for the Discord client.
/// </summary>
internal sealed partial class DiscordEventListener
{
    private readonly DiscordSocketClient _client;
    private readonly Log _log;
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscordEventListener" /> class.
    /// </summary>
    /// <param name="client">Discord client to use.</param>
    /// <param name="mediator">Mediator to use.</param>
    /// <param name="logger">Logger to use.</param>
    public DiscordEventListener(IDiscordClient client, IMediator mediator, ILogger<DiscordEventListener> logger)
    {
        _client = (DiscordSocketClient)client;
        _mediator = mediator;
        _log = new Log(logger);
    }

    /// <summary>
    /// Hooks up the events to be published to Mediator handlers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to use.</param>
    public Task InitializeEventsAsync(CancellationToken cancellationToken)
    {
        _client.Ready += () => PublishInBackgroundAsync(new ReadyEvent(), cancellationToken);

        _client.JoinedGuild += guild => PublishInBackgroundAsync(
            new JoinedGuildEvent { Guild = guild },
            cancellationToken);

        _client.Log += logMessage => _mediator.Send(
            new RedirectLogMessageToLogger { LogMessage = logMessage },
            cancellationToken);

        _client.MessageCommandExecuted += messageCommand => PublishInBackgroundAsync(
            new MessageCommandExecutedEvent { MessageCommand = messageCommand },
            cancellationToken);

        _client.SlashCommandExecuted += slashCommand => PublishInBackgroundAsync(
            new SlashCommandExecutedEvent { SlashCommand = slashCommand },
            cancellationToken);

        _client.ReactionAdded += (message, channel, reaction) => PublishInBackgroundAsync(
            new ReactionAddedEvent
            {
                Message = message,
                Channel = channel,
                Reaction = reaction
            },
            cancellationToken);

        _log.EventsInitialized();
        return Task.CompletedTask;
    }

    private Task PublishInBackgroundAsync(INotification notification, CancellationToken cancellationToken)
    {
        _ = Task.Run(
            async () =>
            {
                try
                {
                    await _mediator.Publish(notification, cancellationToken);
                }
                catch (Exception ex)
                {
                    _log.FailedToPublishNotification(ex, notification.GetType().Name);
                }
            },
            cancellationToken);

        return Task.CompletedTask;
    }

    private sealed partial class Log
    {
        private readonly ILogger<DiscordEventListener> _logger;

        public Log(ILogger<DiscordEventListener> logger)
        {
            _logger = logger;
        }

        [LoggerMessage(Level = LogLevel.Information, Message = "Discord events initialized.")]
        public partial void EventsInitialized();

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to publish notification '{notificationName}'.")]
        public partial void FailedToPublishNotification(Exception ex, string notificationName);
    }
}
