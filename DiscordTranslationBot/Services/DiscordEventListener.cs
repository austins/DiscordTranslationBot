using Discord;
using Discord.WebSocket;
using DiscordTranslationBot.Models.Discord;
using DiscordTranslationBot.Notifications;

namespace DiscordTranslationBot.Services;

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
        _client.Ready += () => PublishInBackgroundAsync(new ReadyNotification(), cancellationToken);

        _client.JoinedGuild += guild => PublishInBackgroundAsync(
            new JoinedGuildNotification { Guild = guild },
            cancellationToken);

        _client.Log += logMessage => PublishInBackgroundAsync(
            new LogNotification { LogMessage = logMessage },
            cancellationToken);

        _client.MessageCommandExecuted += command => PublishInBackgroundAsync(
            new MessageCommandExecutedNotification { Command = command },
            cancellationToken);

        _client.SlashCommandExecuted += command => PublishInBackgroundAsync(
            new SlashCommandExecutedNotification { Command = command },
            cancellationToken);

        _client.ReactionAdded += async (message, _, reaction) => await PublishInBackgroundAsync(
            new ReactionAddedNotification
            {
                Message = await message.GetOrDownloadAsync(),
                Reaction = new Reaction
                {
                    UserId = reaction.UserId,
                    Emote = reaction.Emote
                }
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
