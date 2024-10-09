using System.Net;
using DiscordTranslationBot.Countries.Models;
using DiscordTranslationBot.Providers.Translation.AzureTranslator;
using DiscordTranslationBot.Providers.Translation.AzureTranslator.Models;
using DiscordTranslationBot.Providers.Translation.Models;
using Refit;
using Languages = DiscordTranslationBot.Providers.Translation.AzureTranslator.Models.Languages;

namespace DiscordTranslationBot.Tests.Unit.Providers.Translation.AzureTranslator;

public sealed class AzureTranslatorProviderTests : IAsyncLifetime
{
    private readonly IAzureTranslatorClient _client;
    private readonly ICountry _country;
    private readonly LoggerFake<AzureTranslatorProvider> _logger;
    private readonly AzureTranslatorProvider _sut;

    public AzureTranslatorProviderTests()
    {
        _country = Substitute.For<ICountry>();
        _country.Name.Returns("France");
        _country.LangCodes.Returns(new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fr" });

        _client = Substitute.For<IAzureTranslatorClient>();

        var languagesResponse = Substitute.For<IApiResponse<Languages>>();
        languagesResponse.IsSuccessStatusCode.Returns(true);
        languagesResponse.Content.Returns(
            new Languages
            {
                LangCodes = new Dictionary<string, Language>
                {
                    { "en", new Language { Name = "English" } },
                    { "fr", new Language { Name = "French" } }
                }
            });

        _client.GetLanguagesAsync(default).ReturnsForAnyArgs(languagesResponse);

        _logger = new LoggerFake<AzureTranslatorProvider>();

        _sut = new AzureTranslatorProvider(_client, _logger);
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
    public async Task TranslateAsync_WithSourceLanguage_Returns_Expected()
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

        var response = Substitute.For<IApiResponse<IList<TranslateResult>>>();
        response.IsSuccessStatusCode.Returns(true);

        response.Content.Returns(
            [new TranslateResult { Translations = [new TranslationData { Text = expected.TranslatedText }] }]);

        _client
            .TranslateAsync(
                expected.TargetLanguageCode,
                Arg.Is<IList<TranslateRequest>>(x => x[0].Text == text),
                Arg.Any<CancellationToken>(),
                sourceLanguage.LangCode)
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

        var expected = new TranslationResult
        {
            DetectedLanguageCode = "en",
            DetectedLanguageName = "English",
            TargetLanguageCode = "fr",
            TargetLanguageName = "French",
            TranslatedText = "translated"
        };

        var response = Substitute.For<IApiResponse<IList<TranslateResult>>>();
        response.IsSuccessStatusCode.Returns(true);

        response.Content.Returns(
        [
            new TranslateResult
            {
                DetectedLanguage = new DetectedLanguage { LanguageCode = expected.DetectedLanguageCode },
                Translations = [new TranslationData { Text = expected.TranslatedText }]
            }
        ]);

        _client
            .TranslateAsync(
                expected.TargetLanguageCode,
                Arg.Is<IList<TranslateRequest>>(x => x[0].Text == text),
                Arg.Any<CancellationToken>(),
                Arg.Any<string?>())
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

        var response = Substitute.For<IApiResponse<Languages>>();
        response.IsSuccessStatusCode.Returns(true);
        response.Content.Returns(new Languages { LangCodes = new Dictionary<string, Language>() });

        _client.GetLanguagesAsync(default).ReturnsForAnyArgs(response);

        // Create a new instance of the SUT as the constructor has already called InitializeSupportedLanguagesAsync on the class SUT.
        var sut = new AzureTranslatorProvider(_client, _logger);

        // Act & Assert
        await sut
            .Invoking(x => x.InitializeSupportedLanguagesAsync(CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Languages endpoint returned no language codes.");

        await _client.Received(1).GetLanguagesAsync(default);
    }

    [Fact]
    public async Task TranslateByCountryAsync_Throws_ArgumentException_TextExceedsCharacterLimit()
    {
        // Arrange
        var text = new string('a', AzureTranslatorProvider.TextCharacterLimit);

        // Act & Assert
        await _sut
            .Invoking(x => x.TranslateByCountryAsync(_country, text, CancellationToken.None))
            .Should()
            .ThrowAsync<ArgumentException>();

        await _client.DidNotReceiveWithAnyArgs().TranslateAsync(default!, default!, default);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task TranslateByCountryAsync_Throws_InvalidOperationException_WhenStatusCodeUnsuccessful(
        HttpStatusCode statusCode)
    {
        // Arrange
        const string text = "test";

        var response = Substitute.For<IApiResponse<IList<TranslateResult>>>();
        response.IsSuccessStatusCode.Returns(false);
        response.StatusCode.Returns(statusCode);

        _client.TranslateAsync(default!, default!, default).ReturnsForAnyArgs(response);

        // Act & Assert
        await _sut
            .Invoking(x => x.TranslateByCountryAsync(_country, text, CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>();

        await _client.ReceivedWithAnyArgs(1).TranslateAsync(default!, default!, default);
    }

    [Fact]
    public async Task TranslateByCountryAsync_Throws_InvalidOperationException_WhenNoTranslations()
    {
        // Arrange
        const string text = "test";

        var response = Substitute.For<IApiResponse<IList<TranslateResult>>>();
        response.IsSuccessStatusCode.Returns(true);
        response.Content.Returns(
        [
            new TranslateResult
            {
                DetectedLanguage = new DetectedLanguage { LanguageCode = "en" },
                Translations = []
            }
        ]);

        _client.TranslateAsync(default!, default!, default).ReturnsForAnyArgs(response);

        // Act & Assert
        await _sut
            .Invoking(x => x.TranslateByCountryAsync(_country, text, CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>();

        await _client.ReceivedWithAnyArgs(1).TranslateAsync(default!, default!, default);
    }
}
