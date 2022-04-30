using Discord;
using Discord.WebSocket;
using DiscordTranslationBot.Configuration;
using DiscordTranslationBot.Services;
using Microsoft.Extensions.Options;

namespace DiscordTranslationBot;

/// <summary>The main worker service.</summary>
public class Worker : BackgroundService
{
    private readonly DiscordSocketClient _client;
    private readonly IOptions<DiscordOptions> _discordOptions;
    private readonly DiscordEventListener _eventListener;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly LibreTranslate.Net.LibreTranslate _libreTranslate;
    private readonly ILogger<Worker> _logger;

    /// <summary>Initializes the main worker service.</summary>
    /// <param name="libreTranslate">LibreTranslate client to use.</param>
    /// <param name="client">Discord client to use.</param>
    /// <param name="eventListener">Discord event listener to use.</param>
    /// <param name="discordOptions">Discord configuration options.</param>
    /// <param name="hostApplicationLifetime">Host application lifetime to use.</param>
    /// <param name="logger">Logger to use.</param>
    public Worker(
        LibreTranslate.Net.LibreTranslate libreTranslate,
        DiscordSocketClient client,
        DiscordEventListener eventListener,
        IOptions<DiscordOptions> discordOptions,
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger<Worker> logger)
    {
        _libreTranslate = libreTranslate;
        _client = client;
        _eventListener = eventListener;
        _discordOptions = discordOptions;
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
    }

    /// <summary>The main execution thread that monitors for a cancellation request to stop the app.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000, cancellationToken);
        }
    }

    /// <summary>Runs when app is started.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        // Verify that the LibreTranslate API URL can be connected to initially.
        try
        {
            await _libreTranslate.GetSupportedLanguagesAsync();
        }
        catch (HttpRequestException ex) when
            (ex.StackTrace?.Contains(nameof(LibreTranslate.Net.LibreTranslate)) == true)
        {
            _logger.LogError(ex, "Unable to connect to the LibreTranslate API URL.");
            _hostApplicationLifetime.StopApplication();
            return;
        }

        // Verify that the Discord BotToken is set.
        if (string.IsNullOrWhiteSpace(_discordOptions.Value.BotToken))
        {
            _logger.LogError($"The Discord {nameof(_discordOptions.Value.BotToken)} must be set.");
            _hostApplicationLifetime.StopApplication();
            return;
        }

        // Initialize the Discord client.
        await _client.LoginAsync(TokenType.Bot, _discordOptions.Value.BotToken);
        await _client.StartAsync();

        // Initialize the Discord event listener.
        await _eventListener.InitializeEvents();

        await base.StartAsync(cancellationToken);
    }

    /// <summary>Stops the app gracefully.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.LogoutAsync();
        await _client.StopAsync();

        await base.StopAsync(cancellationToken);
    }
}