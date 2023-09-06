using System.Text;
using DiscordTranslationBot.Models.Providers.Translation;
using DiscordTranslationBot.Models.Providers.Translation.AzureTranslator;
using DiscordTranslationBot.Providers.Translation;

namespace DiscordTranslationBot.Tests.Providers.Translation;

public sealed class TranslationProviderBaseStaticTests
{
    [Fact]
    public async Task DeserializeResponseAsync_Returns_Expected()
    {
        // Arrange
        const string detectedLanguageCode = "en";
        const string translation = "の";

        const string content =
            $$"""{"detectedLanguage":{"language":"{{detectedLanguageCode}}","score":1.0},"translations":[{"text":"{{translation}}","to":"ja"}]}""";

        using var response = new HttpResponseMessage();
        response.Content = new StringContent(content, Encoding.UTF8, "application/json");

        var expected = new TranslateResult
        {
            DetectedLanguage = new DetectedLanguage { LanguageCode = detectedLanguageCode },
            Translations = new List<TranslationData> { new() { Text = translation } }
        };

        // Act
        var result = await TranslationProviderBase.DeserializeResponseAsync<TranslateResult>(
            response.Content,
            CancellationToken.None
        );

        // Assert
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task DeserializeResponseAsListAsync_Returns_Expected()
    {
        // Arrange
        const string detectedLanguageCode = "en";
        const string translation = "の";

        const string content =
            $$"""[{"detectedLanguage":{"language":"{{detectedLanguageCode}}","score":1.0},"translations":[{"text":"{{translation}}","to":"ja"}]}]""";

        using var response = new HttpResponseMessage();
        response.Content = new StringContent(content, Encoding.UTF8, "application/json");

        var expected = new List<TranslateResult>
        {
            new()
            {
                DetectedLanguage = new DetectedLanguage { LanguageCode = detectedLanguageCode },
                Translations = new List<TranslationData> { new() { Text = translation } }
            }
        };

        // Act
        var result = await TranslationProviderBase.DeserializeResponseAsListAsync<TranslateResult>(
            response.Content,
            CancellationToken.None
        );

        // Assert
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task SerializeRequest_Single_Returns_Expected()
    {
        // Arrange
        const string text = "の";
        const string expected = $$"""{"Text":"{{text}}"}""";

        using var httpClient = new HttpClient();

        var request = Substitute.For<ITranslateRequest>();
        request.Text = text;

        // Act
        var result = TranslationProviderBase.SerializeRequest(request);

        // Assert
        (await result.ReadAsStringAsync(CancellationToken.None))
            .Should()
            .Be(expected);
    }

    [Fact]
    public async Task SerializeRequest_Many_Returns_Expected()
    {
        // Arrange
        const string text = "の";
        const string expected = $$"""[{"Text":"{{text}}"}]""";

        using var httpClient = new HttpClient();

        var request = new List<ITranslateRequest> { Substitute.For<ITranslateRequest>() };
        request[0].Text = text;

        // Act
        var result = TranslationProviderBase.SerializeRequest(request);

        // Assert
        (await result.ReadAsStringAsync(CancellationToken.None))
            .Should()
            .Be(expected);
    }
}
