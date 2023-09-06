using DiscordTranslationBot.Exceptions;
using DiscordTranslationBot.Models;
using DiscordTranslationBot.Providers.Translation;
using NeoSmart.Unicode;
using RichardSzalay.MockHttp;

namespace DiscordTranslationBot.Tests.Providers.Translation;

public abstract class TranslationProviderBaseTests : IAsyncLifetime
{
    protected MockHttpMessageHandler MockHttpMessageHandler { get; } = new();

    protected IHttpClientFactory HttpClientFactory { get; } = Substitute.For<IHttpClientFactory>();

    protected TranslationProviderBase Sut { get; init; } = null!;

    public async Task InitializeAsync()
    {
        HttpClientFactory
            .CreateClient(Arg.Is<string>(x => x == TranslationProviderBase.ClientName))
            .Returns(_ => new HttpClient(MockHttpMessageHandler));

        await Sut.InitializeSupportedLanguagesAsync(CancellationToken.None);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public void ProviderName_IsNotEmpty()
    {
        // Assert
        Sut.ProviderName.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TranslateByCountryAsync_Throws_UnsupportedCountryException_IfLangCodeNotFound()
    {
        // Arrange
        var country = new Country(Emoji.FlagFrance.ToString()!, "unsupported_country")
        {
            LangCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        };

        const string text = "test";

        // Act & Assert
        await Sut.Invoking(x => x.TranslateByCountryAsync(country, text, CancellationToken.None))
            .Should()
            .ThrowAsync<UnsupportedCountryException>();
    }
}
