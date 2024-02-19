using Discord;
using Discord.WebSocket;
using DiscordTranslationBot.Configuration;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Services;
using Microsoft.Extensions.Options;

namespace DiscordTranslationBot;

/// <summary>
/// The main worker service.
/// </summary>
internal sealed partial class Worker : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly ICountryService _countryService;
    private readonly IOptions<DiscordOptions> _discordOptions;
    private readonly DiscordEventListener _eventListener;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly Log _log;
    private readonly IReadOnlyList<TranslationProviderBase> _translationProviders;

    /// <summary>
    /// Initializes a new instance of the <see cref="Worker" /> class.
    /// </summary>
    /// <param name="translationProviders">Translation providers to use.</param>
    /// <param name="countryService">Country service to use.</param>
    /// <param name="client">Discord client to use.</param>
    /// <param name="eventListener">Discord event listener to use.</param>
    /// <param name="discordOptions">Discord configuration options.</param>
    /// <param name="hostApplicationLifetime">Host application lifetime to use.</param>
    /// <param name="logger">Logger to use.</param>
    public Worker(
        IEnumerable<TranslationProviderBase> translationProviders,
        ICountryService countryService,
        IDiscordClient client,
        DiscordEventListener eventListener,
        IOptions<DiscordOptions> discordOptions,
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger<Worker> logger)
    {
        _translationProviders = translationProviders.ToList();
        _countryService = countryService;
        _client = (DiscordSocketClient)client;
        _eventListener = eventListener;
        _discordOptions = discordOptions;
        _hostApplicationLifetime = hostApplicationLifetime;
        _log = new Log(logger);
    }

    /// <summary>
    /// Runs when app is started.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Check if no translation providers are enabled.
        if (!_translationProviders.Any())
        {
            _log.NoTranslationProvidersEnabled();
            _hostApplicationLifetime.StopApplication();
            return;
        }

        _log.TranslationProvidersEnabled(string.Join(", ", _translationProviders.Select(tp => tp.ProviderName)));

        // Initialize the translator providers.
        foreach (var translationProvider in _translationProviders)
        {
            _log.InitializingTranslationProvider(translationProvider.ProviderName);
            await translationProvider.InitializeSupportedLanguagesAsync(cancellationToken);
            _log.InitializedTranslationProvider(translationProvider.ProviderName);
        }

        // Initialize the country service.
        _countryService.Initialize();

        // Initialize the Discord client.
        await _client.LoginAsync(TokenType.Bot, _discordOptions.Value.BotToken);
        await _client.StartAsync();

        // Initialize the Discord event listener.
        await _eventListener.InitializeEventsAsync();
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

    private sealed partial class Log
    {
        private readonly ILogger<Worker> _logger;

        public Log(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        [LoggerMessage(
            Level = LogLevel.Error,
            Message =
                "No translation providers enabled. Please configure and enable at least one translation provider.")]
        public partial void NoTranslationProvidersEnabled();

        [LoggerMessage(Level = LogLevel.Information, Message = "Translation providers enabled: {providerNames}")]
        public partial void TranslationProvidersEnabled(string providerNames);

        [LoggerMessage(Level = LogLevel.Information, Message = "Initializing translation provider: {providerName}")]
        public partial void InitializingTranslationProvider(string providerName);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Finished initializing translation provider: {providerName}")]
        public partial void InitializedTranslationProvider(string providerName);
    }
}
