using Discord;
using Discord.WebSocket;
using DiscordTranslationBot.Notifications;
using MediatR;

namespace DiscordTranslationBot.Services;

/// <summary>Configures the events for the Discord client.</summary>
public sealed class DiscordEventListener
{
    private readonly CancellationToken _cancellationToken;
    private readonly DiscordSocketClient _client;
    private readonly ILogger<DiscordEventListener> _logger;
    private readonly IMediator _mediator;

    /// <summary>Initializes the DiscordEventListener.</summary>
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

    /// <summary>Hooks up the events to be published to MediatR handlers.</summary>
    public async Task InitializeEvents()
    {
        _client.Log += Log;
        _client.ReactionAdded += ReactionAdded;

        _logger.LogInformation("Notification events initialized.");
        await Task.CompletedTask;
    }

    /// <summary>Log event.</summary>
    /// <param name="logMessage">Discord log message.</param>
    private Task Log(LogMessage logMessage)
    {
        return _mediator.Publish(
            new LogNotification { LogMessage = logMessage },
            _cancellationToken);
    }

    /// <summary>ReactionAdded event.</summary>
    /// <param name="message">Discord user message.</param>
    /// <param name="channel">Discord message channel.</param>
    /// <param name="reaction">The reaction.</param>
    private Task ReactionAdded(
        Cacheable<IUserMessage, ulong> message,
        Cacheable<IMessageChannel, ulong> channel,
        SocketReaction reaction)
    {
        return _mediator.Publish(
            new ReactionAddedNotification { Message = message, Channel = channel, Reaction = reaction },
            _cancellationToken);
    }
}