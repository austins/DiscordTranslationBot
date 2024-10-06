using Discord;
using Discord.WebSocket;
using DiscordTranslationBot.Commands.Logging;
using DiscordTranslationBot.Discord.Models;
using DiscordTranslationBot.Notifications.Events;

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
        _client.Ready += async () => await _mediator.Publish(new ReadyNotification(), cancellationToken);

        _client.Log += async logMessage => await _mediator.Send(
            new RedirectLogMessageToLogger { LogMessage = logMessage },
            cancellationToken);

        // User initiated events should run on a new thread to not block the gateway thread.
        // Each event published can act within a per-request scope.
        _client.JoinedGuild += guild => PublishInBackgroundAsync(
            new JoinedGuildNotification { Guild = guild },
            cancellationToken);

        _client.MessageCommandExecuted += messageCommand => PublishInBackgroundAsync(
            new MessageCommandExecutedNotification { Interaction = messageCommand },
            cancellationToken);

        _client.SelectMenuExecuted += interaction => PublishInBackgroundAsync(
            new SelectMenuExecutedNotification { Interaction = interaction },
            cancellationToken);

        _client.ButtonExecuted += interaction => PublishInBackgroundAsync(
            new ButtonExecutedNotification { Interaction = interaction },
            cancellationToken);

        _client.SlashCommandExecuted += slashCommand => PublishInBackgroundAsync(
            new SlashCommandExecutedNotification { Interaction = slashCommand },
            cancellationToken);

        _client.ReactionAdded += async (messageCache, channel, reaction) =>
        {
            // The message may be null if the reaction was not added on a user message, e.g. "pinned a message to this channel".
            // If it's null, no-op.
            var message = await messageCache.GetOrDownloadAsync();
            if (message is not null)
            {
                await PublishInBackgroundAsync(
                    new ReactionAddedNotification
                    {
                        Message = message,
                        Channel = await channel.GetOrDownloadAsync(),
                        ReactionInfo = ReactionInfo.FromSocketReaction(reaction)
                    },
                    cancellationToken);
            }
        };

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
