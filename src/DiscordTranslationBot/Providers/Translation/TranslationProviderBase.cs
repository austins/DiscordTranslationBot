using DiscordTranslationBot.Countries.Exceptions;
using DiscordTranslationBot.Countries.Models;
using DiscordTranslationBot.Providers.Translation.Models;
using System.Net;

namespace DiscordTranslationBot.Providers.Translation;

/// <summary>
/// Base class for translation providers.
/// </summary>
internal abstract partial class TranslationProviderBase : ITranslationProvider
{
    public virtual IReadOnlySet<SupportedLanguage> SupportedLanguages { get; protected set; } =
        new HashSet<SupportedLanguage>();

    public virtual IReadOnlySet<string>? TranslateCommandLangCodes { get; }

    public abstract Task InitializeSupportedLanguagesAsync(CancellationToken cancellationToken);

    public abstract Task<TranslationResult> TranslateAsync(
        SupportedLanguage targetLanguage,
        string text,
        CancellationToken cancellationToken,
        SupportedLanguage? sourceLanguage = null);

    public Task<TranslationResult> TranslateByCountryAsync(
        Country country,
        string text,
        CancellationToken cancellationToken)
    {
        // Gets the lang code that a country supports.
        var targetLanguage =
            SupportedLanguages.FirstOrDefault(supportedLang => country.LangCodes.Contains(supportedLang.LangCode))
            ?? throw new LanguageNotSupportedForCountryException(
                $"Target language isn't supported by {GetType().Name} for {country.Name}.");

        return TranslateAsync(targetLanguage, text, cancellationToken);
    }

    protected partial class Log
    {
        public Log(ILogger logger)
        {
            Logger = logger;
        }

        protected ILogger Logger { get; }

        [LoggerMessage(Level = LogLevel.Error, Message = "Response failure with status {statusCode}: {message}")]
        public partial void ResponseFailure(string message, HttpStatusCode statusCode, Exception? ex = null);

        [LoggerMessage(Level = LogLevel.Error, Message = "Languages endpoint returned no language codes.")]
        public partial void NoLanguageCodesReturned();

        [LoggerMessage(Level = LogLevel.Error, Message = "No translation returned.")]
        public partial void NoTranslationReturned();
    }
}

internal interface ITranslationProvider
{
    /// <summary>
    /// Lang codes that can be specified for the translate command choices.
    /// </summary>
    public IReadOnlySet<string>? TranslateCommandLangCodes { get; }

    /// <summary>
    /// Supported language codes for the provider.
    /// </summary>
    public IReadOnlySet<SupportedLanguage> SupportedLanguages { get; }

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
    /// <param name="sourceLanguage">The supported language to translate from.</param>
    /// <returns>Translated text.</returns>
    public Task<TranslationResult> TranslateAsync(
        SupportedLanguage targetLanguage,
        string text,
        CancellationToken cancellationToken,
        SupportedLanguage? sourceLanguage = null);

    /// <summary>
    /// Translate text by country.
    /// </summary>
    /// <param name="country">The country containing language codes to translate to.</param>
    /// <param name="text">The text to translate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Translated text.</returns>
    /// <exception cref="LanguageNotSupportedForCountryException">Country does not support the provider's supported lang codes.</exception>
    public Task<TranslationResult> TranslateByCountryAsync(
        Country country,
        string text,
        CancellationToken cancellationToken);
}
