using System.Net;
using DiscordTranslationBot.Models;
using DiscordTranslationBot.Models.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.LibreTranslate;
using DiscordTranslationBot.Providers.Translation.LibreTranslate.Models;
using NeoSmart.Unicode;
using Refit;

namespace DiscordTranslationBot.Tests.Providers.Translation.LibreTranslate;

[TestClass]
public sealed class LibreTranslateProviderTests : TranslationProviderBaseTests
{
    private readonly ILibreTranslateClient _client;
    private readonly ILogger<LibreTranslateProvider> _logger;

    public LibreTranslateProviderTests()
    {
        _client = Substitute.For<ILibreTranslateClient>();

        var languagesResponse = Substitute.For<IApiResponse<IList<Language>>>();
        languagesResponse.IsSuccessStatusCode.Returns(true);
        languagesResponse.Content.Returns(
            new List<Language>
            {
                new()
                {
                    LangCode = "en",
                    Name = "English"
                },
                new()
                {
                    LangCode = "fr",
                    Name = "French"
                }
            });

        _client.GetLanguagesAsync(default).ReturnsForAnyArgs(languagesResponse);

        _logger = Substitute.For<ILogger<LibreTranslateProvider>>();

        Sut = new LibreTranslateProvider(_client, _logger);
    }

    [TestMethod]
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

        _client.TranslateAsync(
                Arg.Is<TranslateRequest>(
                    x => x.SourceLangCode == sourceLanguage.LangCode
                        && x.TargetLangCode == targetLanguage.LangCode
                        && x.Text == text),
                Arg.Any<CancellationToken>())
            .Returns(response);

        // Act
        var result = await Sut.TranslateAsync(targetLanguage, text, CancellationToken.None, sourceLanguage);

        // Assert
        result.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
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

        var response = Substitute.For<IApiResponse<TranslateResult>>();
        response.IsSuccessStatusCode.Returns(true);

        response.Content.Returns(
            new TranslateResult
            {
                DetectedLanguage = new DetectedLanguage { LanguageCode = expected.DetectedLanguageCode },
                TranslatedText = expected.TranslatedText
            });

        _client.TranslateAsync(
                Arg.Is<TranslateRequest>(
                    x => x.SourceLangCode == "auto" && x.TargetLangCode == targetLanguageCode && x.Text == text),
                Arg.Any<CancellationToken>())
            .Returns(response);

        // Act
        var result = await Sut.TranslateByCountryAsync(country, text, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public async Task InitializeSupportedLanguagesAsync_Throws_InvalidOperationException_WhenNoSupportedLanguageCodes()
    {
        // Arrange
        _client.ClearSubstitute();

        var response = Substitute.For<IApiResponse<IList<Language>>>();
        response.IsSuccessStatusCode.Returns(true);
        response.Content.Returns(new List<Language>());

        _client.GetLanguagesAsync(default).ReturnsForAnyArgs(response);

        // Create a new instance of the SUT as the constructor has already called InitializeSupportedLanguagesAsync on the class SUT.
        var sut = new LibreTranslateProvider(_client, _logger);

        // Act & Assert
        await sut.Invoking(x => x.InitializeSupportedLanguagesAsync(CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Languages endpoint returned no language codes.");

        await _client.Received(1).GetLanguagesAsync(default);
    }

    [DataTestMethod]
    [DataRow(HttpStatusCode.InternalServerError)]
    [DataRow(HttpStatusCode.ServiceUnavailable)]
    public async Task TranslateByCountryAsync_Throws_InvalidOperationException_WhenStatusCodeUnsuccessful(
        HttpStatusCode statusCode)
    {
        // Arrange
        var country = new Country(Emoji.FlagFrance.ToString()!, "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        const string text = "test";

        var response = Substitute.For<IApiResponse<TranslateResult>>();
        response.IsSuccessStatusCode.Returns(false);
        response.StatusCode.Returns(statusCode);

        _client.TranslateAsync(
                Arg.Is<TranslateRequest>(
                    x => x.SourceLangCode == "auto" && x.TargetLangCode == country.LangCodes.First() && x.Text == text),
                Arg.Any<CancellationToken>())
            .Returns(response);

        // Act & Assert
        await Sut.Invoking(x => x.TranslateByCountryAsync(country, text, CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>();

        await _client.ReceivedWithAnyArgs(1).TranslateAsync(default!, default);
    }

    [TestMethod]
    public async Task TranslateByCountryAsync_Throws_InvalidOperationException_WhenNoTranslatedText()
    {
        // Arrange
        var country = new Country(Emoji.FlagFrance.ToString()!, "France")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" }
        };

        const string text = "test";

        var response = Substitute.For<IApiResponse<TranslateResult>>();
        response.IsSuccessStatusCode.Returns(true);

        response.Content.Returns(
            new TranslateResult
            {
                DetectedLanguage = new DetectedLanguage { LanguageCode = "de" },
                TranslatedText = string.Empty
            });

        _client.TranslateAsync(
                Arg.Is<TranslateRequest>(
                    x => x.SourceLangCode == "auto" && x.TargetLangCode == country.LangCodes.First() && x.Text == text),
                Arg.Any<CancellationToken>())
            .Returns(response);

        // Act & Assert
        await Sut.Invoking(x => x.TranslateByCountryAsync(country, text, CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>();

        await _client.ReceivedWithAnyArgs(1).TranslateAsync(default!, default);
    }
}
