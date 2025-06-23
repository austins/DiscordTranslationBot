using Discord;
using DiscordTranslationBot.Providers.Translation.Exceptions;
using DiscordTranslationBot.Providers.Translation.Models;
using System.Runtime.CompilerServices;

namespace DiscordTranslationBot.Providers.Translation;

internal sealed partial class TranslationProviderFactory : ITranslationProviderFactory
{
    private const int MaxOptionsCount = SlashCommandOptionBuilder.MaxChoiceCount;
    private readonly Log _log;
    private readonly IReadOnlyList<ITranslationProvider> _providers;
    private bool _initialized;
    private IReadOnlyList<SupportedLanguage>? _supportedLanguagesForOptions;

    public TranslationProviderFactory(
        IEnumerable<ITranslationProvider> translationProviders,
        ILogger<TranslationProviderFactory> logger)
    {
        _providers = translationProviders.ToList();
        _log = new Log(logger);
    }

    public ITranslationProvider PrimaryProvider
    {
        get
        {
            ThrowIfProvidersNotInitialized();
            return _providers[0];
        }
    }

    public async Task<bool> InitializeProvidersAsync(CancellationToken cancellationToken)
    {
        if (!_initialized)
        {
            // Check if no translation providers are enabled.
            if (_providers.Count == 0)
            {
                _log.NoProvidersEnabled();
                return false;
            }

            _log.ProvidersEnabled(_providers.Select(x => x.GetType().Name));

            // Initialize the translator providers.
            async Task InitializeSupportedLanguagesAsync(ITranslationProvider translationProvider)
            {
                var providerName = translationProvider.GetType().Name;
                _log.InitializingProvider(providerName);
                await translationProvider.InitializeSupportedLanguagesAsync(cancellationToken);
                _log.InitializedProvider(providerName);
            }

            await Task.WhenAll(_providers.Select(InitializeSupportedLanguagesAsync));

            _initialized = true;
        }

        return true;
    }

    public IReadOnlyList<SupportedLanguage> GetSupportedLanguagesForOptions()
    {
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

            _supportedLanguagesForOptions = [.. supportedLanguages.OrderBy(l => l.Name)];
        }

        return _supportedLanguagesForOptions;
    }

    public async Task<TranslationResult?> TranslateAsync(
        Func<ITranslationProvider, CancellationToken, Task<TranslationResult?>> action,
        CancellationToken cancellationToken)
    {
        ThrowIfProvidersNotInitialized();

        TranslationResult? translationResult = null;
        foreach (var translationProvider in _providers)
        {
            var providerName = translationProvider.GetType().Name;
            _log.TranslatorAttempt(providerName);

            try
            {
                translationResult = await action(translationProvider, cancellationToken);
                if (translationResult is not null)
                {
                    _log.TranslationSuccess(providerName);
                    break;
                }
            }
            catch (Exception ex)
            {
                _log.TranslationFailure(ex, providerName);

                // If this is the last provider, rethrow the exception.
                if (ReferenceEquals(translationProvider, _providers[^1]))
                {
                    throw new TranslationFailureException(providerName, ex);
                }
            }
        }

        return translationResult;
    }

    private void ThrowIfProvidersNotInitialized([CallerMemberName] string? memberName = null)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException(
                $"Providers must be initialized before calling {nameof(TranslationProviderFactory)}.{memberName}.");
        }
    }

    private sealed partial class Log(ILogger logger)
    {
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

        [LoggerMessage(Level = LogLevel.Information, Message = "Attempting to use {providerName}...")]
        public partial void TranslatorAttempt(string providerName);

        [LoggerMessage(Level = LogLevel.Information, Message = "Successfully translated text with {providerName}.")]
        public partial void TranslationSuccess(string providerName);

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to translate text with {providerName}.")]
        public partial void TranslationFailure(Exception ex, string providerName);
    }
}

internal interface ITranslationProviderFactory
{
    public ITranslationProvider PrimaryProvider { get; }

    public Task<bool> InitializeProvidersAsync(CancellationToken cancellationToken);

    public IReadOnlyList<SupportedLanguage> GetSupportedLanguagesForOptions();

    /// <summary>
    /// Iterates through available translation providers until a translation result is returned.
    /// </summary>
    /// <param name="action">The action to take on the current translation provider.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Translation result or null.</returns>
    /// <exception cref="TranslationFailureException">If the last provider failed.</exception>
    public Task<TranslationResult?> TranslateAsync(
        Func<ITranslationProvider, CancellationToken, Task<TranslationResult?>> action,
        CancellationToken cancellationToken);
}
