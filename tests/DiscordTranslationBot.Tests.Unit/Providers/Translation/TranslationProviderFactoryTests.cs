using DiscordTranslationBot.Providers.Translation.Models;
#pragma warning disable IDE0005
using DiscordTranslationBot.Providers.Translation;

#pragma warning restore IDE0005

namespace DiscordTranslationBot.Tests.Unit.Providers.Translation;

public sealed class TranslationProviderFactoryTests
{
    private readonly ITranslationProvider _lastProvider;
    private readonly ITranslationProvider _primaryProvider;
    private readonly TranslationProviderFactory _sut;

    public TranslationProviderFactoryTests()
    {
        _primaryProvider = Substitute.For<ITranslationProvider>();
        _lastProvider = Substitute.For<ITranslationProvider>();

        _sut = new TranslationProviderFactory(
            [_primaryProvider, _lastProvider],
            new LoggerFake<TranslationProviderFactory>());
    }

    [Test]
    public void Providers_ThrowsIfNotInitialized()
    {
        // Act + Assert
        Should.Throw<InvalidOperationException>(() => _sut.Providers);
        Should.Throw<InvalidOperationException>(() => _sut.PrimaryProvider);
        Should.Throw<InvalidOperationException>(() => _sut.LastProvider);
        Should.Throw<InvalidOperationException>(() => _sut.GetSupportedLanguagesForOptions());
    }

    [Test]
    public async Task Providers_ReturnsIfInitialized(CancellationToken cancellationToken)
    {
        // Arrange
        _primaryProvider.When(x => x.InitializeSupportedLanguagesAsync(Arg.Any<CancellationToken>())).Do(_ => { });
        await _sut.InitializeProvidersAsync(cancellationToken);

        // Act + Assert
        _sut.Providers.ShouldBeEquivalentTo(
            new List<ITranslationProvider>
            {
                _primaryProvider,
                _lastProvider
            });

        _sut.PrimaryProvider.ShouldBe(_primaryProvider);
        _sut.LastProvider.ShouldBe(_lastProvider);
    }

    [Test]
    public async Task InitializeProvidersAsync_Success(CancellationToken cancellationToken)
    {
        // Arrange
        _primaryProvider.When(x => x.InitializeSupportedLanguagesAsync(Arg.Any<CancellationToken>())).Do(_ => { });
        _lastProvider.When(x => x.InitializeSupportedLanguagesAsync(Arg.Any<CancellationToken>())).Do(_ => { });

        // Act
        var result = await _sut.InitializeProvidersAsync(cancellationToken);

        // Assert
        result.ShouldBeTrue();
        await _primaryProvider.Received(1).InitializeSupportedLanguagesAsync(Arg.Any<CancellationToken>());
        await _lastProvider.Received(1).InitializeSupportedLanguagesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task InitializeProvidersAsync_NoProviders_ReturnsFalse(CancellationToken cancellationToken)
    {
        // Arrange
        var logger = new LoggerFake<TranslationProviderFactory>();
        var sut = new TranslationProviderFactory([], logger);

        // Act
        var result = await sut.InitializeProvidersAsync(cancellationToken);

        // Assert
        result.ShouldBeFalse();

        var logEntry = logger.Entries.ShouldHaveSingleItem();
        logEntry.LogLevel.ShouldBe(LogLevel.Error);

        logEntry.Message.ShouldBe(
            "No translation providers enabled. Please configure and enable at least one translation provider.");
    }

    [Test]
    public async Task GetSupportedLanguagesForOptions_Success(CancellationToken cancellationToken)
    {
        // Arrange
        _primaryProvider.When(x => x.InitializeSupportedLanguagesAsync(Arg.Any<CancellationToken>())).Do(_ => { });
        _lastProvider.When(x => x.InitializeSupportedLanguagesAsync(Arg.Any<CancellationToken>())).Do(_ => { });
        await _sut.InitializeProvidersAsync(cancellationToken);

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
        result.ToList().ShouldBeEquivalentTo(expected);
        _ = _primaryProvider.Received().TranslateCommandLangCodes;
        _ = _primaryProvider.Received().SupportedLanguages;
        _ = _lastProvider.DidNotReceive().TranslateCommandLangCodes;
        _ = _lastProvider.DidNotReceive().SupportedLanguages;
    }
}
