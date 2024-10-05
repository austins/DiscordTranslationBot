using Discord;
using Discord.WebSocket;
using DiscordTranslationBot.Discord;
using DiscordTranslationBot.Providers.Translation;
using Microsoft.Extensions.Options;
using DiscordEventListener = DiscordTranslationBot.Discord.DiscordEventListener;

namespace DiscordTranslationBot;

/// <summary>
/// The main worker service.
/// </summary>
internal sealed class Worker : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly IOptions<DiscordOptions> _discordOptions;
    private readonly DiscordEventListener _eventListener;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ITranslationProviderFactory _translationProviderFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="Worker" /> class.
    /// </summary>
    /// <param name="translationProviderFactory">Translation provider factory to use.</param>
    /// <param name="client">Discord client to use.</param>
    /// <param name="eventListener">Discord event listener to use.</param>
    /// <param name="discordOptions">Discord configuration options.</param>
    /// <param name="hostApplicationLifetime">Host application lifetime to use.</param>
    public Worker(
        ITranslationProviderFactory translationProviderFactory,
        IDiscordClient client,
        DiscordEventListener eventListener,
        IOptions<DiscordOptions> discordOptions,
        IHostApplicationLifetime hostApplicationLifetime)
    {
        _translationProviderFactory = translationProviderFactory;
        _client = (DiscordSocketClient)client;
        _eventListener = eventListener;
        _discordOptions = discordOptions;
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    /// <summary>
    /// Runs when app is started.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Initialize the translator providers.
        if (!await _translationProviderFactory.InitializeProvidersAsync(cancellationToken))
        {
            // No translation providers are enabled.
            _hostApplicationLifetime.StopApplication();
            return;
        }

        // Initialize the Discord client.
        await _client.LoginAsync(TokenType.Bot, _discordOptions.Value.BotToken);
        await _client.StartAsync();

        // Initialize the Discord event listener.
        await _eventListener.InitializeEventsAsync(cancellationToken);
    }

    /// <summary>
    /// Stops the app gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.LogoutAsync();
        await _client.StopAsync();
    }
}
