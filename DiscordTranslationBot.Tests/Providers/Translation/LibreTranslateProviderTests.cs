using System.Net;
using System.Text.Json;
using DiscordTranslationBot.Configuration.TranslationProviders;
using DiscordTranslationBot.Models;
using DiscordTranslationBot.Models.Providers.Translation;
using DiscordTranslationBot.Models.Providers.Translation.LibreTranslate;
using DiscordTranslationBot.Providers.Translation;
using Microsoft.Extensions.Options;
using NeoSmart.Unicode;
using RichardSzalay.MockHttp;

namespace DiscordTranslationBot.Tests.Providers.Translation;

public sealed class LibreTranslateProviderTests : TranslationProviderBaseTests
{
    private readonly ILogger<LibreTranslateProvider> _logger;
    private readonly IOptions<TranslationProvidersOptions> _translationProvidersOptions;

    public LibreTranslateProviderTests()
    {
        MockHttpMessageHandler
            .When(HttpMethod.Get, "*/languages")
            .Respond(
                "application/json",
                """
                            [
                              {
                                "code": "en",
                                "name": "English"
                              },
                              {
                                "code": "fr",
                                "name": "French"
                              }
                            ]
                            """
            );

        _translationProvidersOptions = Options.Create(
            new TranslationProvidersOptions
            {
                LibreTranslate = new LibreTranslateOptions { Enabled = true, ApiUrl = new Uri("http://localhost") }
            }
        );

        _logger = Substitute.For<ILogger<LibreTranslateProvider>>();

        Sut = new LibreTranslateProvider(HttpClientFactory, _translationProvidersOptions, _logger);
    }

    [Fact]
    public async Task Translate_WithSourceLanguage_Returns_Expected()
    {
        // Arrange
        var targetLanguage = new SupportedLanguage { LangCode = "fr", Name = "French" };
        var sourceLanguage = new SupportedLanguage { LangCode = "en", Name = "English" };

        const string text = "test";

        var expected = new TranslationResult
        {
            DetectedLanguageCode = null,
            DetectedLanguageName = null,
            TargetLanguageCode = targetLanguage.LangCode,
            TargetLanguageName = "French",
            TranslatedText = "translated"
        };

        var request = new TranslateRequest
        {
            SourceLangCode = sourceLanguage.LangCode,
            TargetLangCode = targetLanguage.LangCode,
            Text = text
        };

        MockHttpMessageHandler
            .When(HttpMethod.Post, "*/translate")
            .WithContent(await TranslationProviderBase.SerializeRequest(request).ReadAsStringAsync())
            .Respond(
                "application/json",
                $$"""
                          {
                              "translatedText": "{{expected.TranslatedText}}"
                          }
                          """
            );

        // Act
        var result = await Sut.TranslateAsync(targetLanguage, text, CancellationToken.None, sourceLanguage);

        // Assert
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task TranslateByCountryAsync_Returns_Expected()
    {
        // Arrange
        var country = new Country(Emoji.FlagFrance.ToString()!, "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        const string text = "test";
        const string targetLanguageCode = "fr";

        var expected = new TranslationResult
        {
            DetectedLanguageCode = "en",
            DetectedLanguageName = "English",
            TargetLanguageCode = targetLanguageCode,
            TargetLanguageName = "French",
            TranslatedText = "translated"
        };

        var request = new TranslateRequest
        {
            SourceLangCode = "auto",
            TargetLangCode = targetLanguageCode,
            Text = text
        };

        MockHttpMessageHandler
            .When(HttpMethod.Post, "*/translate")
            .WithContent(await TranslationProviderBase.SerializeRequest(request).ReadAsStringAsync())
            .Respond(
                "application/json",
                $$"""
                          {
                              "detectedLanguage": {"confidence": 100, "language": "{{expected.DetectedLanguageCode}}"},
                              "translatedText": "{{expected.TranslatedText}}"
                          }
                          """
            );

        // Act
        var result = await Sut.TranslateByCountryAsync(country, text, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task InitializeSupportedLanguagesAsync_Throws_InvalidOperationException_WhenNoSupportedLanguageCodes()
    {
        // Arrange
        MockHttpMessageHandler.ResetBackendDefinitions();
        MockHttpMessageHandler.When(HttpMethod.Get, "*/languages").Respond("application/json", "[]");

        // Create a new instance of the SUT as the constructor has already called InitializeSupportedLanguagesAsync on the class SUT.
        var sut = new LibreTranslateProvider(HttpClientFactory, _translationProvidersOptions, _logger);

        // Act & Assert
        await sut.Invoking(x => x.InitializeSupportedLanguagesAsync(CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Languages endpoint returned no language codes.");
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task TranslateByCountryAsync_Throws_InvalidOperationException_WhenStatusCodeUnsuccessful(
        HttpStatusCode statusCode
    )
    {
        // Arrange
        var country = new Country(Emoji.FlagFrance.ToString()!, "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        const string text = "test";

        MockHttpMessageHandler.When(HttpMethod.Post, "*/translate").Respond(statusCode);

        // Act & Assert
        await Sut.Invoking(x => x.TranslateByCountryAsync(country, text, CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task TranslateByCountryAsync_Throws_InvalidOperationException_WhenNoTranslatedText()
    {
        // Arrange
        var country = new Country(Emoji.FlagFrance.ToString()!, "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        const string text = "test";

        MockHttpMessageHandler
            .When(HttpMethod.Post, "*/translate")
            .Respond(
                "application/json",
                """
                            {
                                "detectedLanguage": {"confidence": 0, "language": "de"},
                                "translatedText": ""
                            }
                            """
            );

        // Act & Assert
        await Sut.Invoking(x => x.TranslateByCountryAsync(country, text, CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task TranslateByCountryAsync_Throws_JsonException_OnFailureToDeserialize()
    {
        // Arrange
        var country = new Country(Emoji.FlagFrance.ToString()!, "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        const string text = "test";

        MockHttpMessageHandler.When(HttpMethod.Post, "*/translate").Respond(_ => new StringContent("invalid_json"));

        // Act & Assert
        await Sut.Invoking(x => x.TranslateByCountryAsync(country, text, CancellationToken.None))
            .Should()
            .ThrowAsync<JsonException>();
    }

    [Fact]
    public async Task TranslateByCountryAsync_Throws_HttpRequestException_OnFailureToSendRequest()
    {
        // Arrange
        var country = new Country(Emoji.FlagFrance.ToString()!, "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        const string text = "test";

        MockHttpMessageHandler.When(HttpMethod.Post, "*/translate").Respond(_ => throw new HttpRequestException());

        // Act & Assert
        await Sut.Invoking(x => x.TranslateByCountryAsync(country, text, CancellationToken.None))
            .Should()
            .ThrowAsync<HttpRequestException>();
    }
}
