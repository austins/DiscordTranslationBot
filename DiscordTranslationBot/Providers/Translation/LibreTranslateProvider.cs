using System.Text.Json;
using DiscordTranslationBot.Configuration.TranslationProviders;
using DiscordTranslationBot.Extensions;
using DiscordTranslationBot.Models.Providers.Translation;
using DiscordTranslationBot.Models.Providers.Translation.LibreTranslate;
using Microsoft.Extensions.Options;

namespace DiscordTranslationBot.Providers.Translation;

/// <summary>
/// Provider for LibreTranslate.
/// </summary>
public sealed class LibreTranslateProvider : TranslationProviderBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly LibreTranslateOptions _libreTranslateOptions;
    private readonly Log<LibreTranslateProvider> _log;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibreTranslateProvider" /> class.
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
        _libreTranslateOptions = translationProvidersOptions.Value.LibreTranslate;
        _log = new Log(logger);
    }

    /// <inheritdoc cref="TranslationProviderBase.ProviderName" />
    public override string ProviderName => "LibreTranslate";

    /// <inheritdoc cref="TranslationProviderBase.InitializeSupportedLanguagesAsync" />
    /// <remarks>
    /// List of supported language codes reference: https://libretranslate.com/languages.
    /// </remarks>
    public override async Task InitializeSupportedLanguagesAsync(CancellationToken cancellationToken)
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

    /// <inheritdoc cref="TranslationProviderBase.TranslateAsync" />
    /// <exception cref="InvalidOperationException">An error occured.</exception>
    public override async Task<TranslationResult> TranslateAsync(
        SupportedLanguage targetLanguage,
        string text,
        CancellationToken cancellationToken,
        SupportedLanguage? sourceLanguage = null
    )
    {
        try
        {
            var result = new TranslationResult
            {
                TargetLanguageCode = targetLanguage.LangCode,
                TargetLanguageName = targetLanguage.Name
            };

            using var httpClient = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"{_libreTranslateOptions.ApiUrl}/translate"),
                Content = httpClient.SerializeTranslationRequestContent(
                    new TranslateRequest
                    {
                        Text = text,
                        SourceLangCode = sourceLanguage?.LangCode ?? "auto",
                        TargetLangCode = targetLanguage.LangCode
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

            var content = await response.Content.DeserializeTranslationResponseContentAsync<TranslateResult>(
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
                    sl => sl.LangCode.Equals(result.DetectedLanguageCode, StringComparison.OrdinalIgnoreCase)
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

    private sealed class Log : Log<LibreTranslateProvider>
    {
        public Log(ILogger<LibreTranslateProvider> logger)
            : base(logger) { }
    }
}
