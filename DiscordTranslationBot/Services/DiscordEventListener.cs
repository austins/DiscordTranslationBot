using Discord;
using Discord.WebSocket;
using DiscordTranslationBot.Models.Discord;
using DiscordTranslationBot.Notifications;
using MediatR;

namespace DiscordTranslationBot.Services;

/// <summary>
/// Configures the events for the Discord client.
/// </summary>
public sealed class DiscordEventListener
{
    private readonly CancellationToken _cancellationToken;
    private readonly DiscordSocketClient _client;
    private readonly ILogger<DiscordEventListener> _logger;
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscordEventListener"/> class.
    /// </summary>
    /// <param name="client">Discord client to use.</param>
    /// <param name="mediator">Mediator to use.</param>
    /// <param name="logger">Logger to use.</param>
    public DiscordEventListener(
        DiscordSocketClient client,
        IMediator mediator,
        ILogger<DiscordEventListener> logger)
    {
        _client = client;
        _mediator = mediator;
        _logger = logger;
        _cancellationToken = new CancellationTokenSource().Token;
    }

    /// <summary>
    /// Hooks up the events to be published to MediatR handlers.
    /// </summary>
    public Task InitializeEventsAsync()
    {
        _client.Log += LogAsync;
        _client.ReactionAdded += ReactionAddedAsync;

        _logger.LogInformation("Notification events initialized.");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Log event.
    /// </summary>
    /// <param name="logMessage">Discord log message.</param>
    private Task LogAsync(LogMessage logMessage)
    {
        return _mediator.Publish(
            new LogNotification { LogMessage = logMessage },
            _cancellationToken);
    }

    /// <summary>
    /// ReactionAdded event.
    /// </summary>
    /// <param name="message">Discord user message.</param>
    /// <param name="channel">Discord message channel.</param>
    /// <param name="reaction">The reaction.</param>
    private Task ReactionAddedAsync(
        Cacheable<IUserMessage, ulong> message,
        Cacheable<IMessageChannel, ulong> channel,
        SocketReaction reaction)
    {
        return _mediator.Publish(
            new ReactionAddedNotification
            {
                Message = message.GetOrDownloadAsync(),
                Channel = channel.GetOrDownloadAsync(),
                Reaction = new Reaction
                {
                    UserId = reaction.UserId,
                    Emote = reaction.Emote,
                },
            },
            _cancellationToken);
    }
}
