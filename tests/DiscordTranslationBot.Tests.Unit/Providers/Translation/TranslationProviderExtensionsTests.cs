using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.AzureTranslator.Models;

namespace DiscordTranslationBot.Tests.Unit.Providers.Translation;

public sealed class TranslationProviderExtensionsTests
{
    [Fact]
    public async Task RefitSettings_ContentSerializer_DeserializesResponseModels()
    {
        // Arrange
        using var content = new StringContent("""{"translation":{"en":{"name":"English"}}}""");

        // Act
        var languages = await TranslationProviderExtensions.RefitSettings.ContentSerializer.FromHttpContentAsync<Languages>(
            content,
            TestContext.Current.CancellationToken);

        // Assert
        languages.Should().NotBeNull();
        languages.LangCodes.Should().ContainKey("en").WhoseValue.Name.Should().Be("English");
    }
}
