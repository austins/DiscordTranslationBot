using System.Net;
using DiscordTranslationBot.Exceptions;
using DiscordTranslationBot.Models;
using DiscordTranslationBot.Models.Providers.Translation;

namespace DiscordTranslationBot.Providers.Translation;

/// <summary>
/// Interface for <see cref="TranslationProviderBase"/>.
/// </summary>
public interface ITranslationProvider
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
    /// The name of the translation provider.
    /// </summary>
    public string ProviderName { get; }

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
        SupportedLanguage? sourceLanguage = null
    );

    /// <summary>
    /// Translate text by country.
    /// </summary>
    /// <param name="country">The country containing language codes to translate to.</param>
    /// <param name="text">The text to translate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Translated text.</returns>
    /// <exception cref="UnsupportedCountryException">Country not supported.</exception>
    public Task<TranslationResult> TranslateByCountryAsync(
        Country country,
        string text,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Initialize the <see cref="SupportedLanguages"/> for the provider.
    /// </summary>
    /// <remarks>
    /// This is called for each provider in <see cref="Worker.StartAsync"/> when the application starts up.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task InitializeSupportedLanguagesAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Base class for translation providers.
/// </summary>
public abstract partial class TranslationProviderBase : ITranslationProvider
{
    /// <inheritdoc cref="ITranslationProvider.TranslateCommandLangCodes"/>
    public virtual IReadOnlySet<string>? TranslateCommandLangCodes => null;

    /// <inheritdoc cref="ITranslationProvider.SupportedLanguages"/>
    public IReadOnlySet<SupportedLanguage> SupportedLanguages { get; set; } =
        new HashSet<SupportedLanguage>();

    /// <inheritdoc cref="ITranslationProvider.ProviderName"/>
    public abstract string ProviderName { get; }

    /// <inheritdoc cref="ITranslationProvider.TranslateByCountryAsync"/>
    public Task<TranslationResult> TranslateByCountryAsync(
        Country country,
        string text,
        CancellationToken cancellationToken
    )
    {
        // Gets the lang code that a country supports.
        var targetLanguage =
            SupportedLanguages.FirstOrDefault(
                supportedLang => country.LangCodes.Contains(supportedLang.LangCode)
            )
            ?? throw new UnsupportedCountryException(
                $"Translation for country {country.Name} isn't supported."
            );

        return TranslateAsync(targetLanguage, text, cancellationToken);
    }

    /// <inheritdoc cref="ITranslationProvider.InitializeSupportedLanguagesAsync"/>
    public abstract Task InitializeSupportedLanguagesAsync(CancellationToken cancellationToken);

    /// <inheritdoc cref="ITranslationProvider.TranslateAsync"/>
    public abstract Task<TranslationResult> TranslateAsync(
        SupportedLanguage targetLanguage,
        string text,
        CancellationToken cancellationToken,
        SupportedLanguage? sourceLanguage = null
    );

#pragma warning disable CS1591
    protected abstract partial class Log<TTranslationProvider>
        where TTranslationProvider : TranslationProviderBase
    {
        private readonly ILogger<TTranslationProvider> _logger;

        protected Log(ILogger<TTranslationProvider> logger)
        {
            _logger = logger;
        }

        [LoggerMessage(
            Level = LogLevel.Error,
            Message = "{endpointName} endpoint returned unsuccessful status code {statusCode}."
        )]
        public partial void ResponseFailure(string endpointName, HttpStatusCode statusCode);

        [LoggerMessage(
            Level = LogLevel.Error,
            Message = "Languages endpoint returned no language codes."
        )]
        public partial void NoLanguageCodesReturned();

        [LoggerMessage(Level = LogLevel.Error, Message = "No translation returned.")]
        public partial void NoTranslationReturned();

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to deserialize the response.")]
        public partial void DeserializationFailure(Exception ex);

        [LoggerMessage(
            Level = LogLevel.Error,
            Message = "Unable to connect to the {providerName} API URL."
        )]
        public partial void ConnectionFailure(Exception ex, string providerName);
    }
#pragma warning restore CS1591
}
