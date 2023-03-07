using System.Net;
using System.Text.Json;
using DiscordTranslationBot.Configuration.TranslationProviders;
using DiscordTranslationBot.Extensions;
using DiscordTranslationBot.Models;
using DiscordTranslationBot.Models.Providers.Translation;
using DiscordTranslationBot.Models.Providers.Translation.LibreTranslate;
using Microsoft.Extensions.Options;

namespace DiscordTranslationBot.Providers.Translation;

/// <summary>
/// Provider for LibreTranslate.
/// </summary>
public sealed partial class LibreTranslateProvider : TranslationProviderBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly LibreTranslateOptions _libreTranslateOptions;
    private readonly Log _log;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibreTranslateProvider"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory to use.</param>
    /// <param name="translationProvidersOptions">Translation providers options.</param>
    /// <param name="logger">Logger to use.</param>
    public LibreTranslateProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<TranslationProvidersOptions> translationProvidersOptions,
        ILogger<LibreTranslateProvider> logger
    )
    {
        _httpClientFactory = httpClientFactory;
        _libreTranslateOptions = translationProvidersOptions.Value.LibreTranslate!;
        _log = new Log(logger);
    }

    /// <inheritdoc cref="TranslationProviderBase.ProviderName"/>
    public override string ProviderName => "LibreTranslate";

    /// <inheritdoc cref="TranslationProviderBase.InitializeSupportedLanguagesAsync"/>
    /// <remarks>
    /// List of supported language codes reference: https://libretranslate.com/languages.
    /// </remarks>
    public override async Task InitializeSupportedLanguagesAsync(
        CancellationToken cancellationToken
    )
    {
        if (SupportedLanguages.Any())
        {
            return;
        }

        using var httpClient = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{_libreTranslateOptions.ApiUrl}languages")
        };

        var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _log.ResponseFailure("Languages", response.StatusCode);

            throw new InvalidOperationException(
                $"Languages endpoint returned unsuccessful status code {response.StatusCode}."
            );
        }

        var content = JsonSerializer.Deserialize<IList<Language>>(
            await response.Content.ReadAsStringAsync(cancellationToken)
        );

        if (content?.Any() != true)
        {
            _log.NoLanguageCodesReturned();
            throw new InvalidOperationException("Languages endpoint returned no language codes.");
        }

        SupportedLanguages = content
            .Select(lc => new SupportedLanguage { LangCode = lc.LangCode, Name = lc.Name })
            .ToHashSet();
    }

    /// <inheritdoc cref="TranslationProviderBase.TranslateAsync"/>
    /// <exception cref="InvalidOperationException">An error occured.</exception>
    public override async Task<TranslationResult> TranslateAsync(
        Country country,
        string text,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var supportedLanguage = GetSupportedLanguageByCountry(country);
            var result = new TranslationResult
            {
                TargetLanguageCode = supportedLanguage.LangCode,
                TargetLanguageName = supportedLanguage.Name
            };

            using var httpClient = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"{_libreTranslateOptions.ApiUrl}/translate"),
                Content = httpClient.SerializeTranslationRequestContent(
                    new
                    {
                        q = text,
                        source = "auto",
                        target = supportedLanguage.LangCode,
                        format = "text"
                    }
                )
            };

            var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _log.ResponseFailure("Translate", response.StatusCode);

                throw new InvalidOperationException(
                    $"Translate endpoint returned unsuccessful status code {response.StatusCode}."
                );
            }

            var content =
                await response.Content.DeserializeTranslationResponseContentAsync<TranslateResult>(
                    cancellationToken
                );

            if (string.IsNullOrWhiteSpace(content?.TranslatedText))
            {
                _log.NoTranslationReturned();
                throw new InvalidOperationException("No translation returned.");
            }

            result.DetectedLanguageCode = content.DetectedLanguage?.LanguageCode;

            result.DetectedLanguageName = SupportedLanguages
                .SingleOrDefault(
                    sl =>
                        sl.LangCode.Equals(
                            result.DetectedLanguageCode,
                            StringComparison.OrdinalIgnoreCase
                        )
                )
                ?.Name;

            result.TranslatedText = content.TranslatedText;

            return result;
        }
        catch (JsonException ex)
        {
            _log.DeserializationFailure(ex);
            throw;
        }
        catch (HttpRequestException ex)
        {
            _log.ConnectionFailure(ex, ProviderName);
            throw;
        }
    }

    private sealed partial class Log
    {
        private readonly ILogger<LibreTranslateProvider> _logger;

        public Log(ILogger<LibreTranslateProvider> logger)
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
}
