using System.Text;
using DiscordTranslationBot.Extensions;
using DiscordTranslationBot.Models.Providers.Translation;
using DiscordTranslationBot.Models.Providers.Translation.AzureTranslator;

namespace DiscordTranslationBot.Tests.Extensions;

public sealed class TranslationExtensionsTests
{
    [Fact]
    public async Task SerializeTranslationRequestContent_Single_Returns_Expected()
    {
        // Arrange
        const string text = "の";
        const string expected = $@"{{""Text"":""{text}""}}";

        using var httpClient = new HttpClient();
        var content = new TranslateRequest { Text = text };

        // Act
        var result = httpClient.SerializeTranslationRequestContent(content);

        // Assert
        (await result.ReadAsStringAsync(CancellationToken.None))
            .Should()
            .Be(expected);
    }

    [Fact]
    public async Task SerializeTranslationRequestContent_Many_Returns_Expected()
    {
        // Arrange
        const string text = "の";
        const string expected = $@"[{{""Text"":""{text}""}}]";

        using var httpClient = new HttpClient();
        var content = new List<ITranslateRequest> { new TranslateRequest { Text = text } };

        // Act
        var result = httpClient.SerializeTranslationRequestContent(content);

        // Assert
        (await result.ReadAsStringAsync(CancellationToken.None))
            .Should()
            .Be(expected);
    }

    [Fact]
    public async Task DeserializeTranslationResponseContentAsync_Returns_Expected()
    {
        // Arrange
        const string detectedLanguageCode = "en";
        const string translation = "の";

        const string content =
            $@"{{""detectedLanguage"":{{""language"":""{detectedLanguageCode}"",""score"":1.0}},""translations"":[{{""text"":""{translation}"",""to"":""ja""}}]}}";

        using var httpResponseMessage = new HttpResponseMessage
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };

        var expected = new TranslateResult
        {
            DetectedLanguage = new DetectedLanguage { LanguageCode = detectedLanguageCode },
            Translations = new List<TranslationData> { new() { Text = translation } }
        };

        // Act
        var result = await httpResponseMessage.Content.DeserializeTranslationResponseContentAsync<TranslateResult>(
            CancellationToken.None
        );

        // Assert
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task DeserializeTranslationResponseContentsAsync_Returns_Expected()
    {
        // Arrange
        const string detectedLanguageCode = "en";
        const string translation = "の";

        const string content =
            $@"[{{""detectedLanguage"":{{""language"":""{detectedLanguageCode}"",""score"":1.0}},""translations"":[{{""text"":""{translation}"",""to"":""ja""}}]}}]";

        using var httpResponseMessage = new HttpResponseMessage
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };

        var expected = new List<TranslateResult>
        {
            new()
            {
                DetectedLanguage = new DetectedLanguage { LanguageCode = detectedLanguageCode },
                Translations = new List<TranslationData> { new() { Text = translation } }
            }
        };

        // Act
        var result = await httpResponseMessage.Content.DeserializeTranslationResponseContentsAsync<TranslateResult>(
            CancellationToken.None
        );

        // Assert
        result.Should().BeEquivalentTo(expected);
    }
}
