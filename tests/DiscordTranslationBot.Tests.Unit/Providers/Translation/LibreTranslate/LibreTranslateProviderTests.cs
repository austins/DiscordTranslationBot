using DiscordTranslationBot.Countries.Models;
using DiscordTranslationBot.Providers.Translation.LibreTranslate;
using DiscordTranslationBot.Providers.Translation.LibreTranslate.Models;
using DiscordTranslationBot.Providers.Translation.Models;
using Refit;
using System.Net;

namespace DiscordTranslationBot.Tests.Unit.Providers.Translation.LibreTranslate;

public sealed class LibreTranslateProviderTests : IAsyncLifetime
{
    private readonly ILibreTranslateClient _client;
    private readonly ICountry _country;
    private readonly LoggerFake<LibreTranslateProvider> _logger;
    private readonly LibreTranslateProvider _sut;

    public LibreTranslateProviderTests()
    {
        _country = Substitute.For<ICountry>();
        _country.Name.Returns("France");
        _country.LangCodes.Returns(new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" });

        _client = Substitute.For<ILibreTranslateClient>();

        var languagesResponse = Substitute.For<IApiResponse<IList<Language>>>();
        languagesResponse.IsSuccessStatusCode.Returns(true);
        languagesResponse.Content.Returns(
        [
            new Language
            {
                LangCode = "en",
                Name = "English"
            },
            new Language
            {
                LangCode = "fr",
                Name = "French"
            }
        ]);

        _client.GetLanguagesAsync(default).ReturnsForAnyArgs(languagesResponse);

        _logger = new LoggerFake<LibreTranslateProvider>();

        _sut = new LibreTranslateProvider(_client, _logger);
    }

    public async Task InitializeAsync()
    {
        await _sut.InitializeSupportedLanguagesAsync(CancellationToken.None);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
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
            TargetLanguageCode = targetLanguage.LangCode,
            TargetLanguageName = "French",
            TranslatedText = "translated"
        };

        var response = Substitute.For<IApiResponse<TranslateResult>>();
        response.IsSuccessStatusCode.Returns(true);
        response.Content.Returns(new TranslateResult { TranslatedText = expected.TranslatedText });

        _client
            .TranslateAsync(
                Arg.Is<TranslateRequest>(
                    x => x.SourceLangCode == sourceLanguage.LangCode
                         && x.TargetLangCode == targetLanguage.LangCode
                         && x.Text == text),
                Arg.Any<CancellationToken>())
            .Returns(response);

        // Act
        var result = await _sut.TranslateAsync(targetLanguage, text, CancellationToken.None, sourceLanguage);

        // Assert
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task TranslateByCountryAsync_Returns_Expected()
    {
        // Arrange
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

        var response = Substitute.For<IApiResponse<TranslateResult>>();
        response.IsSuccessStatusCode.Returns(true);

        response.Content.Returns(
            new TranslateResult
            {
                DetectedLanguage = new DetectedLanguage { LanguageCode = expected.DetectedLanguageCode },
                TranslatedText = expected.TranslatedText
            });

        _client
            .TranslateAsync(
                Arg.Is<TranslateRequest>(
                    x => x.SourceLangCode == "auto" && x.TargetLangCode == targetLanguageCode && x.Text == text),
                Arg.Any<CancellationToken>())
            .Returns(response);

        // Act
        var result = await _sut.TranslateByCountryAsync(_country, text, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task InitializeSupportedLanguagesAsync_Throws_InvalidOperationException_WhenNoSupportedLanguageCodes()
    {
        // Arrange
        _client.ClearSubstitute();

        var response = Substitute.For<IApiResponse<IList<Language>>>();
        response.IsSuccessStatusCode.Returns(true);
        response.Content.Returns([]);

        _client.GetLanguagesAsync(default).ReturnsForAnyArgs(response);

        // Create a new instance of the SUT as the constructor has already called InitializeSupportedLanguagesAsync on the class SUT.
        var sut = new LibreTranslateProvider(_client, _logger);

        // Act & Assert
        await sut
            .Invoking(x => x.InitializeSupportedLanguagesAsync(CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Languages endpoint returned no language codes.");

        await _client.Received(1).GetLanguagesAsync(default);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task TranslateByCountryAsync_Throws_InvalidOperationException_WhenStatusCodeUnsuccessful(
        HttpStatusCode statusCode)
    {
        // Arrange
        const string text = "test";

        var response = Substitute.For<IApiResponse<TranslateResult>>();
        response.IsSuccessStatusCode.Returns(false);
        response.StatusCode.Returns(statusCode);

        _client
            .TranslateAsync(
                Arg.Is<TranslateRequest>(
                    x => x.SourceLangCode == "auto"
                         && x.TargetLangCode == _country.LangCodes.First()
                         && x.Text == text),
                Arg.Any<CancellationToken>())
            .Returns(response);

        // Act & Assert
        await _sut
            .Invoking(x => x.TranslateByCountryAsync(_country, text, CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>();

        await _client.ReceivedWithAnyArgs(1).TranslateAsync(default!, default);
    }

    [Fact]
    public async Task TranslateByCountryAsync_Throws_InvalidOperationException_WhenNoTranslatedText()
    {
        // Arrange
        const string text = "test";

        var response = Substitute.For<IApiResponse<TranslateResult>>();
        response.IsSuccessStatusCode.Returns(true);

        response.Content.Returns(
            new TranslateResult
            {
                DetectedLanguage = new DetectedLanguage { LanguageCode = "de" },
                TranslatedText = string.Empty
            });

        _client
            .TranslateAsync(
                Arg.Is<TranslateRequest>(
                    x => x.SourceLangCode == "auto"
                         && x.TargetLangCode == _country.LangCodes.First()
                         && x.Text == text),
                Arg.Any<CancellationToken>())
            .Returns(response);

        // Act & Assert
        await _sut
            .Invoking(x => x.TranslateByCountryAsync(_country, text, CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>();

        await _client.ReceivedWithAnyArgs(1).TranslateAsync(default!, default);
    }
}
