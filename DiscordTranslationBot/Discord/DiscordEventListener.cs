using Discord;
using Discord.WebSocket;
using DiscordTranslationBot.Commands.Logging;
using DiscordTranslationBot.Discord.Events;
using DiscordTranslationBot.Discord.Models;

namespace DiscordTranslationBot.Discord;

/// <summary>
/// Configures the events for the Discord client.
/// </summary>
internal sealed partial class DiscordEventListener
{
    private readonly DiscordSocketClient _client;
    private readonly Log _log;
    private readonly global::Mediator.Mediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscordEventListener" /> class.
    /// </summary>
    /// <param name="client">Discord client to use.</param>
    /// <param name="mediator">Mediator to use.</param>
    /// <param name="logger">Logger to use.</param>
    public DiscordEventListener(
        IDiscordClient client,
        global::Mediator.Mediator mediator,
        ILogger<DiscordEventListener> logger)
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
        // Discord client initiated events can run on its gateway thread.
        _client.Ready += async () => await _mediator.Publish(new ReadyEvent(), cancellationToken);

        _client.Log += async logMessage => await _mediator.Send(
            new RedirectLogMessageToLogger { LogMessage = logMessage },
            cancellationToken);

        // User initiated events should run on a new thread to not block the gateway thread.
        // Each event published can act within a per-request scope.
        _client.JoinedGuild += guild => PublishInBackgroundAsync(
            new JoinedGuildEvent { Guild = guild },
            cancellationToken);

        _client.MessageCommandExecuted += messageCommand => PublishInBackgroundAsync(
            new MessageCommandExecutedEvent { MessageCommand = messageCommand },
            cancellationToken);

        _client.SlashCommandExecuted += slashCommand => PublishInBackgroundAsync(
            new SlashCommandExecutedEvent { SlashCommand = slashCommand },
            cancellationToken);

        _client.ReactionAdded += async (message, channel, reaction) => await PublishInBackgroundAsync(
            new ReactionAddedEvent
            {
                Message = await message.GetOrDownloadAsync(),
                Channel = await channel.GetOrDownloadAsync(),
                ReactionInfo = ReactionInfo.FromSocketReaction(reaction)
            },
            cancellationToken);

        _log.EventsInitialized();

        return Task.CompletedTask;
    }

    private Task PublishInBackgroundAsync(INotification notification, CancellationToken cancellationToken)
    {
        var notificationName = notification.GetType().Name;

        _ = Task.Run(
            async () =>
            {
                try
                {
                    _log.PublishingNotification(notificationName);
                    await _mediator.Publish(notification, cancellationToken);
                }
                catch (Exception ex)
                {
                    _log.FailedToPublishNotification(ex, notificationName);
                }
            },
            cancellationToken);

        return Task.CompletedTask;
    }

    private sealed partial class Log
    {
        private readonly ILogger _logger;

        public Log(ILogger logger)
        {
            _logger = logger;
        }

        [LoggerMessage(Level = LogLevel.Information, Message = "Discord events initialized.")]
        public partial void EventsInitialized();

        [LoggerMessage(Level = LogLevel.Information, Message = "Publishing notification '{notificationName}'...")]
        public partial void PublishingNotification(string notificationName);

        [LoggerMessage(
            Level = LogLevel.Error,
            Message = "Failed to publish notification '{notificationName}' in background.")]
        public partial void FailedToPublishNotification(Exception ex, string notificationName);
    }
}
