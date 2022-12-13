using Discord;
using Discord.WebSocket;
using DiscordTranslationBot.Configuration;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Services;
using Microsoft.Extensions.Options;
using Serilog;
using ILogger = Serilog.ILogger;

namespace DiscordTranslationBot;

/// <summary>
/// The main worker service.
/// </summary>
public sealed class Worker : BackgroundService
{
    private static readonly ILogger Logger = Log.ForContext<Worker>();
    private readonly DiscordSocketClient _client;
    private readonly IOptions<DiscordOptions> _discordOptions;
    private readonly DiscordEventListener _eventListener;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly IEnumerable<TranslationProviderBase> _translationProviders;

    /// <summary>
    /// Initializes a new instance of the <see cref="Worker"/> class.
    /// </summary>
    /// <param name="translationProviders">Translation providers to use.</param>
    /// <param name="client">Discord client to use.</param>
    /// <param name="eventListener">Discord event listener to use.</param>
    /// <param name="discordOptions">Discord configuration options.</param>
    /// <param name="hostApplicationLifetime">Host application lifetime to use.</param>
    public Worker(
        IEnumerable<TranslationProviderBase> translationProviders,
        DiscordSocketClient client,
        DiscordEventListener eventListener,
        IOptions<DiscordOptions> discordOptions,
        IHostApplicationLifetime hostApplicationLifetime
    )
    {
        _translationProviders = translationProviders;
        _client = client;
        _eventListener = eventListener;
        _discordOptions = discordOptions;
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    /// <summary>
    /// Runs when app is started.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        // Verify that the Discord BotToken is set.
        if (string.IsNullOrWhiteSpace(_discordOptions.Value.BotToken))
        {
            Logger.Error(
                "The Discord {ConfigName} must be set.",
                nameof(_discordOptions.Value.BotToken)
            );

            _hostApplicationLifetime.StopApplication();
            return;
        }

        // Initialize the translator providers.
        foreach (var translationProvider in _translationProviders)
        {
            await translationProvider.InitializeSupportedLanguagesAsync(cancellationToken);
        }

        // Initialize the Discord client.
        await _client.LoginAsync(TokenType.Bot, _discordOptions.Value.BotToken);
        await _client.StartAsync();

        // Initialize the Discord event listener.
        await _eventListener.InitializeEventsAsync();

        await base.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Stops the app gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.LogoutAsync();
        await _client.StopAsync();

        await base.StopAsync(cancellationToken);
    }

    /// <summary>
    /// The main execution thread that monitors for a cancellation request to stop the app.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
}
