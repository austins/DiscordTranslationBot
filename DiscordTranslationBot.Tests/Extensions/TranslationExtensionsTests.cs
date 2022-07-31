using System.Text;
using DiscordTranslationBot.Extensions;
using DiscordTranslationBot.Models.Providers.Translation.AzureTranslator;
using FluentAssertions;
using Xunit;

namespace DiscordTranslationBot.Tests.Extensions;

public sealed class TranslationExtensionsTests
{
    [Fact]
    public async Task SerializeTranslationRequestContent_Returns_Expected()
    {
        // Arrange
        const string text = "の";
        const string expected = $@"[{{""Text"":""{text}""}}]";

        using var httpClient = new HttpClient();
        var content = new object[] { new { Text = text } };

        // Act
        var result = httpClient.SerializeTranslationRequestContent(content);

        // Assert
        var bytes = await result.ReadAsByteArrayAsync(CancellationToken.None);
        var resultAsString = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        resultAsString.Should().Be(expected);
    }

    [Fact]
    public async Task DeserializeTranslationResponseContentAsync_Returns_Expected()
    {
        // Arrange
        const string detectedLanguageCode = "en";
        const string translation = "の";

        const string content = $@"[{{""detectedLanguage"":{{""language"":""{detectedLanguageCode}"",""score"":1.0}},""translations"":[{{""text"":""{translation}"",""to"":""ja""}}]}}]";
        using var httpResponseMessage = new HttpResponseMessage
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json"),
        };

        var expected = new List<TranslateResult>
        {
            new()
            {
                DetectedLanguage = new DetectedLanguage { LanguageCode = detectedLanguageCode },
                Translations = new List<TranslationData> { new() { Text = translation } },
            },
        };

        // Act
        var result = await httpResponseMessage.Content.DeserializeTranslationResponseContentAsync<IList<TranslateResult>>(CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expected);
    }
}
