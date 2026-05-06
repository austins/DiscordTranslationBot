using Discord;
using DiscordTranslationBot.Providers.Translation;
using DiscordTranslationBot.Providers.Translation.Exceptions;
using DiscordTranslationBot.Providers.Translation.Models;
using System.Collections.Frozen;
using System.Collections.Immutable;

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

    [Fact]
    public async Task Providers_ThrowsIfNotInitialized()
    {
        // Act + Assert
        _sut.Invoking(x => x.PrimaryProvider).Should().Throw<InvalidOperationException>();
        _sut.Invoking(x => x.GetSupportedLanguagesForOptions()).Should().Throw<InvalidOperationException>();

        await _sut
            .Awaiting(x => x.TranslateAsync(
                async (_, _) => await Task.FromResult<TranslationResult?>(null),
                TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Providers_ReturnsIfInitialized()
    {
        // Arrange
        _primaryProvider
            .When(x => x.InitializeSupportedLanguagesAsync(TestContext.Current.CancellationToken))
            .Do(_ => { });

        await _sut.InitializeProvidersAsync(TestContext.Current.CancellationToken);

        // Act + Assert
        _sut.PrimaryProvider.Should().Be(_primaryProvider);
    }

    [Fact]
    public async Task InitializeProvidersAsync_Success()
    {
        // Arrange
        _primaryProvider
            .When(x => x.InitializeSupportedLanguagesAsync(TestContext.Current.CancellationToken))
            .Do(_ => { });

        _lastProvider
            .When(x => x.InitializeSupportedLanguagesAsync(TestContext.Current.CancellationToken))
            .Do(_ => { });

        // Act
        var result = await _sut.InitializeProvidersAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
        await _primaryProvider.Received(1).InitializeSupportedLanguagesAsync(TestContext.Current.CancellationToken);
        await _lastProvider.Received(1).InitializeSupportedLanguagesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task InitializeProvidersAsync_NoProviders_ReturnsFalse()
    {
        // Arrange
        var logger = new LoggerFake<TranslationProviderFactory>();
        var sut = new TranslationProviderFactory([], logger);

        // Act
        var result = await sut.InitializeProvidersAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeFalse();

        logger.Entries.Should().ContainSingle();
        logger.Entries[0].LogLevel.Should().Be(LogLevel.Error);
        logger
            .Entries[0]
            .Message.Should()
            .Be("No translation providers enabled. Please configure and enable at least one translation provider.");
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task GetSupportedLanguagesForOptions_Success(bool hasTranslateCommandLangCodes, bool fillRemaining)
    {
        // Arrange
        _primaryProvider
            .When(x => x.InitializeSupportedLanguagesAsync(TestContext.Current.CancellationToken))
            .Do(_ => { });

        _lastProvider
            .When(x => x.InitializeSupportedLanguagesAsync(TestContext.Current.CancellationToken))
            .Do(_ => { });

        await _sut.InitializeProvidersAsync(TestContext.Current.CancellationToken);

        var supportedLanguages = new Dictionary<string, string>
        {
            { "c", "Charlie" },
            { "d", "Delta" },
            { "a", "Alpha" },
            { "b", "Beta" }
        }.ToFrozenDictionary();

        FrozenSet<string> translateCommandLangCodes;
        ImmutableArray<SupportedLanguage> expectedResult;

        if (!hasTranslateCommandLangCodes)
        {
            // Empty TranslateCommandLangCodes - take first N from SupportedLanguages.
            _primaryProvider.TranslateCommandLangCodes.Returns([]);

            expectedResult = supportedLanguages
                .Take(SlashCommandOptionBuilder.MaxChoiceCount)
                .Select(lc => new SupportedLanguage(lc.Key, lc.Value))
                .OrderBy(l => l.Name)
                .ToImmutableArray();
        }
        else if (fillRemaining)
        {
            // TranslateCommandLangCodes has fewer codes than MaxOptionsCount - fill from remaining.
            translateCommandLangCodes = new HashSet<string> { "a" }.ToFrozenSet();
            _primaryProvider.TranslateCommandLangCodes.Returns(translateCommandLangCodes);

            expectedResult =
            [
                new SupportedLanguage("a", "Alpha"),
                new SupportedLanguage("b", "Beta"),
                new SupportedLanguage("c", "Charlie"),
                new SupportedLanguage("d", "Delta")
            ];
        }
        else
        {
            // TranslateCommandLangCodes has fewer codes than MaxOptionsCount - fill from remaining.
            translateCommandLangCodes = new HashSet<string>
            {
                "a",
                "b"
            }.ToFrozenSet();

            _primaryProvider.TranslateCommandLangCodes.Returns(translateCommandLangCodes);

            expectedResult =
            [
                new SupportedLanguage("a", "Alpha"),
                new SupportedLanguage("b", "Beta"),
                new SupportedLanguage("c", "Charlie"),
                new SupportedLanguage("d", "Delta")
            ];
        }

        _primaryProvider.SupportedLanguages.Returns(supportedLanguages);

        // Act
        var result = _sut.GetSupportedLanguagesForOptions();

        // Assert
        result.Should().BeEquivalentTo(expectedResult, o => o.WithStrictOrdering());
        _ = _primaryProvider.Received().TranslateCommandLangCodes;
        _ = _primaryProvider.Received().SupportedLanguages;
        _ = _lastProvider.DidNotReceive().TranslateCommandLangCodes;
        _ = _lastProvider.DidNotReceive().SupportedLanguages;
    }

    [Fact]
    public async Task TranslateAsync_Success()
    {
        // Arrange
        _primaryProvider
            .When(x => x.InitializeSupportedLanguagesAsync(TestContext.Current.CancellationToken))
            .Do(_ => { });

        _lastProvider
            .When(x => x.InitializeSupportedLanguagesAsync(TestContext.Current.CancellationToken))
            .Do(_ => { });

        await _sut.InitializeProvidersAsync(TestContext.Current.CancellationToken);

        _primaryProvider
            .TranslateAsync(default!, default!, TestContext.Current.CancellationToken)
            .ThrowsAsyncForAnyArgs<InvalidOperationException>();

        var translationResult = new TranslationResult { TargetLanguageCode = "a" };
        _lastProvider
            .TranslateAsync(default!, default!, TestContext.Current.CancellationToken)
            .ReturnsForAnyArgs(translationResult);

        // Act
        var result = await _sut.TranslateAsync(
            async (translationProvider, ct) =>
                await translationProvider.TranslateAsync(new SupportedLanguage("a", "a"), "text", ct),
            TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(translationResult);

        await _primaryProvider
            .ReceivedWithAnyArgs(1)
            .TranslateAsync(default!, default!, TestContext.Current.CancellationToken);

        await _lastProvider
            .ReceivedWithAnyArgs(1)
            .TranslateAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TranslateAsync_ThrowsIfLastProvider()
    {
        // Arrange
        _primaryProvider
            .When(x => x.InitializeSupportedLanguagesAsync(TestContext.Current.CancellationToken))
            .Do(_ => { });

        _lastProvider
            .When(x => x.InitializeSupportedLanguagesAsync(TestContext.Current.CancellationToken))
            .Do(_ => { });

        await _sut.InitializeProvidersAsync(TestContext.Current.CancellationToken);

        _primaryProvider
            .TranslateAsync(default!, default!, TestContext.Current.CancellationToken)
            .ThrowsAsyncForAnyArgs(new ArgumentException("test"));

        _lastProvider
            .TranslateAsync(default!, default!, TestContext.Current.CancellationToken)
            .ThrowsAsyncForAnyArgs<InvalidOperationException>();

        // Act & Assert
        await _sut
            .Awaiting(x => x.TranslateAsync(
                async (translationProvider, ct) =>
                    await translationProvider.TranslateAsync(new SupportedLanguage("a", "a"), "text", ct),
                TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<TranslationFailureException>()
            .Where(ex => !string.IsNullOrWhiteSpace(ex.ProviderName) && ex.InnerException is InvalidOperationException);
    }
}
