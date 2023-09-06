using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using DiscordTranslationBot.Exceptions;
using DiscordTranslationBot.Models;
using DiscordTranslationBot.Models.Providers.Translation;

namespace DiscordTranslationBot.Providers.Translation;

/// <summary>
/// Interface for <see cref="TranslationProviderBase" />.
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
}

/// <summary>
/// Base class for translation providers.
/// </summary>
public abstract partial class TranslationProviderBase : ITranslationProvider
{
    /// <summary>
    /// Name of the translation provider HTTP client.
    /// </summary>
    public const string ClientName = "TranslationProvider";

    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly JsonSerializerOptions SerializerOptions =
        new() { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslationProviderBase" /> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory to use.</param>
    protected TranslationProviderBase(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc cref="ITranslationProvider.TranslateCommandLangCodes" />
    public virtual IReadOnlySet<string>? TranslateCommandLangCodes => null;

    /// <inheritdoc cref="ITranslationProvider.SupportedLanguages" />
    public IReadOnlySet<SupportedLanguage> SupportedLanguages { get; protected set; } =
        new HashSet<SupportedLanguage>();

    /// <inheritdoc cref="ITranslationProvider.ProviderName" />
    public abstract string ProviderName { get; }

    /// <summary>
    /// Creates a named HTTP client instance for the translation provider.
    /// </summary>
    /// <returns>An HttpClient instance.</returns>
    public HttpClient CreateHttpClient()
    {
        return _httpClientFactory.CreateClient(ClientName);
    }

    /// <inheritdoc cref="ITranslationProvider.InitializeSupportedLanguagesAsync" />
    public abstract Task InitializeSupportedLanguagesAsync(CancellationToken cancellationToken);

    /// <inheritdoc cref="ITranslationProvider.TranslateAsync" />
    public abstract Task<TranslationResult> TranslateAsync(
        SupportedLanguage targetLanguage,
        string text,
        CancellationToken cancellationToken,
        SupportedLanguage? sourceLanguage = null
    );

    /// <inheritdoc cref="ITranslationProvider.TranslateByCountryAsync" />
    public Task<TranslationResult> TranslateByCountryAsync(
        Country country,
        string text,
        CancellationToken cancellationToken
    )
    {
        // Gets the lang code that a country supports.
        var targetLanguage =
            SupportedLanguages.FirstOrDefault(supportedLang => country.LangCodes.Contains(supportedLang.LangCode))
            ?? throw new UnsupportedCountryException($"Translation for country {country.Name} isn't supported.");

        return TranslateAsync(targetLanguage, text, cancellationToken);
    }

    /// <summary>
    /// Deserializes response content to a type suitable for processing a translation result.
    /// </summary>
    /// <param name="content">The HttpContent instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <typeparam name="TTranslateResult">Type to deserialize response content to.</typeparam>
    /// <returns>Deserialized response content.</returns>
    public static Task<TTranslateResult?> DeserializeResponseAsync<TTranslateResult>(
        HttpContent content,
        CancellationToken cancellationToken
    )
        where TTranslateResult : ITranslateResult
    {
        return content.ReadFromJsonAsync<TTranslateResult>(SerializerOptions, cancellationToken);
    }

    /// <summary>
    /// Deserializes response content list to a type suitable for processing a translation result.
    /// </summary>
    /// <param name="content">The HttpContent instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <typeparam name="TTranslateResult">Type to deserialize response content to.</typeparam>
    /// <returns>Deserialized response content.</returns>
    public static Task<IList<TTranslateResult>?> DeserializeResponseAsListAsync<TTranslateResult>(
        HttpContent content,
        CancellationToken cancellationToken
    )
        where TTranslateResult : ITranslateResult
    {
        return content.ReadFromJsonAsync<IList<TTranslateResult>>(SerializerOptions, cancellationToken);
    }

    /// <summary>
    /// Serializes a request body object to be used in a request for a translation.
    /// </summary>
    /// <param name="request">Translate request to serialize.</param>
    /// <returns>StringContent to set assigned to <see cref="HttpRequestMessage.Content" />.</returns>
    public static StringContent SerializeRequest<TTranslateRequest>(TTranslateRequest request)
        where TTranslateRequest : ITranslateRequest
    {
        return new StringContent(
            JsonSerializer.Serialize(request, SerializerOptions),
            Encoding.UTF8,
            "application/json"
        );
    }

    /// <summary>
    /// Serializes a list of request body objects to be used in a request for a translation.
    /// </summary>
    /// <param name="request">List of translate requests to serialize.</param>
    /// <returns>StringContent to set assigned to <see cref="HttpRequestMessage.Content" />.</returns>
    public static StringContent SerializeRequest<TTranslateRequest>(IList<TTranslateRequest> request)
        where TTranslateRequest : ITranslateRequest
    {
        return new StringContent(
            JsonSerializer.Serialize(request, SerializerOptions),
            Encoding.UTF8,
            "application/json"
        );
    }

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

        [LoggerMessage(Level = LogLevel.Error, Message = "Languages endpoint returned no language codes.")]
        public partial void NoLanguageCodesReturned();

        [LoggerMessage(Level = LogLevel.Error, Message = "No translation returned.")]
        public partial void NoTranslationReturned();

        [LoggerMessage(Level = LogLevel.Error, Message = "Failed to deserialize the response.")]
        public partial void DeserializationFailure(Exception ex);

        [LoggerMessage(Level = LogLevel.Error, Message = "Unable to connect to the {providerName} API URL.")]
        public partial void ConnectionFailure(Exception ex, string providerName);
    }
#pragma warning restore CS1591
}
