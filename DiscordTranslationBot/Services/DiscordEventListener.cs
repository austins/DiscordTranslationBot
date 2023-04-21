using Discord;
using Discord.WebSocket;
using DiscordTranslationBot.Commands;
using DiscordTranslationBot.Models.Discord;
using DiscordTranslationBot.Notifications;

namespace DiscordTranslationBot.Services;

/// <summary>
/// Configures the events for the Discord client.
/// </summary>
public sealed partial class DiscordEventListener
{
    private readonly CancellationToken _cancellationToken;
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
        _cancellationToken = new CancellationTokenSource().Token;
        _log = new Log(logger);
    }

    /// <summary>
    /// Hooks up the events to be published to Mediator handlers.
    /// </summary>
    public Task InitializeEventsAsync()
    {
        _client.Log += async logMessage =>
            await _mediator.Send(new LogDiscordMessage { LogMessage = logMessage }, _cancellationToken);

        _client.Ready += async () => await _mediator.Publish(new ReadyNotification(), _cancellationToken);

        _client.JoinedGuild += async guild =>
            await _mediator.Publish(new JoinedGuildNotification { Guild = guild }, _cancellationToken);

        _client.MessageCommandExecuted += async command =>
            await _mediator.Publish(new MessageCommandExecutedNotification { Command = command }, _cancellationToken);

        _client.SlashCommandExecuted += async command =>
            await _mediator.Publish(new SlashCommandExecutedNotification { Command = command }, _cancellationToken);

        _client.ReactionAdded += async (message, channel, reaction) =>
            await _mediator.Publish(
                new ReactionAddedNotification
                {
                    Message = await message.GetOrDownloadAsync(),
                    Channel = await channel.GetOrDownloadAsync(),
                    Reaction = new Reaction { UserId = reaction.UserId, Emote = reaction.Emote }
                },
                _cancellationToken
            );

        _log.EventsInitialized();
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
    }
}
