﻿using System.Net;
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

        static HttpResponseMessage GetLanguagesResponseAsync()
        {
            const string languagesContent = @"{""translation"":{""fr"":{""name"":""French"",""nativeName"":""Français"",""dir"":""ltr""}}}";
            var languagesResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(languagesContent),
            };

            return languagesResponse;
        }

        _httpClient
            .Setup(x => x.SendAsync(
                It.Is<HttpRequestMessage>(x => x.RequestUri!.AbsolutePath.EndsWith("languages")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GetLanguagesResponseAsync);

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
        var country = new Country(NeoSmart.Unicode.Emoji.FlagFrance.ToString(), "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" },
        };

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
            .Setup(x => x.SendAsync(
                It.Is<HttpRequestMessage>(x => x.RequestUri!.AbsolutePath.EndsWith("translate")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await Sut.TranslateAsync(country, text, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task TranslateAsync_Throws_InvalidOperationException_WhenNoSupportedLanguageCodes()
    {
        // Arrange
        var country = new Country(NeoSmart.Unicode.Emoji.FlagFrance.ToString(), "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" },
        };

        const string text = "test";

        const string languagesContent = "{}";
        using var languagesResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(languagesContent),
        };

        _httpClient
            .Setup(x => x.SendAsync(
                It.Is<HttpRequestMessage>(x => x.RequestUri!.AbsolutePath.EndsWith("languages")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(languagesResponse);

        // Act & Assert
        await Sut
            .Invoking(x => x.TranslateAsync(country, text, CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task TranslateAsync_Throws_ArgumentException_TextExceedsCharacterLimit()
    {
        // Arrange
        var country = new Country(NeoSmart.Unicode.Emoji.FlagFrance.ToString(), "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" },
        };

        var text = new string('a', AzureTranslatorProvider.TextCharacterLimit);

        // Act & Assert
        await Sut
            .Invoking(x => x.TranslateAsync(country, text, CancellationToken.None))
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
        var country = new Country(NeoSmart.Unicode.Emoji.FlagFrance.ToString(), "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" },
        };

        const string text = "test";

        using var response = new HttpResponseMessage { StatusCode = statusCode };

        _httpClient
            .Setup(x => x.SendAsync(
                It.Is<HttpRequestMessage>(x => x.RequestUri!.AbsolutePath.EndsWith("translate")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act & Assert
        await Sut
            .Invoking(x => x.TranslateAsync(country, text, CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task TranslateAsync_Throws_InvalidOperationException_WhenNoTranslations()
    {
        // Arrange
        var country = new Country(NeoSmart.Unicode.Emoji.FlagFrance.ToString(), "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" },
        };

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
            .Setup(x => x.SendAsync(
                It.Is<HttpRequestMessage>(x => x.RequestUri!.AbsolutePath.EndsWith("translate")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act & Assert
        await Sut
            .Invoking(x => x.TranslateAsync(country, text, CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task TranslateAsync_Throws_JsonException_OnFailureToDeserialize()
    {
        // Arrange
        var country = new Country(NeoSmart.Unicode.Emoji.FlagFrance.ToString(), "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" },
        };

        const string text = "test";

        const string content = "invalid_json";

        using var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(content),
        };

        _httpClient
            .Setup(x => x.SendAsync(
                It.Is<HttpRequestMessage>(x => x.RequestUri!.AbsolutePath.EndsWith("translate")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act & Assert
        await Sut
            .Invoking(x => x.TranslateAsync(country, text, CancellationToken.None))
            .Should()
            .ThrowAsync<JsonException>();
    }

    [Fact]
    public async Task TranslateAsync_Throws_HttpRequestException_OnFailureToSendRequest()
    {
        // Arrange
        var country = new Country(NeoSmart.Unicode.Emoji.FlagFrance.ToString(), "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" },
        };

        const string text = "test";

        _httpClient
            .Setup(x => x.SendAsync(
                It.Is<HttpRequestMessage>(x => x.RequestUri!.AbsolutePath.EndsWith("translate")),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException());

        // Act & Assert
        await Sut
            .Invoking(x => x.TranslateAsync(country, text, CancellationToken.None))
            .Should()
            .ThrowAsync<HttpRequestException>();
    }
}