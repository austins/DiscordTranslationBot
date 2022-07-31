﻿using System.Text;
using System.Text.Json;
using DiscordTranslationBot.Configuration.TranslationProviders;
using DiscordTranslationBot.Models;
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
    private readonly ILogger<LibreTranslateProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibreTranslateProvider"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory to use.</param>
    /// <param name="translationProvidersOptions">Translation providers options.</param>
    /// <param name="logger">Logger to use.</param>
    public LibreTranslateProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<TranslationProvidersOptions> translationProvidersOptions,
        ILogger<LibreTranslateProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _libreTranslateOptions = translationProvidersOptions.Value.LibreTranslate;
        _logger = logger;
    }

    /// <inheritdoc cref="TranslationProviderBase.ProviderName"/>
    public override string ProviderName => "LibreTranslate";

    /// <inheritdoc cref="TranslationProviderBase.TranslateAsync"/>
    /// <exception cref="InvalidOperationException">An error occured.</exception>
    public override async Task<TranslationResult> TranslateAsync(Country country, string text, CancellationToken cancellationToken)
    {
        await InitializeSupportedLangCodesAsync(cancellationToken);

        try
        {
            var langCode = GetLangCodeByCountry(country);
            var result = new TranslationResult { TargetLanguageCode = langCode };

            using var httpClient = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(
                    $"{_libreTranslateOptions.ApiUrl}/translate"),
            };

            var requestBody = JsonSerializer.Serialize(new
            {
                q = text,
                source = "auto",
                target = langCode,
                format = "text",
            });

            request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Translate endpoint returned unsuccessful status code {response.StatusCode}.");
                throw new InvalidOperationException($"Translate endpoint returned unsuccessful status code {response.StatusCode}.");
            }

            var content = JsonSerializer.Deserialize<TranslateResult>(
                await response.Content.ReadAsStringAsync(cancellationToken));

            if (string.IsNullOrWhiteSpace(content?.TranslatedText))
            {
                _logger.LogError("No translation returned.");
                throw new InvalidOperationException("No translation returned.");
            }

            result.DetectedLanguageCode = content.DetectedLanguage?.LanguageCode;
            result.TranslatedText = content.TranslatedText;

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize the response.");
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, $"Unable to connect to the {ProviderName} API URL.");
            throw;
        }
    }

    /// <inheritdoc cref="TranslationProviderBase.InitializeSupportedLangCodesAsync"/>
    /// <remarks>
    /// List of supported language codes reference: https://libretranslate.com/languages.
    /// </remarks>
    protected override async Task InitializeSupportedLangCodesAsync(CancellationToken cancellationToken)
    {
        if (SupportedLangCodes.Any()) return;

        using var httpClient = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{_libreTranslateOptions.ApiUrl}/languages"),
        };

        var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Languages endpoint returned unsuccessful status code {response.StatusCode}.");
            throw new InvalidOperationException($"Languages endpoint returned unsuccessful status code {response.StatusCode}.");
        }

        var content = JsonSerializer.Deserialize<IList<Language>>(await response.Content.ReadAsStringAsync(cancellationToken));
        if (content?.Any() != true)
        {
            _logger.LogError("Languages endpoint returned no language codes.");
            throw new InvalidOperationException("Languages endpoint returned no language codes.");
        }

        SupportedLangCodes = content
            .Select(lc => lc.LangCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
