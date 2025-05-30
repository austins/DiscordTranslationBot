using Discord;
using Discord.WebSocket;
using DiscordTranslationBot.Discord.Models;
using DiscordTranslationBot.Notifications.Events;
using DiscordTranslationBot.Telemetry;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

namespace DiscordTranslationBot.Discord;

/// <summary>
/// Configures the events for the Discord client.
/// </summary>
internal sealed partial class DiscordEventListener
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> NotificationPropertyCache = new();
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
            async () => new ReactionAddedNotification
            {
                // The message and channel are retrieved lazily in the background.
                // We can't use the Cacheable types directly as they cannot be constructed directly/tested.
                Message = await message.GetOrDownloadAsync(),
                Channel = await channel.GetOrDownloadAsync(),
                ReactionInfo = ReactionInfo.FromSocketReaction(reaction)
            },
            cancellationToken);

        _log.EventsInitialized();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Publishes a notification in the background.
    /// Events should run on a new thread to not block the gateway thread, so all logic within this method should be done within the Task.Run() call.
    /// </summary>
    /// <param name="notificationFactory">The notification factory.</param>
    /// <param name="cancellationToken">Cancellation token to use.</param>
    /// <returns>A completed task because Discord events are asynchronous.</returns>
    private Task PublishInBackgroundAsync(
        Func<ValueTask<INotification>> notificationFactory,
        CancellationToken cancellationToken)
    {
        _ = Task.Run(
            async () =>
            {
                try
                {
                    var notification = await notificationFactory();
                    var notificationName = notification.GetType().Name;

                    // Create a trace scope within the background thread.
                    using var traceActivity = _activitySource.CreateActivity(
                        $"{nameof(DiscordEventListener)}.{nameof(PublishInBackgroundAsync)}: {{notificationName}}",
                        ActivityKind.Internal);

                    // Only start the trace scope if this is not a log notification to reduce polluting the logs.
                    Dictionary<string, object> traceState = [];
                    if (notification is not LogNotification)
                    {
                        traceActivity?.Start();
                        traceActivity?.SetTag("notificationName", notificationName);
                        traceState = BuildTraceState(traceState, notification);
                    }

                    using var traceLogScope = _logger.BeginScope(traceState);

                    try
                    {
                        await _mediator.Publish(notification, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _log.NotificationException(ex, notificationName);
                    }
                }
                catch (Exception ex)
                {
                    _log.FailedToPublishInBackground(ex);
                }
            },
            cancellationToken);

        return Task.CompletedTask;
    }

    private Task PublishInBackgroundAsync(INotification notification, CancellationToken cancellationToken)
    {
        return PublishInBackgroundAsync(() => ValueTask.FromResult(notification), cancellationToken);
    }

    /// <summary>
    /// Builds a state with contextual information about a notification initiated by an event for a logger scope.
    /// </summary>
    /// <param name="traceState">The trace state to add information to.</param>
    /// <param name="notification">The notification.</param>
    /// <returns>State for a logger scope.</returns>
    private static Dictionary<string, object> BuildTraceState(
        Dictionary<string, object> traceState,
        INotification notification)
    {
        const string statePrefix = "trace.";
        const string guildIdKey = "guildId";
        const string channelIdKey = "channelId";
        const string userIdKey = "userId";

        var props = NotificationPropertyCache.GetOrAdd(
            notification.GetType(),
            type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance));

        foreach (var prop in props)
        {
            if (prop.GetValue(notification) is { } value)
            {
                switch (value)
                {
                    case IDiscordInteraction interaction:
                        TryAddStateIfNotNull(guildIdKey, interaction.GuildId);
                        TryAddStateIfNotNull(channelIdKey, interaction.ChannelId);
                        TryAddStateIfNotNull(userIdKey, interaction.User.Id);
                        break;
                    case IChannel channel:
                        TryAddStateIfNotNull(guildIdKey, (channel as IGuildChannel)?.GuildId);
                        TryAddStateIfNotNull(channelIdKey, channel.Id);
                        break;
                    case ReactionInfo reaction:
                        TryAddStateIfNotNull(userIdKey, reaction.UserId);
                        break;
                }
            }
        }

        return traceState;

        void TryAddStateIfNotNull(string name, object? value)
        {
            if (value is not null)
            {
                traceState.TryAdd($"{statePrefix}{name}", value);
            }
        }
    }

    private sealed partial class Log(ILogger logger)
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Discord events initialized.")]
        public partial void EventsInitialized();

        [LoggerMessage(
            Level = LogLevel.Error,
            Message = "An exception was thrown while publishing notification '{notificationName}'.")]
        public partial void NotificationException(Exception ex, string notificationName);

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to publish notification in background.")]
        public partial void FailedToPublishInBackground(Exception ex);
    }
}
