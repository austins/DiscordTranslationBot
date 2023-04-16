using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DiscordTranslationBot.Configuration.TranslationProviders;
using DiscordTranslationBot.Models;
using DiscordTranslationBot.Models.Providers.Translation;
using DiscordTranslationBot.Models.Providers.Translation.LibreTranslate;
using DiscordTranslationBot.Providers.Translation;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeoSmart.Unicode;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace DiscordTranslationBot.Tests.Providers.Translation;

public sealed class LibreTranslateProviderTests : TranslationProviderBaseTests
{
    private readonly HttpClient _httpClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LibreTranslateProvider> _logger;
    private readonly IOptions<TranslationProvidersOptions> _translationProvidersOptions;

    public LibreTranslateProviderTests()
    {
        _httpClient = Substitute.For<HttpClient>();

        _httpClient.SendAsync(
                Arg.Is<HttpRequestMessage>(x => x.RequestUri!.AbsolutePath.EndsWith("languages")),
                Arg.Any<CancellationToken>())
            .Returns(
                _ => new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        @"[
  {
    ""code"": ""en"",
    ""name"": ""English""
  },
  {
    ""code"": ""fr"",
    ""name"": ""French""
  }
]")
                });

        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_httpClient);

        _translationProvidersOptions = Options.Create(
            new TranslationProvidersOptions
            {
                LibreTranslate = new LibreTranslateOptions
                {
                    Enabled = true,
                    ApiUrl = new Uri("http://localhost")
                }
            });

        _logger = Substitute.For<ILogger<LibreTranslateProvider>>();

        Sut = new LibreTranslateProvider(_httpClientFactory, _translationProvidersOptions, _logger);
    }

    [Fact]
    public async Task Translate_WithSourceLanguage_Returns_Expected()
    {
        // Arrange
        var targetLanguage = new SupportedLanguage
        {
            LangCode = "fr",
            Name = "French"
        };
        var sourceLanguage = new SupportedLanguage
        {
            LangCode = "en",
            Name = "English"
        };

        const string text = "test";

        var expected = new TranslationResult
        {
            DetectedLanguageCode = null,
            DetectedLanguageName = null,
            TargetLanguageCode = "fr",
            TargetLanguageName = "French",
            TranslatedText = "translated"
        };

        TranslateRequest? requestContent = null;

        _httpClient.SendAsync(
                Arg.Is<HttpRequestMessage>(x => x.RequestUri!.AbsolutePath.EndsWith("translate")),
                Arg.Any<CancellationToken>())
            .Returns(
                x =>
                {
                    requestContent = x.ArgAt<HttpRequestMessage>(0).Content!
                        .ReadFromJsonAsync<TranslateRequest>(cancellationToken: x.ArgAt<CancellationToken>(1))
                        .GetAwaiter()
                        .GetResult();

                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(
                            $@"{{
    ""translatedText"": ""{expected.TranslatedText}""
}}")
                    };
                });

        // Act
        var result = await Sut.TranslateAsync(targetLanguage, text, CancellationToken.None, sourceLanguage);

        // Assert
        result.Should().BeEquivalentTo(expected);

        requestContent!.SourceLangCode.Should().Be(sourceLanguage.LangCode);
        requestContent.TargetLangCode.Should().Be(targetLanguage.LangCode);
        requestContent.Format.Should().Be("text");
        requestContent.Text.Should().Be(text);
    }

    [Fact]
    public async Task TranslateByCountryAsync_Returns_Expected()
    {
        // Arrange
        var country = new Country(Emoji.FlagFrance.ToString(), "France")
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

        TranslateRequest? requestContent = null;

        _httpClient.SendAsync(
                Arg.Is<HttpRequestMessage>(x => x.RequestUri!.AbsolutePath.EndsWith("translate")),
                Arg.Any<CancellationToken>())
            .Returns(
                x =>
                {
                    requestContent = x.ArgAt<HttpRequestMessage>(0).Content!
                        .ReadFromJsonAsync<TranslateRequest>(cancellationToken: x.ArgAt<CancellationToken>(1))
                        .GetAwaiter()
                        .GetResult();

                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(
                            $@"{{
    ""detectedLanguage"": {{""confidence"": 100, ""language"": ""{expected.DetectedLanguageCode}""}},
    ""translatedText"": ""{expected.TranslatedText}""
}}")
                    };
                });

        // Act
        var result = await Sut.TranslateByCountryAsync(country, text, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expected);

        requestContent!.SourceLangCode.Should().Be("auto");
        requestContent.TargetLangCode.Should().Be(country.LangCodes.First());
        requestContent.Format.Should().Be("text");
        requestContent.Text.Should().Be(text);
    }

    [Fact]
    public async Task TranslateByCountryAsync_Throws_InvalidOperationException_WhenNoSupportedLanguageCodes()
    {
        // Arrange
        _httpClient.SendAsync(
                Arg.Is<HttpRequestMessage>(x => x.RequestUri!.AbsolutePath.EndsWith("languages")),
                Arg.Any<CancellationToken>())
            .Returns(
                _ => new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("[]")
                });

        // Create a new instance of the SUT as the constructor has already called InitializeSupportedLanguagesAsync on the class SUT.
        var sut = new LibreTranslateProvider(_httpClientFactory, _translationProvidersOptions, _logger);

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
        HttpStatusCode statusCode)
    {
        // Arrange
        var country = new Country(Emoji.FlagFrance.ToString(), "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        const string text = "test";

        _httpClient.SendAsync(
                Arg.Is<HttpRequestMessage>(x => x.RequestUri!.AbsolutePath.EndsWith("translate")),
                Arg.Any<CancellationToken>())
            .Returns(_ => new HttpResponseMessage { StatusCode = statusCode });

        // Act & Assert
        await Sut.Invoking(x => x.TranslateByCountryAsync(country, text, CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task TranslateByCountryAsync_Throws_InvalidOperationException_WhenNoTranslatedText()
    {
        // Arrange
        var country = new Country(Emoji.FlagFrance.ToString(), "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        const string text = "test";

        _httpClient.SendAsync(
                Arg.Is<HttpRequestMessage>(x => x.RequestUri!.AbsolutePath.EndsWith("translate")),
                Arg.Any<CancellationToken>())
            .Returns(
                _ => new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        @"{
    ""detectedLanguage"": {""confidence"": 0, ""language"": ""de""},
    ""translatedText"": """"
}")
                });

        // Act & Assert
        await Sut.Invoking(x => x.TranslateByCountryAsync(country, text, CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task TranslateByCountryAsync_Throws_JsonException_OnFailureToDeserialize()
    {
        // Arrange
        var country = new Country(Emoji.FlagFrance.ToString(), "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        const string text = "test";

        _httpClient.SendAsync(
                Arg.Is<HttpRequestMessage>(x => x.RequestUri!.AbsolutePath.EndsWith("translate")),
                Arg.Any<CancellationToken>())
            .Returns(
                _ => new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("invalid_json")
                });

        // Act & Assert
        await Sut.Invoking(x => x.TranslateByCountryAsync(country, text, CancellationToken.None))
            .Should()
            .ThrowAsync<JsonException>();
    }

    [Fact]
    public async Task TranslateByCountryAsync_Throws_HttpRequestException_OnFailureToSendRequest()
    {
        // Arrange
        var country = new Country(Emoji.FlagFrance.ToString(), "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        const string text = "test";

        _httpClient.SendAsync(
                Arg.Is<HttpRequestMessage>(x => x.RequestUri!.AbsolutePath.EndsWith("translate")),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException());

        // Act & Assert
        await Sut.Invoking(x => x.TranslateByCountryAsync(country, text, CancellationToken.None))
            .Should()
            .ThrowAsync<HttpRequestException>();
    }
}
