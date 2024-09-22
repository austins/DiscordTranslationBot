using Discord;
using DiscordTranslationBot.Providers.Translation.Models;

namespace DiscordTranslationBot.Providers.Translation;

public sealed partial class TranslationProviderFactory
{
    private const int MaxOptionsCount = SlashCommandBuilder.MaxOptionsCount;
    private readonly Log _log;
    private bool _initialized;
    private TranslationProviderBase? _lastProvider;
    private TranslationProviderBase? _primaryProvider;
    private List<SupportedLanguage>? _supportedLanguagesForOptions;

    public TranslationProviderFactory(
        IEnumerable<TranslationProviderBase> translationProviders,
        ILogger<TranslationProviderFactory> logger)
    {
        Providers = translationProviders.ToList();
        _log = new Log(logger);
    }

    public IReadOnlyList<TranslationProviderBase> Providers { get; }

    public TranslationProviderBase PrimaryProvider => _primaryProvider ??= Providers[0];

    public TranslationProviderBase LastProvider => _lastProvider ??= Providers[^1];

    public async Task<bool> InitializeProvidersAsync(CancellationToken cancellationToken)
    {
        if (!_initialized)
        {
            // Check if no translation providers are enabled.
            if (!Providers.Any())
            {
                _log.NoProvidersEnabled();
                return false;
            }

            _log.ProvidersEnabled(Providers.Select(tp => tp.ProviderName));

            // Initialize the translator providers.
            foreach (var translationProvider in Providers)
            {
                _log.InitializingProvider(translationProvider.ProviderName);
                await translationProvider.InitializeSupportedLanguagesAsync(cancellationToken);
                _log.InitializeProvider(translationProvider.ProviderName);
            }

            _initialized = true;
        }

        return true;
    }

    public IReadOnlyList<SupportedLanguage> GetSupportedLanguagesForOptions()
    {
        if (_supportedLanguagesForOptions is null)
        {
            // Gather list of language choices for the command's options.
            if (PrimaryProvider.TranslateCommandLangCodes is null)
            {
                // If no lang codes are specified, take the first up to the max options limit.
                _supportedLanguagesForOptions = PrimaryProvider.SupportedLanguages.Take(MaxOptionsCount).ToList();
            }
            else
            {
                // Get valid specified lang codes up to the limit.
                _supportedLanguagesForOptions = PrimaryProvider
                    .SupportedLanguages
                    .Where(l => PrimaryProvider.TranslateCommandLangCodes.Contains(l.LangCode))
                    .Take(MaxOptionsCount)
                    .ToList();

                // If there are fewer languages found than the max options and more supported languages,
                // get the rest up to the max options limit.
                if (_supportedLanguagesForOptions.Count < MaxOptionsCount
                    && PrimaryProvider.SupportedLanguages.Count > _supportedLanguagesForOptions.Count)
                {
                    _supportedLanguagesForOptions.AddRange(
                        PrimaryProvider
                            .SupportedLanguages
                            .Where(l => !_supportedLanguagesForOptions.Contains(l))
                            .Take(MaxOptionsCount - _supportedLanguagesForOptions.Count)
                            .ToList());
                }
            }
        }

        return [.._supportedLanguagesForOptions.OrderBy(l => l.Name)];
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
        public partial void InitializeProvider(string providerName);
    }
}
