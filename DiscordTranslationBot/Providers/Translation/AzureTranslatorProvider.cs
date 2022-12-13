using System.Text.Json;
using DiscordTranslationBot.Configuration.TranslationProviders;
using DiscordTranslationBot.Extensions;
using DiscordTranslationBot.Models;
using DiscordTranslationBot.Models.Providers.Translation;
using DiscordTranslationBot.Models.Providers.Translation.AzureTranslator;
using Microsoft.Extensions.Options;
using Serilog;
using ILogger = Serilog.ILogger;

namespace DiscordTranslationBot.Providers.Translation;

/// <summary>
/// Provider for Azure Translator.
/// </summary>
public sealed class AzureTranslatorProvider : TranslationProviderBase
{
    /// <summary>
    /// Azure has a limit of 10,000 characters for text in a request.
    /// See: https://docs.microsoft.com/en-us/azure/cognitive-services/translator/reference/v3-0-translate#request-body.
    /// </summary>
    public const int TextCharacterLimit = 10000;

    private static readonly ILogger Logger = Log.ForContext<AzureTranslatorProvider>();

    private readonly AzureTranslatorOptions _azureTranslatorOptions;

    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureTranslatorProvider"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory to use.</param>
    /// <param name="translationProvidersOptions">Translation providers options.</param>
    public AzureTranslatorProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<TranslationProvidersOptions> translationProvidersOptions
    )
    {
        _httpClientFactory = httpClientFactory;
        _azureTranslatorOptions = translationProvidersOptions.Value.AzureTranslator;
    }

    /// <inheritdoc cref="TranslationProviderBase.ProviderName"/>
    public override string ProviderName => "Azure Translator";

    /// <inheritdoc cref="TranslationProviderBase.InitializeSupportedLanguagesAsync"/>
    /// <remarks>
    /// List of supported language codes reference: https://docs.microsoft.com/en-us/azure/cognitive-services/translator/language-support#translation.
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
            RequestUri = new Uri(
                "https://api.cognitive.microsofttranslator.com/languages?api-version=3.0&scope=translation"
            )
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

        var content = JsonSerializer.Deserialize<Languages>(
            await response.Content.ReadAsStringAsync(cancellationToken)
        );
        if (content?.LangCodes?.Any() != true)
        {
            Logger.Error("Languages endpoint returned no language codes.");
            throw new InvalidOperationException("Languages endpoint returned no language codes.");
        }

        SupportedLanguages = content.LangCodes
            .Select(lc => new SupportedLanguage { LangCode = lc.Key, Name = lc.Value.Name })
            .ToHashSet();
    }

    /// <inheritdoc cref="TranslationProviderBase.TranslateAsync"/>
    /// <exception cref="ArgumentException">Text exceeds character limit.</exception>
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

            if (text.Length >= TextCharacterLimit)
            {
                Logger.Error(
                    "The text can't exceed {TextCharacterLimit} characters including spaces. Length: {TextLength}.",
                    TextCharacterLimit,
                    text.Length
                );

                throw new ArgumentException(
                    $"The text can't exceed {TextCharacterLimit} characters including spaces. Length: {text.Length}."
                );
            }

            var result = new TranslationResult
            {
                TargetLanguageCode = supportedLanguage.LangCode,
                TargetLanguageName = supportedLanguage.Name
            };

            using var httpClient = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(
                    $"{_azureTranslatorOptions.ApiUrl}translate?api-version=3.0&to={result.TargetLanguageCode}"
                ),
                Content = httpClient.SerializeTranslationRequestContent(
                    new object[] { new { Text = text } }
                )
            };

            request.Headers.Add("Ocp-Apim-Subscription-Key", _azureTranslatorOptions.SecretKey);
            request.Headers.Add("Ocp-Apim-Subscription-Region", _azureTranslatorOptions.Region);

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

            var content = await response.Content.DeserializeTranslationResponseContentAsync<
                IList<TranslateResult>
            >(cancellationToken);

            var translation = content?.SingleOrDefault();
            if (translation?.Translations.Any() != true)
            {
                Logger.Error("No translation returned.");
                throw new InvalidOperationException("No translation returned.");
            }

            result.DetectedLanguageCode = translation.DetectedLanguage?.LanguageCode;

            result.DetectedLanguageName = SupportedLanguages
                .SingleOrDefault(
                    sl =>
                        sl.LangCode.Equals(
                            result.DetectedLanguageCode,
                            StringComparison.OrdinalIgnoreCase
                        )
                )
                ?.Name;

            result.TranslatedText = translation.Translations[0].Text;

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
