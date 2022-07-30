using System.Net;
using System.Text.Json;
using DiscordTranslationBot.Configuration.TranslationProviders;
using DiscordTranslationBot.Models;
using DiscordTranslationBot.Models.Providers.Translation;
using DiscordTranslationBot.Providers.Translation;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace DiscordTranslationBot.Tests.Providers.Translation;

public sealed class AzureTranslatorProviderTests : TranslationProviderBaseTests
{
    private readonly Mock<HttpClient> _httpClient;

    public AzureTranslatorProviderTests()
    {
        _httpClient = new Mock<HttpClient>(MockBehavior.Strict);
        _httpClient.As<IDisposable>().Setup(x => x.Dispose());

        var httpClientFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        httpClientFactory
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(_httpClient.Object);

        var translationProvidersOptions = Options.Create(new TranslationProvidersOptions
        {
            AzureTranslator = new AzureTranslatorOptions
            {
                ApiUrl = new Uri("http://localhost"),
                Region = "westus2",
                SecretKey = "test",
            },
        });

        Sut = new AzureTranslatorProvider(
            httpClientFactory.Object,
            translationProvidersOptions,
            Mock.Of<ILogger<AzureTranslatorProvider>>());
    }

    protected override TranslationProviderBase Sut { get; }

    [Fact]
    public async Task Translate_Returns_Expected()
    {
        // Arrange
        const string countryName = CountryName.France;
        const string text = "test";

        var expected = new TranslationResult
        {
            DetectedLanguageCode = "en",
            TargetLanguageCode = "fr",
            TranslatedText = "translated",
        };

        var content = $@"[
    {{
        ""detectedLanguage"": {{""language"": ""{expected.DetectedLanguageCode}"", ""score"": 1.0}},
        ""translations"": [
            {{""text"": ""{expected.TranslatedText}"", ""to"": ""{expected.TargetLanguageCode}""}}
        ]
    }}
]";

        using var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(content),
        };

        _httpClient
            .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await Sut.TranslateAsync(countryName, text, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task TranslateAsync_Throws_ArgumentException_TextExceedsCharacterLimit()
    {
        // Arrange
        const string countryName = CountryName.France;
        var text = new string('a', AzureTranslatorProvider.TextCharacterLimit);

        // Act & Assert
        await Sut
            .Invoking(x => x.TranslateAsync(countryName, text, CancellationToken.None))
            .Should()
            .ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task TranslateAsync_Throws_InvalidOperationException_WhenStatusCodeUnsuccessful(HttpStatusCode statusCode)
    {
        // Arrange
        const string countryName = CountryName.France;
        const string text = "test";

        using var response = new HttpResponseMessage { StatusCode = statusCode };

        _httpClient
            .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act & Assert
        await Sut
            .Invoking(x => x.TranslateAsync(countryName, text, CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task TranslateAsync_Throws_InvalidOperationException_WhenNoTranslations()
    {
        // Arrange
        const string countryName = CountryName.France;
        const string text = "test";

        var content = @"[
    {
        ""detectedLanguage"": {""language"": ""en"", ""score"": 1.0},
        ""translations"": []
    }
]";

        using var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(content),
        };

        _httpClient
            .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act & Assert
        await Sut
            .Invoking(x => x.TranslateAsync(countryName, text, CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task TranslateAsync_Throws_JsonException_OnFailureToDeserialize()
    {
        // Arrange
        const string countryName = CountryName.France;
        const string text = "test";

        const string content = "invalid_json";

        using var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(content),
        };

        _httpClient
            .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act & Assert
        await Sut
            .Invoking(x => x.TranslateAsync(countryName, text, CancellationToken.None))
            .Should()
            .ThrowAsync<JsonException>();
    }

    [Fact]
    public async Task TranslateAsync_Throws_HttpRequestException_OnFailureToSendRequest()
    {
        // Arrange
        const string countryName = CountryName.France;
        const string text = "test";

        _httpClient
            .Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException());

        // Act & Assert
        await Sut
            .Invoking(x => x.TranslateAsync(countryName, text, CancellationToken.None))
            .Should()
            .ThrowAsync<HttpRequestException>();
    }
}
