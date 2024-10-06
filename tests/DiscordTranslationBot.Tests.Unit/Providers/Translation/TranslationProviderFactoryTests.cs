using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.Models;

namespace DiscordTranslationBot.Tests.Unit.Providers.Translation;

public sealed class TranslationProviderFactoryTests
{
    private readonly TranslationProviderBase _lastProvider;
    private readonly TranslationProviderBase _primaryProvider;
    private readonly TranslationProviderFactory _sut;

    public TranslationProviderFactoryTests()
    {
        _primaryProvider = Substitute.For<TranslationProviderBase>();
        _lastProvider = Substitute.For<TranslationProviderBase>();

        _sut = new TranslationProviderFactory(
            [_primaryProvider, _lastProvider],
            new LoggerFake<TranslationProviderFactory>());
    }

    [Fact]
    public void Providers_ThrowsIfNotInitialized()
    {
        // Act + Assert
        _sut.Invoking(x => x.Providers).Should().Throw<InvalidOperationException>();
        _sut.Invoking(x => x.PrimaryProvider).Should().Throw<InvalidOperationException>();
        _sut.Invoking(x => x.LastProvider).Should().Throw<InvalidOperationException>();
        _sut.Invoking(x => x.GetSupportedLanguagesForOptions()).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task Providers_ReturnsIfInitialized()
    {
        // Arrange
        _primaryProvider.When(x => x.InitializeSupportedLanguagesAsync(Arg.Any<CancellationToken>())).Do(x => { });
        await _sut.InitializeProvidersAsync(CancellationToken.None);

        // Act + Assert
        _sut.Providers.Should().BeEquivalentTo([_primaryProvider, _lastProvider]);
        _sut.PrimaryProvider.Should().Be(_primaryProvider);
        _sut.LastProvider.Should().Be(_lastProvider);
    }

    [Fact]
    public async Task InitializeProvidersAsync_Success()
    {
        // Arrange
        _primaryProvider.When(x => x.InitializeSupportedLanguagesAsync(Arg.Any<CancellationToken>())).Do(x => { });
        _lastProvider.When(x => x.InitializeSupportedLanguagesAsync(Arg.Any<CancellationToken>())).Do(x => { });

        // Act
        var result = await _sut.InitializeProvidersAsync(CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        await _primaryProvider.Received(1).InitializeSupportedLanguagesAsync(Arg.Any<CancellationToken>());
        await _lastProvider.Received(1).InitializeSupportedLanguagesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializeProvidersAsync_NoProviders_ReturnsFalse()
    {
        // Arrange
        var logger = new LoggerFake<TranslationProviderFactory>();
        var sut = new TranslationProviderFactory([], logger);

        // Act
        var result = await sut.InitializeProvidersAsync(CancellationToken.None);

        // Assert
        result.Should().BeFalse();

        logger.Entries.Count.Should().Be(1);

        var logEntry = logger.Entries[0];
        logEntry.LogLevel.Should().Be(LogLevel.Error);

        logEntry
            .Message
            .Should()
            .Be("No translation providers enabled. Please configure and enable at least one translation provider.");
    }

    [Fact]
    public async Task GetSupportedLanguagesForOptions_Success()
    {
        // Arrange
        _primaryProvider.When(x => x.InitializeSupportedLanguagesAsync(Arg.Any<CancellationToken>())).Do(x => { });
        _lastProvider.When(x => x.InitializeSupportedLanguagesAsync(Arg.Any<CancellationToken>())).Do(x => { });
        await _sut.InitializeProvidersAsync(CancellationToken.None);

        var expected = new List<SupportedLanguage>
        {
            new()
            {
                LangCode = "a",
                Name = "A"
            },
            new()
            {
                LangCode = "z",
                Name = "Z"
            }
        };

        _primaryProvider.SupportedLanguages.Returns(expected.ToHashSet());

        // Act
        var result = _sut.GetSupportedLanguagesForOptions();

        // Assert
        result.Should().BeEquivalentTo(expected, o => o.WithStrictOrdering());
        _ = _primaryProvider.Received().TranslateCommandLangCodes;
        _ = _primaryProvider.Received().SupportedLanguages;
        _ = _lastProvider.DidNotReceive().TranslateCommandLangCodes;
        _ = _lastProvider.DidNotReceive().SupportedLanguages;
    }
}
