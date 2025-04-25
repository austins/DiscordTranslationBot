using Discord;
using Discord.WebSocket;
using DiscordTranslationBot.Discord.Models;
using DiscordTranslationBot.Notifications.Events;
using DiscordTranslationBot.Telemetry;
using System.Diagnostics;
using System.Reflection;

namespace DiscordTranslationBot.Discord;

/// <summary>
/// Configures the events for the Discord client.
/// </summary>
internal sealed partial class DiscordEventListener
{
    private readonly DiscordSocketClient _client;
    private readonly global::Mediator.Mediator _mediator;
    private readonly ILogger<DiscordEventListener> _logger;
    private readonly Log _log;
    private readonly ActivitySource _activitySource;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscordEventListener" /> class.
    /// </summary>
    /// <param name="client">Discord client to use.</param>
    /// <param name="mediator">Mediator to use.</param>
    /// <param name="logger">Logger to use.</param>
    /// <param name="instrumentation">Instrumentation to use.</param>
    public DiscordEventListener(
        IDiscordClient client,
        global::Mediator.Mediator mediator,
        ILogger<DiscordEventListener> logger,
        Instrumentation instrumentation)
    {
        _client = (DiscordSocketClient)client;
        _mediator = mediator;
        _logger = logger;
        _log = new Log(logger);
        _activitySource = instrumentation.ActivitySource;
    }

    /// <summary>
    /// Hooks up the events to be published to Mediator handlers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to use.</param>
    public Task InitializeEventsAsync(CancellationToken cancellationToken)
    {
        _client.Ready += () => PublishInBackgroundAsync(new ReadyNotification(), cancellationToken);

        _client.Log += logMessage => PublishInBackgroundAsync(
            new LogNotification { LogMessage = logMessage },
            cancellationToken);

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

        _client.ReactionAdded += (message, channel, reaction) => PublishInBackgroundAsync(
            new ReactionAddedNotification
            {
                Message = message,
                Channel = channel,
                ReactionInfo = ReactionInfo.FromSocketReaction(reaction)
            },
            cancellationToken);

        _log.EventsInitialized();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Publishes a notification in the background.
    /// Events should run on a new thread to not block the gateway thread.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">Cancellation token to use.</param>
    /// <returns>A completed task because Discord events are asynchronous.</returns>
    private Task PublishInBackgroundAsync(INotification notification, CancellationToken cancellationToken)
    {
        _ = Task.Run(
            async () =>
            {
                try
                {
                    // Start a trace scope within the background thread.
                    using var traceActivity = _activitySource.StartActivity();
                    using var traceLogScope = _logger.BeginScope(await BuildTraceStateAsync(notification));

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

    /// <summary>
    /// Builds a state with information about a notification initiated by an event for a logger scope.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <returns>State for a logger scope.</returns>
    private static async Task<Dictionary<string, object>> BuildTraceStateAsync(INotification notification)
    {
        const string statePrefix = "trace.";
        const string guildIdKey = "guildId";
        const string channelIdKey = "channelId";
        const string userIdKey = "userId";

        var state = new Dictionary<string, object>();

        var props = notification.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in props)
        {
            if (typeof(IDiscordInteraction).IsAssignableFrom(prop.PropertyType))
            {
                var interaction = (IDiscordInteraction)prop.GetValue(notification)!;
                TryAddStateIfNotNull(guildIdKey, interaction.GuildId);
                TryAddStateIfNotNull(channelIdKey, interaction.ChannelId);
                TryAddStateIfNotNull(userIdKey, interaction.User.Id);
            }
            else if (prop.PropertyType.IsGenericType
                     && prop.PropertyType.GetGenericTypeDefinition() == typeof(Cacheable<,>))
            {
                var genericArgs = prop.PropertyType.GetGenericArguments();
                if (genericArgs[0] == typeof(IMessageChannel) && genericArgs[1] == typeof(ulong))
                {
                    var channel = await ((Cacheable<IMessageChannel, ulong>)prop.GetValue(notification)!)
                        .GetOrDownloadAsync();

                    TryAddStateIfNotNull(guildIdKey, (channel as IGuildChannel)?.GuildId);
                    TryAddStateIfNotNull(channelIdKey, channel.Id);
                }
            }
            else if (prop.PropertyType == typeof(ReactionInfo))
            {
                TryAddStateIfNotNull(userIdKey, ((ReactionInfo)prop.GetValue(notification)!).UserId);
            }
        }

        return state;

        void TryAddStateIfNotNull(string name, object? value)
        {
            if (value is not null)
            {
                state.TryAdd($"{statePrefix}{name}", value);
            }
        }
    }

    private sealed partial class Log(ILogger logger)
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Discord events initialized.")]
        public partial void EventsInitialized();

        [LoggerMessage(
            Level = LogLevel.Error,
            Message = "Failed to publish notification '{notificationName}' in background.")]
        public partial void FailedToPublishNotification(Exception ex, string notificationName);
    }
}
