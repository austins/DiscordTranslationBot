using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DiscordTranslationBot.Configuration.TranslationProviders;
using DiscordTranslationBot.Models;
using DiscordTranslationBot.Models.Providers.Translation;
using DiscordTranslationBot.Models.Providers.Translation.AzureTranslator;
using DiscordTranslationBot.Providers.Translation;
using Microsoft.Extensions.Options;
using NeoSmart.Unicode;

namespace DiscordTranslationBot.Tests.Providers.Translation;

public sealed class AzureTranslatorProviderTests : TranslationProviderBaseTests
{
    private readonly HttpClient _httpClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AzureTranslatorProvider> _logger;
    private readonly IOptions<TranslationProvidersOptions> _translationProvidersOptions;

    public AzureTranslatorProviderTests()
    {
        _httpClient = Substitute.For<HttpClient>();

        _httpClient
            .SendAsync(
                Arg.Is<HttpRequestMessage>(x => x.RequestUri!.AbsolutePath.EndsWith("languages")),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                _ =>
                    new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(
                            """
                            {
                              "translation": {
                                "en": {
                                  "name": "English",
                                  "nativeName": "English",
                                  "dir": "ltr"
                                },
                                "fr": {
                                  "name": "French",
                                  "nativeName": "Français",
                                  "dir": "ltr"
                                }
                              }
                            }
                            """
                        )
                    }
            );

        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_httpClient);

        _translationProvidersOptions = Options.Create(
            new TranslationProvidersOptions
            {
                AzureTranslator = new AzureTranslatorOptions
                {
                    Enabled = true,
                    ApiUrl = new Uri("http://localhost"),
                    Region = "westus2",
                    SecretKey = "test"
                }
            }
        );

        _logger = Substitute.For<ILogger<AzureTranslatorProvider>>();

        Sut = new AzureTranslatorProvider(_httpClientFactory, _translationProvidersOptions, _logger);
    }

    [Fact]
    public async Task TranslateAsync_WithSourceLanguage_Returns_Expected()
    {
        // Arrange
        var targetLanguage = new SupportedLanguage { LangCode = "fr", Name = "French" };
        var sourceLanguage = new SupportedLanguage { LangCode = "en", Name = "English" };

        const string text = "test";

        var expected = new TranslationResult
        {
            DetectedLanguageCode = null,
            DetectedLanguageName = null,
            TargetLanguageCode = "fr",
            TargetLanguageName = "French",
            TranslatedText = "translated"
        };

        _httpClient
            .SendAsync(
                Arg.Is<HttpRequestMessage>(
                    x =>
                        x.RequestUri!.AbsolutePath.EndsWith("translate")
                        && x.RequestUri.Query.Contains($"&from={sourceLanguage.LangCode}")
                ),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                _ =>
                    new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(
                            $$"""
                              [
                                  {
                                      "translations": [
                                          {"text": "{{expected.TranslatedText}}", "to": "{{expected.TargetLanguageCode}}"}
                                      ]
                                  }
                              ]
                              """
                        )
                    }
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

        var expected = new TranslationResult
        {
            DetectedLanguageCode = "en",
            DetectedLanguageName = "English",
            TargetLanguageCode = "fr",
            TargetLanguageName = "French",
            TranslatedText = "translated"
        };

        IList<TranslateRequest>? requestContent = null;

        _httpClient
            .SendAsync(
                Arg.Is<HttpRequestMessage>(x => x.RequestUri!.AbsolutePath.EndsWith("translate")),
                Arg.Any<CancellationToken>()
            )
            .Returns(x =>
            {
                requestContent = x.ArgAt<HttpRequestMessage>(0)
                    .Content!.ReadFromJsonAsync<IList<TranslateRequest>>(
                        cancellationToken: x.ArgAt<CancellationToken>(1)
                    )
                    .GetAwaiter()
                    .GetResult();

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        $$"""
                          [
                              {
                                  "detectedLanguage": {"language": "{{expected.DetectedLanguageCode}}", "score": 1.0},
                                  "translations": [
                                      {"text": "{{expected.TranslatedText}}", "to": "{{expected.TargetLanguageCode}}"}
                                  ]
                              }
                          ]
                          """
                    )
                };
            });

        // Act
        var result = await Sut.TranslateByCountryAsync(country, text, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expected);

        requestContent![0].Text.Should().Be(text);
    }

    [Fact]
    public async Task TranslateByCountryAsync_Throws_InvalidOperationException_WhenNoSupportedLanguageCodes()
    {
        // Arrange
        _httpClient
            .SendAsync(
                Arg.Is<HttpRequestMessage>(x => x.RequestUri!.AbsolutePath.EndsWith("languages")),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                _ => new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent("{}") }
            );

        // Create a new instance of the SUT as the constructor has already called InitializeSupportedLanguagesAsync on the class SUT.
        var sut = new AzureTranslatorProvider(_httpClientFactory, _translationProvidersOptions, _logger);

        // Act & Assert
        await sut.Invoking(x => x.InitializeSupportedLanguagesAsync(CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Languages endpoint returned no language codes.");
    }

    [Fact]
    public async Task TranslateByCountryAsync_Throws_ArgumentException_TextExceedsCharacterLimit()
    {
        // Arrange
        var country = new Country(Emoji.FlagFrance.ToString()!, "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        var text = new string('a', AzureTranslatorProvider.TextCharacterLimit);

        // Act & Assert
        await Sut.Invoking(x => x.TranslateByCountryAsync(country, text, CancellationToken.None))
            .Should()
            .ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.TooManyRequests)]
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

        _httpClient
            .SendAsync(
                Arg.Is<HttpRequestMessage>(x => x.RequestUri!.AbsolutePath.EndsWith("translate")),
                Arg.Any<CancellationToken>()
            )
            .Returns(_ => new HttpResponseMessage { StatusCode = statusCode });

        // Act & Assert
        await Sut.Invoking(x => x.TranslateByCountryAsync(country, text, CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task TranslateByCountryAsync_Throws_InvalidOperationException_WhenNoTranslations()
    {
        // Arrange
        var country = new Country(Emoji.FlagFrance.ToString()!, "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        const string text = "test";

        _httpClient
            .SendAsync(
                Arg.Is<HttpRequestMessage>(x => x.RequestUri!.AbsolutePath.EndsWith("translate")),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                _ =>
                    new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(
                            """
                            [
                                {
                                    "detectedLanguage": {"language": "en", "score": 1.0},
                                    "translations": []
                                }
                            ]
                            """
                        )
                    }
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

        _httpClient
            .SendAsync(
                Arg.Is<HttpRequestMessage>(x => x.RequestUri!.AbsolutePath.EndsWith("translate")),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                _ =>
                    new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent("invalid_json")
                    }
            );

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

        _httpClient
            .SendAsync(
                Arg.Is<HttpRequestMessage>(x => x.RequestUri!.AbsolutePath.EndsWith("translate")),
                Arg.Any<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException());

        // Act & Assert
        await Sut.Invoking(x => x.TranslateByCountryAsync(country, text, CancellationToken.None))
            .Should()
            .ThrowAsync<HttpRequestException>();
    }
}
