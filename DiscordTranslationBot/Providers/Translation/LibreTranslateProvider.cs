using System.Text.Json;
using DiscordTranslationBot.Configuration.TranslationProviders;
using DiscordTranslationBot.Extensions;
using DiscordTranslationBot.Models;
using DiscordTranslationBot.Models.Providers.Translation;
using DiscordTranslationBot.Models.Providers.Translation.LibreTranslate;
using Microsoft.Extensions.Options;
using Serilog;
using ILogger = Serilog.ILogger;

namespace DiscordTranslationBot.Providers.Translation;

/// <summary>
/// Provider for LibreTranslate.
/// </summary>
public sealed class LibreTranslateProvider : TranslationProviderBase
{
    private static readonly ILogger Logger = Log.ForContext<LibreTranslateProvider>();
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly LibreTranslateOptions _libreTranslateOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibreTranslateProvider"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory to use.</param>
    /// <param name="translationProvidersOptions">Translation providers options.</param>
    public LibreTranslateProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<TranslationProvidersOptions> translationProvidersOptions
    )
    {
        _httpClientFactory = httpClientFactory;
        _libreTranslateOptions = translationProvidersOptions.Value.LibreTranslate;
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
            Logger.Error(
                "Languages endpoint returned unsuccessful status code {StatusCode}.",
                response.StatusCode
            );

            throw new InvalidOperationException(
                $"Languages endpoint returned unsuccessful status code {response.StatusCode}."
            );
        }

        var content = JsonSerializer.Deserialize<IList<Language>>(
            await response.Content.ReadAsStringAsync(cancellationToken)
        );

        if (content?.Any() != true)
        {
            Logger.Error("Languages endpoint returned no language codes.");
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
                Logger.Error(
                    "Translate endpoint returned unsuccessful status code {StatusCode}.",
                    response.StatusCode
                );

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
                Logger.Error("No translation returned.");
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
            Logger.Error(ex, "Failed to deserialize the response.");
            throw;
        }
        catch (HttpRequestException ex)
        {
            Logger.Error(ex, "Unable to connect to the {ProviderName} API URL.", ProviderName);
            throw;
        }
    }
}
