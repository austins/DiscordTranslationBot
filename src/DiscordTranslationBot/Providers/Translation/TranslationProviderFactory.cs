using Discord;
using DiscordTranslationBot.Providers.Translation.Models;

namespace DiscordTranslationBot.Providers.Translation;

public sealed partial class TranslationProviderFactory
{
    private const int MaxOptionsCount = SlashCommandOptionBuilder.MaxChoiceCount;
    private readonly Log _log;
    private readonly IReadOnlyList<TranslationProviderBase> _providers;
    private bool _initialized;
    private TranslationProviderBase? _lastProvider;
    private TranslationProviderBase? _primaryProvider;
    private IReadOnlyList<SupportedLanguage>? _supportedLanguagesForOptions;

    public TranslationProviderFactory(
        IEnumerable<TranslationProviderBase> translationProviders,
        ILogger<TranslationProviderFactory> logger)
    {
        _providers = translationProviders.ToList();
        _log = new Log(logger);
    }

    public IReadOnlyList<TranslationProviderBase> Providers
    {
        get
        {
            ThrowIfProvidersNotInitialized();
            return _providers;
        }
    }

    public TranslationProviderBase PrimaryProvider
    {
        get
        {
            ThrowIfProvidersNotInitialized();
            return _primaryProvider ??= Providers[0];
        }
    }

    public TranslationProviderBase LastProvider
    {
        get
        {
            ThrowIfProvidersNotInitialized();
            return _lastProvider ??= Providers[^1];
        }
    }

    public async Task<bool> InitializeProvidersAsync(CancellationToken cancellationToken)
    {
        if (!_initialized)
        {
            // Check if no translation providers are enabled.
            if (!_providers.Any())
            {
                _log.NoProvidersEnabled();
                return false;
            }

            _log.ProvidersEnabled(_providers.Select(tp => tp.ProviderName));

            // Initialize the translator providers.
            async Task InitializeSupportedLanguagesAsync(TranslationProviderBase translationProvider)
            {
                _log.InitializingProvider(translationProvider.ProviderName);
                await translationProvider.InitializeSupportedLanguagesAsync(cancellationToken);
                _log.InitializedProvider(translationProvider.ProviderName);
            }

            await Task.WhenAll(_providers.Select(InitializeSupportedLanguagesAsync));

            _initialized = true;
        }

        return true;
    }

    public IReadOnlyList<SupportedLanguage> GetSupportedLanguagesForOptions()
    {
        ThrowIfProvidersNotInitialized();

        if (_supportedLanguagesForOptions is null)
        {
            // Gather list of language choices for the command's options.
            List<SupportedLanguage> supportedLanguages;
            if (PrimaryProvider.TranslateCommandLangCodes is null)
            {
                // If no lang codes are specified, take the first up to the max options limit.
                supportedLanguages = PrimaryProvider.SupportedLanguages.Take(MaxOptionsCount).ToList();
            }
            else
            {
                // Get valid specified lang codes up to the limit.
                supportedLanguages = PrimaryProvider
                    .SupportedLanguages
                    .Where(l => PrimaryProvider.TranslateCommandLangCodes.Contains(l.LangCode))
                    .Take(MaxOptionsCount)
                    .ToList();

                // If there are fewer languages found than the max options and more supported languages,
                // get the rest up to the max options limit.
                if (supportedLanguages.Count < MaxOptionsCount
                    && PrimaryProvider.SupportedLanguages.Count > supportedLanguages.Count)
                {
                    supportedLanguages.AddRange(
                        PrimaryProvider
                            .SupportedLanguages
                            .Where(l => !supportedLanguages.Contains(l))
                            .Take(MaxOptionsCount - supportedLanguages.Count)
                            .ToList());
                }
            }

            _supportedLanguagesForOptions = [..supportedLanguages.OrderBy(l => l.Name)];
        }

        return _supportedLanguagesForOptions;
    }

    private void ThrowIfProvidersNotInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException(
                $"Providers must be initialized before calling {nameof(TranslationProviderFactory)}.{nameof(GetSupportedLanguagesForOptions)}.");
        }
    }

    private sealed partial class Log
    {
        private readonly ILogger _logger;

        public Log(ILogger logger)
        {
            _logger = logger;
        }

        [LoggerMessage(
            Level = LogLevel.Error,
            Message =
                "No translation providers enabled. Please configure and enable at least one translation provider.")]
        public partial void NoProvidersEnabled();

        [LoggerMessage(Level = LogLevel.Information, Message = "Translation providers enabled: {providerNames}")]
        public partial void ProvidersEnabled(IEnumerable<string> providerNames);

        [LoggerMessage(Level = LogLevel.Information, Message = "Initializing translation provider: {providerName}")]
        public partial void InitializingProvider(string providerName);

        [LoggerMessage(
            Level = LogLevel.Information,
            Message = "Finished initializing translation provider: {providerName}")]
        public partial void InitializedProvider(string providerName);
    }
}
