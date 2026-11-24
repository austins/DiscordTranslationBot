using DiscordTranslationBot.Countries.Models;
using DiscordTranslationBot.Providers.Translation.Models;
using DiscordTranslationBot.Telemetry;
using DiscordTranslationBot.Utilities;
using System.Collections.Frozen;
using System.Net;

namespace DiscordTranslationBot.Providers.Translation;

/// <summary>
/// Base class for translation providers.
/// </summary>
internal abstract partial class TranslationProviderBase : ITranslationProvider
{
    private readonly Instrumentation _instrumentation;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslationProviderBase" /> class.
    /// </summary>
    /// <param name="instrumentation">Instrumentation to use.</param>
    /// <param name="logger">Logger to use.</param>
    protected TranslationProviderBase(Instrumentation instrumentation, ILogger logger)
    {
        _instrumentation = instrumentation;
        ProviderLog = new Log(logger);
    }

    public FrozenDictionary<string, string> SupportedLanguages { get; protected set; } = [];

    public virtual FrozenSet<string> TranslateCommandLangCodes { get; } = [];

    protected Log ProviderLog { get; }

    public abstract Task InitializeSupportedLanguagesAsync(CancellationToken cancellationToken);

    public async Task<TranslationOutcome> TranslateAsync(
        SupportedLanguage targetLanguage,
        string text,
        CancellationToken cancellationToken,
        string? sourceLangCode = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        TranslationOutcome outcome;
        try
        {
            outcome = await TranslateCoreAsync(targetLanguage, text, cancellationToken, sourceLangCode);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Convert unexpected errors so the factory can still fall back to the next provider.
            outcome = new ProviderFailure("Translation request failed.", (ex as HttpRequestException)?.StatusCode, ex);
        }

        cancellationToken.ThrowIfCancellationRequested();

#pragma warning disable CA1508 // The analyzer does not track union case patterns.
        return outcome switch
        {
            TranslationResult result => CompleteTranslation(result),
            ProviderFailure failure => LogFailure(failure),
            UnsupportedTargetLanguage => outcome,
            null => throw new InvalidOperationException("The translation provider returned an empty outcome.")
        };
#pragma warning restore CA1508

        TranslationOutcome CompleteTranslation(TranslationResult result)
        {
            _instrumentation.TranslationCharacters.Add(
                text.Length,
                new KeyValuePair<string, object?>("provider", GetType().Name));

            result.TranslatedText = FormatUtility.RestoreDiscordTokens(text, result.TranslatedText);
            return result;
        }

        TranslationOutcome LogFailure(ProviderFailure failure)
        {
            ProviderLog.TranslationFailed(GetType().Name, failure.Reason, failure.StatusCode, failure.Cause);
            return failure;
        }
    }

    public Task<TranslationOutcome> TranslateByCountryAsync(
        Country country,
        string text,
        CancellationToken cancellationToken)
    {
        // Find first language code supported by both the country and the translation provider.
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var langCode in country.LangCodes)
        {
            if (SupportedLanguages.TryGetValue(langCode, out var name))
            {
                return TranslateAsync(new SupportedLanguage(langCode, name), text, cancellationToken);
            }
        }

        return Task.FromResult<TranslationOutcome>(new UnsupportedTargetLanguage(GetType().Name));
    }

    /// <summary>
    /// Translate text with the provider.
    /// </summary>
    /// <param name="targetLanguage">The supported language to translate to.</param>
    /// <param name="text">The text to translate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="sourceLangCode">The supported lang code to translate from.</param>
    /// <returns>The translation outcome.</returns>
    protected abstract Task<TranslationOutcome> TranslateCoreAsync(
        SupportedLanguage targetLanguage,
        string text,
        CancellationToken cancellationToken,
        string? sourceLangCode = null);

    protected partial class Log
    {
        public Log(ILogger logger)
        {
            Logger = logger;
        }

        private ILogger Logger { get; }

        [LoggerMessage(Level = LogLevel.Error, Message = "Response failure with status {statusCode}: {message}")]
        public partial void ResponseFailure(string message, HttpStatusCode? statusCode, Exception? ex = null);

        [LoggerMessage(Level = LogLevel.Error, Message = "Languages endpoint returned no language codes.")]
        public partial void NoLanguageCodesReturned();

        [LoggerMessage(Level = LogLevel.Error, Message = "Translation failed with {providerName} and status {statusCode}: {reason}")]
        public partial void TranslationFailed(
            string providerName,
            string reason,
            HttpStatusCode? statusCode,
            Exception? ex);
    }
}

internal interface ITranslationProvider
{
    /// <summary>
    /// Supported language codes for the provider by lang code and name.
    /// </summary>
    public FrozenDictionary<string, string> SupportedLanguages { get; }

    /// <summary>
    /// Lang codes that can be specified for the translate command choices.
    /// </summary>
    public FrozenSet<string> TranslateCommandLangCodes { get; }

    /// <summary>
    /// Initialize the <see cref="SupportedLanguages" /> for the provider.
    /// </summary>
    /// <remarks>
    /// This is called for each provider in <see cref="Worker.StartAsync" /> when the application starts up.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task InitializeSupportedLanguagesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Translate text.
    /// </summary>
    /// <param name="targetLanguage">The supported language to translate to.</param>
    /// <param name="text">The text to translate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="sourceLangCode">The supported lang code to translate from.</param>
    /// <returns>The translation outcome.</returns>
    public Task<TranslationOutcome> TranslateAsync(
        SupportedLanguage targetLanguage,
        string text,
        CancellationToken cancellationToken,
        string? sourceLangCode = null);

    /// <summary>
    /// Translate text by country.
    /// </summary>
    /// <param name="country">The country containing language codes to translate to.</param>
    /// <param name="text">The text to translate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The translation outcome.</returns>
    public Task<TranslationOutcome> TranslateByCountryAsync(
        Country country,
        string text,
        CancellationToken cancellationToken);
}
